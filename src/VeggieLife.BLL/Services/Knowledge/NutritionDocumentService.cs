using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.Knowledge;
using VeggieLife.BLL.Services.AI;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.DTOs.Ai;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using VeggieLife.DAL.External.Cloudinary;

namespace VeggieLife.BLL.Services.Knowledge;

/// <summary>
/// Quản lý danh mục tài liệu dinh dưỡng PDF lưu trữ trong Qdrant Vector DB (FN-CHAT-07).
/// Đồng bộ danh mục trên Redis với fallback in-memory, gọi trực tiếp AI Service khi Thêm / Xóa / Kích hoạt tài liệu.
/// </summary>
public class NutritionDocumentService : INutritionDocumentService
{
    public const string StorageKey = "veggielife:nutrition_documents";
    private readonly IRedisService _redis;
    private readonly IAiServiceClient _aiServiceClient;
    private readonly ILogger<NutritionDocumentService> _logger;
    private readonly ICloudinaryClient? _cloudinaryClient;
    private readonly AppDbContext? _db;

    public NutritionDocumentService(
        IRedisService redis,
        IAiServiceClient aiServiceClient,
        ILogger<NutritionDocumentService> logger,
        ICloudinaryClient? cloudinaryClient = null,
        AppDbContext? db = null)
    {
        _redis = redis;
        _aiServiceClient = aiServiceClient;
        _logger = logger;
        _cloudinaryClient = cloudinaryClient;
        _db = db;
    }

    public async Task<List<AdminNutritionDocumentDto>> GetDocumentsAsync(CancellationToken ct = default)
    {
        var cached = await _redis.GetAsync<List<AdminNutritionDocumentDto>>(StorageKey, ct);
        var docs = cached != null && cached.Count > 0 ? cached : GetDefaultDocuments();

        // Chuẩn hóa FileUrl sang endpoint nội bộ hệ thống để nhúng iframe 100% hiển thị không bị lỗi Cloudinary 401 ACL deny
        bool needUpdateCache = false;
        foreach (var doc in docs)
        {
            if (string.IsNullOrWhiteSpace(doc.FileUrl) || doc.FileUrl.Contains("res.cloudinary.com"))
            {
                doc.FileUrl = $"/api/v1/admin/ai/knowledge/documents/{doc.Id}/file";
                needUpdateCache = true;
            }
        }

        if (needUpdateCache || cached == null || cached.Count == 0)
        {
            await _redis.SetAsync(StorageKey, docs, null, ct);
        }

        await ReconcileVisibilityAsync(docs, ct);
        return docs;
    }

    public async Task<AdminNutritionDocumentDto> UploadDocumentAsync(UploadNutritionDocumentRequest request, CancellationToken ct = default)
    {
        if (request.FileStream == null || request.FileLength == 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tệp tài liệu không được để trống.");
        }

        if (request.FileLength > 20 * 1024 * 1024)
        {
            throw AppException.BadRequest(ErrorCodes.ErrMediaFileTooLarge, "Dung lượng tệp vượt quá giới hạn 20MB.");
        }

        var fileName = Path.GetFileName(request.FileName);
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext != ".pdf" && ext != ".txt" && ext != ".md")
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Chỉ chấp nhận tệp định dạng .pdf, .txt hoặc .md.");
        }

        using var memoryStream = new MemoryStream();
        await request.FileStream.CopyToAsync(memoryStream, ct);
        memoryStream.Position = 0;

        // Trích văn bản thật; KHÔNG nạp nội dung giả khi không đọc được (RAG chặt sẽ trích dẫn nhầm nội dung bịa).
        // PDF có lớp chữ → PdfPig; PDF scan (chỉ có ảnh) → OCR bằng Gemini ở AI service (D118).
        string text;
        if (ext == ".pdf")
        {
            var (layerText, pageCount) = ReadPdfTextLayer(memoryStream);
            if (IsScannedPdf(layerText, pageCount))
            {
                _logger.LogInformation("[NutritionDocumentService] '{File}' là PDF scan ({Pages} trang), chuyển sang OCR.", fileName, pageCount);
                text = await OcrAndLogAsync(memoryStream.ToArray(), fileName, request.ActorUserId, ct);
            }
            else
            {
                text = layerText;
            }
            EnsureEnoughText(text);
        }
        else
        {
            text = ExtractTextFromStream(memoryStream, ext);
        }

        // Tạo Document ID chuẩn định dạng ký tự chữ số
        var docId = $"DOC_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}"[..24];
        var author = string.IsNullOrWhiteSpace(request.Author) ? "Ban Biên soạn Dinh dưỡng VeggieLife" : request.Author.Trim();

        _logger.LogInformation("[NutritionDocumentService] Đang vector hoá tài liệu '{Title}' (ID: {DocId}) vào Qdrant...", request.Title, docId);

        // Gọi AI Service để chia chunk và nhúng vector vào Qdrant; tài liệu dài chia phần (giới hạn /rag/upsert 200.000 ký tự)
        var parts = SplitIntoParts(text, MaxPartChars);
        var totalChunks = 0;
        try
        {
            for (var i = 0; i < parts.Count; i++)
            {
                var partTitle = parts.Count > 1 ? $"{request.Title.Trim()} — phần {i + 1}/{parts.Count}" : request.Title.Trim();
                totalChunks += await _aiServiceClient.UpsertKnowledgeChunksAsync(
                    "NUTRITION_DOC", PartSourceId(docId, i + 1), partTitle, parts[i], DateTime.UtcNow, ct);
            }
        }
        catch
        {
            // Nạp dở dang → gỡ các phần đã nạp để không còn đoạn mồ côi trong Qdrant
            for (var i = 0; i < parts.Count; i++)
            {
                try { await _aiServiceClient.DeleteKnowledgeAsync("NUTRITION_DOC", PartSourceId(docId, i + 1), CancellationToken.None); }
                catch (Exception ex) { _logger.LogWarning(ex, "[NutritionDocumentService] Không gỡ được phần {Part} của {DocId}.", i + 1, docId); }
            }
            throw;
        }

        // 1. Lưu bản sao tệp nguyên bản vào thư mục hệ thống để phục vụ endpoint xem trực tiếp trong iframe
        try
        {
            var storageDir = GetStorageDirectory();
            Directory.CreateDirectory(storageDir);
            var localPath = Path.Combine(storageDir, $"{docId}{ext}");
            memoryStream.Position = 0;
            using (var fs = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await memoryStream.CopyToAsync(fs, ct);
            }
            _logger.LogInformation("[NutritionDocumentService] Đã lưu tệp nguyên bản tại: {Path}", localPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[NutritionDocumentService] Không thể lưu tệp nguyên bản vào ổ đĩa.");
        }

        // 2. Tải tệp PDF nguyên bản lên Cloudinary CDN (nếu khả dụng)
        if (_cloudinaryClient != null)
        {
            try
            {
                memoryStream.Position = 0;
                var cloudRes = await _cloudinaryClient.UploadRawAsync(memoryStream, fileName, "veggielife/documents", ct);
                _logger.LogInformation("[NutritionDocumentService] Tải tệp lên Cloudinary thành công: {Url}", cloudRes.SecureUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[NutritionDocumentService] Không thể tải tệp lên Cloudinary CDN.");
            }
        }

        // Ưu tiên endpoint nội bộ hệ thống để nhúng iframe 100% hiển thị không bị lỗi Cloudinary 401 ACL deny
        string fileUrl = $"/api/v1/admin/ai/knowledge/documents/{docId}/file";


        var newDoc = new AdminNutritionDocumentDto
        {
            Id = docId,
            Title = request.Title.Trim(),
            Author = author,
            FileName = fileName,
            FileType = ext == ".pdf" ? "PDF" : "MARKDOWN",
            Chunks = totalChunks,
            Parts = parts.Count,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow,
            FileUrl = fileUrl
        };

        var currentDocs = await GetDocumentsAsync(ct);
        currentDocs.Insert(0, newDoc);
        await _redis.SetAsync(StorageKey, currentDocs, null, ct);

        _logger.LogInformation("[NutritionDocumentService] Đã vector hoá và thêm tài liệu '{Title}' thành công.", newDoc.Title);
        return newDoc;
    }

    public async Task<AdminNutritionDocumentDto> UpdateStatusAsync(string id, bool isActive, CancellationToken ct = default)
    {
        var currentDocs = await GetDocumentsAsync(ct);
        var doc = currentDocs.FirstOrDefault(d => string.Equals(d.Id, id, StringComparison.OrdinalIgnoreCase));
        if (doc == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrAdmNotFound, $"Không tìm thấy tài liệu dinh dưỡng có ID '{id}'.");
        }

        // Đổi trạng thái trên Qdrant TRƯỚC; lỗi thì ném ra, giữ nguyên Redis để hai bên không lệch nhau
        var affected = await ApplyVisibilityAsync(doc, isActive, ct);

        doc.IsActive = isActive;
        doc.UpdatedAt = DateTime.UtcNow;
        await _redis.SetAsync(StorageKey, currentDocs, null, ct);

        _logger.LogInformation("[NutritionDocumentService] {Count} đoạn vector của '{Id}' đã chuyển trạng thái.", affected, id);
        _logger.LogInformation("[NutritionDocumentService] Cập nhật trạng thái tài liệu '{Id}' thành {Status}.", id, isActive ? "HOẠT ĐỘNG" : "ĐÃ ẨN");
        return doc;
    }

    public async Task<bool> DeleteDocumentAsync(string id, CancellationToken ct = default)
    {
        var currentDocs = await GetDocumentsAsync(ct);
        var doc = currentDocs.FirstOrDefault(d => string.Equals(d.Id, id, StringComparison.OrdinalIgnoreCase));
        if (doc == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrAdmNotFound, $"Không tìm thấy tài liệu dinh dưỡng có ID '{id}'.");
        }

        _logger.LogInformation("[NutritionDocumentService] Đang xóa vector của tài liệu '{Id}' khỏi Qdrant...", id);
        var (sourceIds, bookNo, prefix) = ResolveQdrantSources(doc);
        if (prefix == null)
        {
            // Tài liệu mặc định (sách/bài nhóm biên soạn) nằm trong thư mục knowledge của AI service, lần reindex sau sẽ nạp lại;
            // nên chỉ ẩn vĩnh viễn trên Qdrant (cờ ẩn được giữ khi reindex) thay vì xóa điểm.
            await _aiServiceClient.SetKnowledgeVisibilityAsync("NUTRITION_DOC", sourceIds, bookNo, null, false, ct);
        }
        else
        {
            for (var i = 1; i <= Math.Max(1, doc.Parts); i++)
            {
                await _aiServiceClient.DeleteKnowledgeAsync("NUTRITION_DOC", PartSourceId(doc.Id, i), ct);
            }
        }

        currentDocs.Remove(doc);
        await _redis.SetAsync(StorageKey, currentDocs, null, ct);

        _logger.LogInformation("[NutritionDocumentService] Đã xóa tài liệu '{Id}' khỏi kho tri thức.", id);
        return true;
    }

    public async Task<(Stream FileStream, string ContentType, string FileName)?> GetDocumentFileAsync(string id, CancellationToken ct = default)
    {
        var currentDocs = await GetDocumentsAsync(ct);
        var doc = currentDocs.FirstOrDefault(d => string.Equals(d.Id, id, StringComparison.OrdinalIgnoreCase));

        var storageDir = GetStorageDirectory();
        var candidatePaths = new List<string>();

        // 1. Tìm theo ID tệp trực tiếp trong thư mục storage
        candidatePaths.Add(Path.Combine(storageDir, $"{id}.pdf"));
        candidatePaths.Add(Path.Combine(storageDir, $"{id}.txt"));
        candidatePaths.Add(Path.Combine(storageDir, $"{id}.md"));
        candidatePaths.Add(Path.Combine(storageDir, id));

        // 2. Tìm theo tên tệp gốc (FileName)
        if (doc != null && !string.IsNullOrWhiteSpace(doc.FileName))
        {
            candidatePaths.Add(Path.Combine(storageDir, doc.FileName));
            // Kiểm tra thêm thư mục static frontend nếu đang chạy trên môi trường dev
            var frontendStatic = Path.GetFullPath(Path.Combine(storageDir, "..", "..", "..", "..", "veggielife-frontend", "public", "docs", "nutrition", doc.FileName));
            candidatePaths.Add(frontendStatic);
        }

        foreach (var p in candidatePaths)
        {
            if (File.Exists(p))
            {
                var ext = Path.GetExtension(p).ToLowerInvariant();
                var contentType = ext switch
                {
                    ".pdf" => "application/pdf",
                    ".txt" => "text/plain; charset=utf-8",
                    ".md" => "text/markdown; charset=utf-8",
                    _ => "application/octet-stream"
                };
                var stream = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.Read);
                var resolvedName = doc?.FileName ?? Path.GetFileName(p);
                return (stream, contentType, resolvedName);
            }
        }

        return null;
    }

    public static string GetStorageDirectory()
    {
        var baseDir = AppContext.BaseDirectory;
        var appDataDocs = Path.Combine(baseDir, "App_Data", "documents");
        if (Directory.Exists(appDataDocs)) return appDataDocs;

        var currentDirDocs = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "documents");
        if (Directory.Exists(currentDirDocs)) return currentDirDocs;

        var projDirDocs = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "App_Data", "documents"));
        if (Directory.Exists(projDirDocs)) return projDirDocs;

        return appDataDocs;
    }

    /// <summary>Giới hạn mỗi lần gọi /rag/upsert (AI service nhận tối đa 200.000 ký tự).</summary>
    public const int MaxPartChars = 150_000;

    /// <summary>PDF có trung bình ít hơn ngần này ký tự mỗi trang coi như bản scan (ảnh), không có lớp văn bản.</summary>
    public const int MinCharsPerPage = 40;

    private static bool _visibilityReconciled;

    /// <summary>sourceId trên Qdrant của phần thứ <paramref name="part"/> (phần 1 giữ nguyên Id để tương thích dữ liệu cũ).</summary>
    public static string PartSourceId(string docId, int part) => part <= 1 ? docId : $"{docId}_P{part}";

    /// <summary>
    /// Ánh xạ Id tài liệu trên giao diện sang nguồn trên Qdrant:
    /// DOC_BOOK_N → các phần sách N (sourceId N*1000+k, do scripts/ingest_books.py nạp);
    /// DOC_GUIDE_X → sourceId "X" (front-matter id trong knowledge/nutrition);
    /// tài liệu Admin tải lên → sourceId = Id và các phần Id_P2, Id_P3...
    /// </summary>
    public static (List<string> SourceIds, int? BookNo, string? Prefix) ResolveQdrantSources(AdminNutritionDocumentDto doc)
    {
        if (doc.Id.StartsWith("DOC_BOOK_", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(doc.Id["DOC_BOOK_".Length..], out var bookNo))
        {
            return (new List<string>(), bookNo, null);
        }

        if (doc.Id.StartsWith("DOC_GUIDE_", StringComparison.OrdinalIgnoreCase))
        {
            return (new List<string> { doc.Id["DOC_GUIDE_".Length..] }, null, null);
        }

        return (new List<string> { doc.Id }, null, doc.Id);
    }

    private Task<int> ApplyVisibilityAsync(AdminNutritionDocumentDto doc, bool active, CancellationToken ct)
    {
        var (sourceIds, bookNo, prefix) = ResolveQdrantSources(doc);
        return _aiServiceClient.SetKnowledgeVisibilityAsync("NUTRITION_DOC", sourceIds, bookNo, prefix, active, ct);
    }

    /// <summary>
    /// Đẩy trạng thái bật/ẩn đang lưu ở Redis sang Qdrant (một lần mỗi tiến trình, khi Admin mở danh sách tài liệu).
    /// Sửa dữ liệu cũ: trước đây ẩn tài liệu chỉ đổi cờ Redis nên đoạn vector vẫn được chatbot trích.
    /// </summary>
    private async Task ReconcileVisibilityAsync(List<AdminNutritionDocumentDto> docs, CancellationToken ct)
    {
        if (_visibilityReconciled) return;
        try
        {
            foreach (var doc in docs)
            {
                await ApplyVisibilityAsync(doc, doc.IsActive, ct);
            }

            // Tài liệu mặc định đã bị xóa khỏi danh sách (bản cũ xóa sai sourceId nên đoạn vector còn sót) → ẩn trên Qdrant
            var listed = new HashSet<string>(docs.Select(d => d.Id), StringComparer.OrdinalIgnoreCase);
            foreach (var removed in GetDefaultDocuments().Where(d => !listed.Contains(d.Id)))
            {
                await ApplyVisibilityAsync(removed, false, ct);
            }
            _visibilityReconciled = true;
            _logger.LogInformation("[NutritionDocumentService] Đã đối soát trạng thái bật/ẩn của {Count} tài liệu với Qdrant.", docs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[NutritionDocumentService] Chưa đối soát được trạng thái tài liệu với Qdrant, sẽ thử lại lần sau.");
        }
    }

    /// <summary>Chia văn bản dài thành các phần ≤ maxChars, cắt ở ranh giới đoạn văn.</summary>
    public static List<string> SplitIntoParts(string text, int maxChars)
    {
        var parts = new List<string>();
        var sb = new StringBuilder();
        foreach (var para in Regex.Split(text, @"\n\s*\n"))
        {
            var piece = para.Trim();
            if (piece.Length == 0) continue;
            while (piece.Length > maxChars)
            {
                if (sb.Length > 0) { parts.Add(sb.ToString().Trim()); sb.Clear(); }
                parts.Add(piece[..maxChars]);
                piece = piece[maxChars..];
            }
            if (sb.Length + piece.Length + 2 > maxChars && sb.Length > 0)
            {
                parts.Add(sb.ToString().Trim());
                sb.Clear();
            }
            sb.Append(piece).Append("\n\n");
        }
        if (sb.ToString().Trim().Length > 0) parts.Add(sb.ToString().Trim());
        return parts;
    }

    /// <summary>Đọc lớp chữ của PDF bằng PdfPig theo thứ tự đọc từng trang; PDF hỏng/có mật khẩu → 422.</summary>
    public static (string Text, int Pages) ReadPdfTextLayer(Stream stream)
    {
        var sb = new StringBuilder();
        try
        {
            using var pdf = UglyToad.PdfPig.PdfDocument.Open(stream);
            foreach (var page in pdf.GetPages())
            {
                var pageText = UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor.ContentOrderTextExtractor.GetText(page);
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    sb.Append(pageText.Trim()).Append("\n\n");
                }
            }
            return (sb.ToString().Replace("\r\n", "\n").Trim(), pdf.NumberOfPages);
        }
        catch (Exception ex) when (ex is not AppException)
        {
            throw AppException.UnprocessableEntity(ErrorCodes.ErrMediaInvalidFile,
                "Không đọc được tệp PDF (tệp hỏng, bị mã hóa hoặc có mật khẩu).");
        }
    }

    /// <summary>PDF có trung bình dưới <see cref="MinCharsPerPage"/> ký tự mỗi trang coi như bản scan (ảnh).</summary>
    public static bool IsScannedPdf(string text, int pages)
        => pages > 0 && Regex.Replace(text, @"\s+", string.Empty).Length < pages * MinCharsPerPage;

    private static void EnsureEnoughText(string text)
    {
        if (Regex.Replace(text, @"\s+", string.Empty).Length < 50)
        {
            throw AppException.UnprocessableEntity(ErrorCodes.ErrMediaInvalidFile, "Tài liệu không có đủ nội dung văn bản để đưa vào kho tri thức.");
        }
    }

    /// <summary>
    /// Trích văn bản không cần AI: TXT/MD đọc UTF-8; PDF đọc lớp chữ. PDF scan → 422 (luồng tải lên sẽ OCR thay vì gọi hàm này).
    /// </summary>
    public static string ExtractTextFromStream(Stream stream, string ext)
    {
        string text;
        if (ext == ".txt" || ext == ".md")
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            text = reader.ReadToEnd().Replace("\r\n", "\n").Trim();
        }
        else
        {
            var (layerText, pageCount) = ReadPdfTextLayer(stream);
            if (IsScannedPdf(layerText, pageCount))
            {
                throw AppException.UnprocessableEntity(ErrorCodes.ErrMediaInvalidFile,
                    $"PDF không có lớp văn bản (bản scan dạng ảnh, {pageCount} trang), cần nhận dạng chữ (OCR).");
            }
            text = layerText;
        }

        EnsureEnoughText(text);
        return text;
    }

    /// <summary>OCR qua AI service và ghi AIQuotaLogs (Feature_Code AI_EMBEDDING — khâu nạp kho tri thức; CHECK của bảng chưa có mã OCR riêng).</summary>
    private async Task<string> OcrAndLogAsync(byte[] pdf, string fileName, int? actorUserId, CancellationToken ct)
    {
        var started = DateTime.UtcNow;
        AiOcrPdfResult result;
        try
        {
            result = await _aiServiceClient.OcrPdfAsync(pdf, fileName, ct);
        }
        catch (AppException ex)
        {
            await LogOcrAsync(actorUserId, 0, 0, "ERROR", ex.Message, (int)(DateTime.UtcNow - started).TotalMilliseconds, ct);
            throw;
        }

        await LogOcrAsync(actorUserId, result.Usage.PromptTokens, result.Usage.CompletionTokens, "SUCCESS", null,
            (int)(DateTime.UtcNow - started).TotalMilliseconds, ct);
        _logger.LogInformation("[NutritionDocumentService] OCR '{File}': {Pages} trang, {Chars} ký tự, {In}/{Out} token.",
            fileName, result.Pages, result.Text.Length, result.Usage.PromptTokens, result.Usage.CompletionTokens);
        return result.Text.Replace("\r\n", "\n").Trim();
    }

    private async Task LogOcrAsync(int? actorUserId, int promptTokens, int completionTokens, string status, string? error, int elapsedMs, CancellationToken ct)
    {
        if (_db == null || actorUserId == null) return;
        try
        {
            _db.AiquotaLogs.Add(new AiquotaLog
            {
                UserId = actorUserId.Value,
                FeatureCode = "AI_EMBEDDING",
                TokensConsumed = promptTokens + completionTokens,
                CostEstimateUsd = AiCostCalculator.Calculate(promptTokens, completionTokens),
                RequestStatus = status,
                ErrorMessage = error is { Length: > 500 } ? error[..500] : error,
                ExecutionTimeMs = elapsedMs,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[NutritionDocumentService] Không ghi được AIQuotaLogs cho OCR.");
        }
    }

    private static List<AdminNutritionDocumentDto> GetDefaultDocuments()
    {
        return new List<AdminNutritionDocumentDto>
        {
            new()
            {
                Id = "DOC_BOOK_2",
                Title = "How Not To Die (Dinh dưỡng & Phòng ngừa bệnh tật toàn diện)",
                Author = "Michael Greger, M.D., FACLM",
                FileName = "How_Not_To_Die.pdf",
                FileType = "PDF",
                Chunks = 1129,
                IsActive = true,
                UpdatedAt = DateTime.Parse("2026-10-10T11:03:19Z").ToUniversalTime()
            },
            new()
            {
                Id = "DOC_BOOK_1",
                Title = "Becoming Vegan (Comprehensive Edition)",
                Author = "Brenda Davis, RD & Vesanto Melina, MS, RD",
                FileName = "Becoming_Vegan_Comprehensive_Edition.pdf",
                FileType = "PDF",
                Chunks = 1113,
                IsActive = true,
                UpdatedAt = DateTime.Parse("2026-10-10T11:01:29Z").ToUniversalTime()
            },
            new()
            {
                Id = "DOC_BOOK_3",
                Title = "The China Study (Bí mật dinh dưỡng cho sức khỏe toàn diện)",
                Author = "T. Colin Campbell, PhD & Thomas M. Campbell II, MD",
                FileName = "The_China_Study_Revised_and_Expanded_Edition.pdf",
                FileType = "PDF",
                Chunks = 386,
                IsActive = true,
                UpdatedAt = DateTime.Parse("2026-10-10T11:03:40Z").ToUniversalTime()
            },
            new()
            {
                Id = "DOC_GUIDE_1",
                Title = "Đạm thực vật cho người ăn chay (Nhu cầu & Nguồn thực phẩm)",
                Author = "Ban Biên soạn Dinh dưỡng VeggieLife (Chuẩn WHO)",
                FileName = "01-dam-thuc-vat.pdf",
                FileType = "PDF",
                Chunks = 2,
                IsActive = true,
                UpdatedAt = DateTime.Parse("2026-10-10T00:00:00Z").ToUniversalTime()
            },
            new()
            {
                Id = "DOC_GUIDE_2",
                Title = "Vitamin B12 cho người ăn chay và thuần chay (Liều lượng & Bổ sung)",
                Author = "Ban Biên soạn Dinh dưỡng VeggieLife",
                FileName = "02-vitamin-b12.pdf",
                FileType = "PDF",
                Chunks = 2,
                IsActive = true,
                UpdatedAt = DateTime.Parse("2026-10-10T00:00:00Z").ToUniversalTime()
            },
            new()
            {
                Id = "DOC_GUIDE_3",
                Title = "Sắt từ thực phẩm chay và cách tăng hấp thu sinh học",
                Author = "Ban Biên soạn Dinh dưỡng VeggieLife",
                FileName = "03-sat-thuc-vat.pdf",
                FileType = "PDF",
                Chunks = 2,
                IsActive = true,
                UpdatedAt = DateTime.Parse("2026-10-10T00:00:00Z").ToUniversalTime()
            },
            new()
            {
                Id = "DOC_GUIDE_4",
                Title = "Canxi và Vitamin D trong chế độ thuần chay",
                Author = "Ban Biên soạn Dinh dưỡng VeggieLife",
                FileName = "04-canxi-vitamin-d.pdf",
                FileType = "PDF",
                Chunks = 2,
                IsActive = true,
                UpdatedAt = DateTime.Parse("2026-10-10T00:00:00Z").ToUniversalTime()
            },
            new()
            {
                Id = "DOC_GUIDE_5",
                Title = "Axit béo Omega-3 từ thực vật và vi tảo biển",
                Author = "Ban Biên soạn Dinh dưỡng VeggieLife",
                FileName = "05-omega-3.pdf",
                FileType = "PDF",
                Chunks = 2,
                IsActive = true,
                UpdatedAt = DateTime.Parse("2026-10-10T00:00:00Z").ToUniversalTime()
            },
            new()
            {
                Id = "DOC_GUIDE_6",
                Title = "Vi chất Kẽm và I-ốt trong chế độ ăn chay",
                Author = "Ban Biên soạn Dinh dưỡng VeggieLife",
                FileName = "06-kem-iot.pdf",
                FileType = "PDF",
                Chunks = 1,
                IsActive = true,
                UpdatedAt = DateTime.Parse("2026-10-10T00:00:00Z").ToUniversalTime()
            },
            new()
            {
                Id = "DOC_GUIDE_7",
                Title = "Dinh dưỡng thuần chay cho Trẻ em và Trẻ vị thành niên",
                Author = "Ban Biên soạn Dinh dưỡng VeggieLife",
                FileName = "07-tre-em-an-chay.pdf",
                FileType = "PDF",
                Chunks = 2,
                IsActive = true,
                UpdatedAt = DateTime.Parse("2026-10-10T00:00:00Z").ToUniversalTime()
            },
            new()
            {
                Id = "DOC_GUIDE_8",
                Title = "Chế độ ăn chay khi mang thai và nuôi con bằng sữa mẹ",
                Author = "Ban Biên soạn Dinh dưỡng VeggieLife",
                FileName = "08-thai-ky-cho-con-bu.pdf",
                FileType = "PDF",
                Chunks = 2,
                IsActive = true,
                UpdatedAt = DateTime.Parse("2026-10-10T00:00:00Z").ToUniversalTime()
            },
            new()
            {
                Id = "DOC_GUIDE_9",
                Title = "Dinh dưỡng ăn chay cho Người cao tuổi",
                Author = "Ban Biên soạn Dinh dưỡng VeggieLife",
                FileName = "09-nguoi-cao-tuoi.pdf",
                FileType = "PDF",
                Chunks = 2,
                IsActive = true,
                UpdatedAt = DateTime.Parse("2026-10-10T00:00:00Z").ToUniversalTime()
            },
            new()
            {
                Id = "DOC_GUIDE_10",
                Title = "Dinh dưỡng thể thao và phục hồi cơ bắp cho người ăn chay",
                Author = "Ban Biên soạn Dinh dưỡng VeggieLife",
                FileName = "10-the-thao-an-chay.pdf",
                FileType = "PDF",
                Chunks = 2,
                IsActive = true,
                UpdatedAt = DateTime.Parse("2026-10-10T00:00:00Z").ToUniversalTime()
            },
            new()
            {
                Id = "DOC_GUIDE_11",
                Title = "Quản lý cân nặng lành mạnh với chế độ ăn chay",
                Author = "Ban Biên soạn Dinh dưỡng VeggieLife",
                FileName = "11-quan-ly-can-nang.pdf",
                FileType = "PDF",
                Chunks = 2,
                IsActive = true,
                UpdatedAt = DateTime.Parse("2026-10-10T00:00:00Z").ToUniversalTime()
            },
            new()
            {
                Id = "DOC_GUIDE_12",
                Title = "Hướng dẫn đọc nhãn thực phẩm và tra cứu mã phụ gia E-number",
                Author = "Ban Biên soạn Dinh dưỡng VeggieLife",
                FileName = "12-doc-nhan-thuc-pham-e-number.pdf",
                FileType = "PDF",
                Chunks = 2,
                IsActive = true,
                UpdatedAt = DateTime.Parse("2026-10-10T00:00:00Z").ToUniversalTime()
            },
            new()
            {
                Id = "DOC_GUIDE_13",
                Title = "Kỹ thuật thay thế thịt, trứng, sữa trong ẩm thực chay",
                Author = "Ban Biên soạn Dinh dưỡng VeggieLife",
                FileName = "13-thay-the-thit-trung-sua.pdf",
                FileType = "PDF",
                Chunks = 2,
                IsActive = true,
                UpdatedAt = DateTime.Parse("2026-10-10T00:00:00Z").ToUniversalTime()
            },
            new()
            {
                Id = "DOC_GUIDE_14",
                Title = "Tổng quan các trường phái ăn chay phổ biến trên thế giới",
                Author = "Ban Biên soạn Dinh dưỡng VeggieLife",
                FileName = "14-cac-truong-phai-an-chay.pdf",
                FileType = "PDF",
                Chunks = 2,
                IsActive = true,
                UpdatedAt = DateTime.Parse("2026-10-10T00:00:00Z").ToUniversalTime()
            },
            new()
            {
                Id = "DOC_GUIDE_15",
                Title = "Quản lý và phòng ngừa dị ứng thực phẩm trong ăn chay",
                Author = "Ban Biên soạn Dinh dưỡng VeggieLife",
                FileName = "15-di-ung-thuc-pham-thuc-vat.pdf",
                FileType = "PDF",
                Chunks = 2,
                IsActive = true,
                UpdatedAt = DateTime.Parse("2026-10-10T00:00:00Z").ToUniversalTime()
            },
            new()
            {
                Id = "DOC_GUIDE_101",
                Title = "Tối ưu năng lượng cho vận động viên và người tập luyện thuần chay",
                Author = "D. Enette Larson-Meyer, PhD, RD",
                FileName = "101-nang-luong-van-dong-vien-chay.pdf",
                FileType = "PDF",
                Chunks = 2,
                IsActive = true,
                UpdatedAt = DateTime.Parse("2026-10-10T00:00:00Z").ToUniversalTime()
            },
            new()
            {
                Id = "DOC_GUIDE_102",
                Title = "Chiến lược bổ sung Carbohydrate cho người tập luyện",
                Author = "D. Enette Larson-Meyer, PhD, RD",
                FileName = "102-carbohydrate-cho-van-dong-vien-chay.pdf",
                FileType = "PDF",
                Chunks = 2,
                IsActive = true,
                UpdatedAt = DateTime.Parse("2026-10-10T00:00:00Z").ToUniversalTime()
            },
            new()
            {
                Id = "DOC_GUIDE_103",
                Title = "Chất béo lành mạnh trong khẩu phần người vận động thể lực",
                Author = "D. Enette Larson-Meyer, PhD, RD",
                FileName = "103-chat-beo-cho-van-dong-vien-chay.pdf",
                FileType = "PDF",
                Chunks = 2,
                IsActive = true,
                UpdatedAt = DateTime.Parse("2026-10-10T00:00:00Z").ToUniversalTime()
            },
            new()
            {
                Id = "DOC_GUIDE_104",
                Title = "Bảo vệ mật độ xương cho vận động viên ăn chay",
                Author = "D. Enette Larson-Meyer, PhD, RD",
                FileName = "104-suc-khoe-xuong-van-dong-vien-chay.pdf",
                FileType = "PDF",
                Chunks = 2,
                IsActive = true,
                UpdatedAt = DateTime.Parse("2026-10-10T00:00:00Z").ToUniversalTime()
            },
            new()
            {
                Id = "DOC_GUIDE_105",
                Title = "Phòng ngừa thiếu sắt và thiếu máu ở người tập luyện thể thao",
                Author = "D. Enette Larson-Meyer, PhD, RD",
                FileName = "105-thieu-sat-o-van-dong-vien-chay.pdf",
                FileType = "PDF",
                Chunks = 2,
                IsActive = true,
                UpdatedAt = DateTime.Parse("2026-10-10T00:00:00Z").ToUniversalTime()
            }
        };
    }
}

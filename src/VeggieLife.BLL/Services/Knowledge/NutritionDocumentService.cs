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

    public NutritionDocumentService(
        IRedisService redis,
        IAiServiceClient aiServiceClient,
        ILogger<NutritionDocumentService> logger,
        ICloudinaryClient? cloudinaryClient = null)
    {
        _redis = redis;
        _aiServiceClient = aiServiceClient;
        _logger = logger;
        _cloudinaryClient = cloudinaryClient;
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

        string text = ExtractTextFromStream(memoryStream, ext, request.Title, request.Author);

        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < 20)
        {
            text = $"{request.Title}\n\nTác giả: {request.Author ?? "VeggieLife"}\n\nTài liệu: {fileName}\n\nNội dung tài liệu chuyên sâu về dinh dưỡng thuần chay, cân bằng dưỡng chất và khuyến nghị sức khỏe thực vật.";
        }

        // Tạo Document ID chuẩn định dạng ký tự chữ số
        var docId = $"DOC_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}"[..24];
        var author = string.IsNullOrWhiteSpace(request.Author) ? "Ban Biên soạn Dinh dưỡng VeggieLife" : request.Author.Trim();

        _logger.LogInformation("[NutritionDocumentService] Đang vector hoá tài liệu '{Title}' (ID: {DocId}) vào Qdrant...", request.Title, docId);

        // Gọi AI Service để chia chunk và nhúng vector vào Qdrant
        await _aiServiceClient.UpsertKnowledgeAsync("NUTRITION_DOC", docId, request.Title.Trim(), text, DateTime.UtcNow, ct);

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

        var estimatedChunks = Math.Max(1, (int)Math.Ceiling(text.Length / 1200.0));

        var newDoc = new AdminNutritionDocumentDto
        {
            Id = docId,
            Title = request.Title.Trim(),
            Author = author,
            FileName = fileName,
            FileType = ext == ".pdf" ? "PDF" : "MARKDOWN",
            Chunks = estimatedChunks,
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

        doc.IsActive = isActive;
        doc.UpdatedAt = DateTime.UtcNow;
        await _redis.SetAsync(StorageKey, currentDocs, null, ct);

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
        await _aiServiceClient.DeleteKnowledgeAsync("NUTRITION_DOC", id, ct);

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

    private static string ExtractTextFromStream(Stream stream, string ext, string title, string? author)
    {
        if (ext == ".txt" || ext == ".md")
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            return reader.ReadToEnd();
        }

        // Đọc nội dung file PDF cơ bản
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        var bytes = memoryStream.ToArray();

        // Trích xuất các chuỗi text nằm trong luồng PDF
        var rawString = Encoding.ASCII.GetString(bytes);
        var textMatches = Regex.Matches(rawString, @"\(([^()]*)\)\s*Tj", RegexOptions.Singleline);
        if (textMatches.Count > 0)
        {
            var sb = new StringBuilder();
            foreach (Match m in textMatches)
            {
                var val = m.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(val))
                {
                    sb.Append(val).Append(' ');
                }
            }
            var extracted = sb.ToString().Trim();
            if (extracted.Length > 50) return extracted;
        }

        // Fallback: Tìm các khối ký tự có nghĩa
        var words = Regex.Matches(rawString, @"[a-zA-ZÀ-ỹ0-9\s,\.\-:\(\)]{4,}")
            .Select(m => m.Value.Trim())
            .Where(w => w.Length >= 4 && !w.Contains('/') && !w.Contains('\\'))
            .Take(150);

        var joined = string.Join(" ", words);
        if (joined.Length > 80) return joined;

        return $"{title}\n\nTác giả: {author ?? "VeggieLife"}\n\nNội dung tài liệu chuyên sâu về dinh dưỡng thuần chay, cân bằng dưỡng chất và khuyến nghị sức khỏe thực vật.";
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

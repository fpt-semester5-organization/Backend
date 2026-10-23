using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.DietarySchools;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Validators.Admin;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.Admin;

/// <summary>
/// Triển khai dịch vụ Quản trị 07 trường phái ăn chay hệ thống (FN-ADM-10, BR-ADM-MAS-001, BR-SYS-MAS-01, D109).
/// </summary>
public class AdminDietarySchoolService : IAdminDietarySchoolService
{
    private const string CacheKeyDietarySchools = "dietary_schools_cache";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly AppDbContext _db;
    private readonly IRedisService _redis;
    private readonly ILogger<AdminDietarySchoolService> _logger;

    public AdminDietarySchoolService(
        AppDbContext db,
        IRedisService redis,
        ILogger<AdminDietarySchoolService> logger)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// FN-ADM-10: Lấy toàn bộ danh sách 07 trường phái ăn chay kèm quy tắc cấm JSON được phân tích cú pháp.
    /// </summary>
    public async Task<IReadOnlyList<AdminDietarySchoolItemDto>> GetDietarySchoolsAsync(CancellationToken ct = default)
    {
        var entities = await _db.DietarySchools
            .AsNoTracking()
            .OrderBy(ds => ds.DietarySchoolId)
            .ToListAsync(ct);

        var result = new List<AdminDietarySchoolItemDto>();
        foreach (var school in entities)
        {
            var rulesDto = ParseRules(school.ForbiddenFoodRules, school.SchoolCode);

            result.Add(new AdminDietarySchoolItemDto
            {
                DietarySchoolId = school.DietarySchoolId,
                SchoolCode = school.SchoolCode,
                SchoolName = school.SchoolName,
                ForbiddenFoodRules = rulesDto
            });
        }

        return result;
    }

    /// <summary>
    /// FN-ADM-10: Cập nhật tên hiển thị và quy tắc thực phẩm cấm của trường phái (không thêm/xóa trường phái).
    /// </summary>
    public async Task<UpdateAdminDietarySchoolResponseData> UpdateDietarySchoolAsync(
        int id,
        UpdateAdminDietarySchoolRequest request,
        int adminId,
        CancellationToken ct = default)
    {
        AdminDietarySchoolValidator.ValidateId(id);
        AdminDietarySchoolValidator.ValidateUpdate(request);

        var school = await _db.DietarySchools.FirstOrDefaultAsync(s => s.DietarySchoolId == id, ct);
        if (school == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrAdmNotFound, $"Không tìm thấy trường phái ăn chay với ID {id}.");
        }

        var oldValue = JsonSerializer.Serialize(new
        {
            school.DietarySchoolId,
            school.SchoolCode,
            school.SchoolName,
            school.ForbiddenFoodRules
        });

        if (request.SchoolName != null)
        {
            school.SchoolName = request.SchoolName.Trim();
        }

        if (request.ForbiddenFoodRules != null)
        {
            var normalizedGroups = request.ForbiddenFoodRules.ForbiddenFoodGroups
                .Select(g => g.Trim().ToUpperInvariant())
                .Distinct()
                .ToList();

            var rulesObject = new
            {
                forbiddenFoodGroups = normalizedGroups,
                forbidAllium = request.ForbiddenFoodRules.ForbidAllium,
                description = string.IsNullOrWhiteSpace(request.ForbiddenFoodRules.Description)
                    ? null
                    : request.ForbiddenFoodRules.Description.Trim()
            };

            school.ForbiddenFoodRules = JsonSerializer.Serialize(rulesObject, JsonOptions);
        }

        school.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var newValue = JsonSerializer.Serialize(new
        {
            school.DietarySchoolId,
            school.SchoolCode,
            school.SchoolName,
            school.ForbiddenFoodRules
        });

        // Ghi AuditLogs
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminId,
            ActorRole = UserRoles.Admin,
            Action = "UPDATE_DIETARY_SCHOOL",
            TargetTable = "DietarySchools",
            TargetId = school.DietarySchoolId.ToString(),
            OldValue = oldValue,
            NewValue = newValue,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        // Xóa bộ nhớ đệm phân tán dùng chung
        try
        {
            await _redis.RemoveAsync(CacheKeyDietarySchools, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi xóa bộ nhớ đệm {CacheKey}", CacheKeyDietarySchools);
        }

        return new UpdateAdminDietarySchoolResponseData
        {
            DietarySchoolId = school.DietarySchoolId
        };
    }

    private ForbiddenFoodRulesDto ParseRules(string? rawJson, string schoolCode)
    {
        var rulesDto = new ForbiddenFoodRulesDto();
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return rulesDto;
        }

        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            if (doc.RootElement.TryGetProperty("forbiddenFoodGroups", out var groupsElem) &&
                groupsElem.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in groupsElem.EnumerateArray())
                {
                    var str = item.GetString();
                    if (!string.IsNullOrEmpty(str))
                    {
                        rulesDto.ForbiddenFoodGroups.Add(str.ToUpperInvariant());
                    }
                }
            }

            if (doc.RootElement.TryGetProperty("forbidAllium", out var alliumElem) &&
                (alliumElem.ValueKind == JsonValueKind.True || alliumElem.ValueKind == JsonValueKind.False))
            {
                rulesDto.ForbidAllium = alliumElem.GetBoolean();
            }

            if (doc.RootElement.TryGetProperty("description", out var descElem) &&
                descElem.ValueKind == JsonValueKind.String)
            {
                rulesDto.Description = descElem.GetString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi phân tích cú pháp ForbiddenFoodRules cho trường phái {Code}", schoolCode);
        }

        return rulesDto;
    }
}

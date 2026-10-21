using System;
using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Admin.Users;

/// <summary>
/// DTO phản hồi danh sách nội dung và thống kê vi phạm của người dùng (FN-ADM-03).
/// Khớp chuẩn response data: { violationSummary, items }
/// </summary>
public class AdminUserContentsResponseData
{
    public int UserId { get; set; }

    public string AccountStatus { get; set; } = string.Empty;

    public DateTime? LockedUntil { get; set; }

    public int CountViolationReports { get; set; }

    public AdminUserViolationSummaryDto ViolationSummary { get; set; } = new();

    public IReadOnlyList<AdminUserContentItemDto> Items { get; set; } = Array.Empty<AdminUserContentItemDto>();
}

/// <summary>
/// Thống kê phiếu báo cáo vi phạm liên quan đến nội dung của người dùng.
/// </summary>
public class AdminUserViolationSummaryDto
{
    /// <summary>
    /// Số lượng vi phạm đã được Admin phê duyệt (Status = APPROVE).
    /// </summary>
    public int Resolved { get; set; }

    /// <summary>
    /// Số lượng báo cáo đã bị Admin bác bỏ (Status = REJECT).
    /// </summary>
    public int Dismissed { get; set; }
}

/// <summary>
/// DTO chi tiết một bài viết, công thức, video hoặc bình luận của người dùng.
/// </summary>
public class AdminUserContentItemDto
{
    /// <summary>
    /// Loại nội dung: POST | RECIPE | VIDEO | COMMENT
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Mã ID của bài viết, công thức, video hoặc bình luận.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Tiêu đề bài viết / công thức / video hoặc trích đoạn bình luận.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Trạng thái kiểm duyệt hoặc trạng thái hiển thị của nội dung (APPROVED_ACTIVE, QUARANTINED_HIDDEN, v.v.).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Thời điểm khởi tạo nội dung.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

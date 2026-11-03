using VeggieLife.BLL.Common.Responses;

namespace VeggieLife.BLL.DTOs.Admin.Surveys;

/// <summary>
/// Phản hồi danh sách khảo sát phân trang kèm số liệu tổng hợp (FN-ADM-15).
/// </summary>
public class AdminSurveyListResponse
{
    public PagedResult<AdminSurveyDto> Surveys { get; set; } = new();
    public AdminSurveySummaryDto Summary { get; set; } = new();
}

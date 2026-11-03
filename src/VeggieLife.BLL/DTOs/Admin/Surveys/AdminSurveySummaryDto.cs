namespace VeggieLife.BLL.DTOs.Admin.Surveys;

/// <summary>
/// DTO thống kê tổng hợp chất lượng khảo sát Chatbot AI (FN-ADM-15).
/// </summary>
public class AdminSurveySummaryDto
{
    public int TotalSurveys { get; set; }
    public double AverageRating { get; set; }
    public int PositiveCount { get; set; }
    public double PositiveRate { get; set; }
    public int NegativeCount { get; set; }
    public double NegativeRate { get; set; }
    public int NeutralCount { get; set; }
}

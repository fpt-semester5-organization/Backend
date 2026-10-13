namespace VeggieLife.BLL.DTOs.Ai;

/// <summary>
/// Nguồn tham chiếu do AI Service truy xuất từ Qdrant (D118, D119).
/// </summary>
public class AiSourceDto
{
    public string Type { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public double? Score { get; set; }
}

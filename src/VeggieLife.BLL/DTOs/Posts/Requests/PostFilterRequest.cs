namespace VeggieLife.BLL.DTOs.Posts.Requests;

/// <summary>
/// DTO chứa các tham số truy vấn tìm kiếm, lọc và phân trang bài viết chia sẻ cộng đồng (FN-POST-05).
/// </summary>
public class PostFilterRequest
{
    /// <summary>
    /// Từ khóa tìm kiếm bài viết theo tiêu đề hoặc nội dung (1–100 ký tự).
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// Lọc theo mã tác giả (> 0).
    /// </summary>
    public int? AuthorId { get; set; }

    /// <summary>
    /// Tiêu chí sắp xếp: NEWEST (Mặc định) hoặc POPULAR (theo số lượt vote giảm dần).
    /// </summary>
    public string? Sort { get; set; }

    /// <summary>
    /// Số thứ tự trang (>= 1, mặc định: 1).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Kích thước số lượng bản ghi mỗi trang (1–20, mặc định: 10).
    /// </summary>
    public int Size { get; set; } = 10;
}

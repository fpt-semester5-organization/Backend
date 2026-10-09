namespace VeggieLife.BLL.Common.Responses;

/// <summary>
/// Đối tượng thông tin phân trang chuẩn Envelope D52: { page, size, total }.
/// </summary>
public class PagedMeta
{
    public int Page { get; set; }
    public int Size { get; set; }
    public int Total { get; set; }

    public PagedMeta()
    {
    }

    public PagedMeta(int page, int size, int total)
    {
        Page = page;
        Size = size;
        Total = total;
    }
}

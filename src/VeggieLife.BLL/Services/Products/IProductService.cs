using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Products.Responses;

namespace VeggieLife.BLL.Services.Products;

/// <summary>
/// Interface dịch vụ tra cứu thông tin sản phẩm và lịch sử quét mã vạch (FN-BAR-01, FN-BAR-02).
/// Tuân thủ các quy tắc: BR-GUEST-07, BR-USR-09, BR-ADM-MAS-002, BR-ADM-AI-005.
/// </summary>
public interface IProductService
{
    /// <summary>
    /// FN-BAR-01: Tra cứu sản phẩm đóng gói theo mã vạch (EAN-8/13, UPC-A), kiểm tra phụ gia E-Numbers và hạn mức Khách.
    /// </summary>
    Task<BarcodeScanResponse> ScanBarcodeAsync(
        string barcode,
        int? userId,
        string? guestId = null,
        string method = "CAMERA_BARCODE",
        CancellationToken ct = default);

    /// <summary>
    /// FN-BAR-02: Lấy lịch sử quét mã vạch của Thành viên kèm kết luận và cảnh báo dị ứng.
    /// </summary>
    Task<PagedResult<UserScanHistoryResponse>> GetUserScanHistoryAsync(
        int userId,
        string? verdict = null,
        bool? allergenOnly = null,
        int page = 1,
        int size = 20,
        CancellationToken ct = default);
}

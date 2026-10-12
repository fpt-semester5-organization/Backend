using System.Threading.Tasks;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Products.Responses;

namespace VeggieLife.BLL.Services.Products
{
    public interface IProductService
    {
        Task<BarcodeScanResponse> ScanBarcodeAsync(string barcode, int? userId);
        Task<PagedResult<UserScanHistoryResponse>> GetUserScanHistoryAsync(int userId, string? verdict, bool? allergenOnly, int page, int size);
    }
}

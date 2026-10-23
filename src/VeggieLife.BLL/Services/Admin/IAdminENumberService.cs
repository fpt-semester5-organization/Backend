using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.ENumbers;

namespace VeggieLife.BLL.Services.Admin;

/// <summary>
/// Giao diện dịch vụ Quản trị từ điển phụ gia thực phẩm E-number (FN-ADM-11, BR-ADM-MAS-002, BR-ADM-AI-005).
/// </summary>
public interface IAdminENumberService
{
    /// <summary>
    /// FN-ADM-11: Tra cứu danh sách mã phụ gia E-number có phân trang và bộ lọc theo trạng thái chay, danh mục, từ khóa.
    /// </summary>
    Task<(IReadOnlyList<AdminENumberListItemDto> Items, PagedMeta Meta)> GetENumbersAsync(
        AdminENumberFilterRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// FN-ADM-11: Lấy chi tiết thông tin một mã phụ gia E-number theo mã Code.
    /// </summary>
    Task<AdminENumberListItemDto> GetENumberByCodeAsync(
        string code,
        CancellationToken ct = default);

    /// <summary>
    /// FN-ADM-11: Thêm mới mã phụ gia E-number vào từ điển, ghi nhận AuditLog và nạp vào kho tri thức chatbot.
    /// </summary>
    Task<CreateAdminENumberResponseData> CreateENumberAsync(
        CreateAdminENumberRequest request,
        int adminId,
        CancellationToken ct = default);

    /// <summary>
    /// FN-ADM-11: Cập nhật thông tin mã phụ gia E-number. Nếu đổi trạng thái chay (Vegan_Status), kích hoạt tính lại kết luận sản phẩm liên quan (bỏ qua Admin_Verified = 1) và cập nhật kho tri thức chatbot.
    /// </summary>
    Task<UpdateAdminENumberResponseData> UpdateENumberAsync(
        string code,
        UpdateAdminENumberRequest request,
        int adminId,
        CancellationToken ct = default);
}

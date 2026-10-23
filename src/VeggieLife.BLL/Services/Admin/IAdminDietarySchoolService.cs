using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.DTOs.Admin.DietarySchools;

namespace VeggieLife.BLL.Services.Admin;

/// <summary>
/// Giao diện dịch vụ Quản trị 07 trường phái ăn chay hệ thống (FN-ADM-10, BR-ADM-MAS-001).
/// </summary>
public interface IAdminDietarySchoolService
{
    /// <summary>
    /// FN-ADM-10: Lấy toàn bộ danh sách 07 trường phái ăn chay cốt lõi kèm quy tắc cấm.
    /// </summary>
    Task<IReadOnlyList<AdminDietarySchoolItemDto>> GetDietarySchoolsAsync(CancellationToken ct = default);

    /// <summary>
    /// FN-ADM-10: Cập nhật tên hiển thị và quy tắc thực phẩm cấm của trường phái (không thêm/xóa trường phái).
    /// </summary>
    Task<UpdateAdminDietarySchoolResponseData> UpdateDietarySchoolAsync(
        int id,
        UpdateAdminDietarySchoolRequest request,
        int adminId,
        CancellationToken ct = default);
}

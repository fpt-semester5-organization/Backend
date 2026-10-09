using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;

namespace VeggieLife.API.Authorization;

/// <summary>
/// Bộ lọc xác thực và phân quyền bắt buộc người dùng phải là Thành viên chính thức (MEMBER, VIP, ADMIN)
/// và chặn triệt để Khách vãng lai (GUEST), trả về thông báo mời đăng ký tương ứng (FN-AUTH-03).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RequireMemberAttribute : Attribute, IAsyncAuthorizationFilter
{
    /// <summary>
    /// Loại tính năng để phân định mã lỗi và thông báo (Community vs Personalization).
    /// </summary>
    public MemberFeatureType FeatureType { get; set; }

    public RequireMemberAttribute(MemberFeatureType featureType = MemberFeatureType.General)
    {
        FeatureType = featureType;
    }

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        // 1. Kiểm tra trạng thái xác thực
        if (user?.Identity == null || !user.Identity.IsAuthenticated)
        {
            throw AppException.Unauthorized(
                ErrorCodes.ErrAuthInvalidToken,
                "Yêu cầu chưa được xác thực hoặc phiên làm việc đã hết hạn. Vui lòng đăng nhập hoặc đăng ký tài khoản.");
        }

        // 2. Kiểm tra vai trò GUEST
        var isGuest = user.IsInRole(UserRoles.Guest) ||
                      user.Claims.Any(c => 
                          (c.Type == ClaimTypes.Role || c.Type == "role" || c.Type.EndsWith("/role")) &&
                          string.Equals(c.Value, UserRoles.Guest, StringComparison.OrdinalIgnoreCase));

        if (isGuest)
        {
            switch (FeatureType)
            {
                case MemberFeatureType.Community:
                    throw AppException.Forbidden(
                        ErrorCodes.ErrGuestCommunityActionDenied,
                        "Khách vãng lai thực hiện thao tác ghi cộng đồng (FN-AUTH-03). Vui lòng đăng ký tài khoản để thích, bình luận và chia sẻ bài viết.");

                case MemberFeatureType.Personalization:
                    throw AppException.Forbidden(
                        ErrorCodes.ErrGuestPersonalizationRestricted,
                        "Khách vãng lai gọi tính năng cá nhân hóa (FN-AUTH-03). Vui lòng đăng ký tài khoản để lưu trữ thông tin và theo dõi sức khỏe cá nhân.");

                default:
                    throw AppException.Forbidden(
                        ErrorCodes.ErrGuestPersonalizationRestricted,
                        "Tính năng chỉ dành cho thành viên chính thức (FN-AUTH-03). Vui lòng đăng ký tài khoản để tiếp tục.");
            }
        }

        // 3. Cho phép MEMBER, VIP, ADMIN tiếp tục
        return Task.CompletedTask;
    }
}

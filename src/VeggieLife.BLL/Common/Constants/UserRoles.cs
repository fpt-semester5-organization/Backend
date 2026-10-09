namespace VeggieLife.BLL.Common.Constants;

/// <summary>
/// Các hằng số vai trò người dùng trong hệ thống VeggieLife.
/// Role trong DB gồm: MEMBER, VIP, ADMIN.
/// GUEST là vai trò danh nghĩa được gắn qua Anonymous JWT Claim (theo BR-GUEST-AUTH-01).
/// </summary>
public static class UserRoles
{
    public const string Guest = "GUEST";
    public const string Member = "MEMBER";
    public const string Vip = "VIP";
    public const string Admin = "ADMIN";
}

/// <summary>
/// Các tên chính sách phân quyền (Authorization Policies) sử dụng trong [Authorize(Policy = ...)].
/// </summary>
public static class PolicyNames
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireVip = "RequireVip";
    public const string RequireMember = "RequireMember";
    public const string RequireGuestOrAbove = "RequireGuestOrAbove";
}

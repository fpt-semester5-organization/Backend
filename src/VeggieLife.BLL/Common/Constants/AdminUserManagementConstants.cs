namespace VeggieLife.BLL.Common.Constants;

/// <summary>
/// Các hành động kỷ luật người dùng do Quản trị viên thực hiện (FN-ADM-02, BR-ADM-USR-002, BR-ADM-SOC-001).
/// </summary>
public static class AdminSanctionActions
{
    public const string Warn = "WARN";
    public const string RestrictComment = "RESTRICT_COMMENT";
    public const string Suspend = "SUSPEND";
    public const string Ban = "BAN";
    public const string Reactivate = "REACTIVATE";
}

/// <summary>
/// Các loại nội dung của người dùng mà Quản trị viên có thể xem lịch sử vi phạm (FN-ADM-03).
/// </summary>
public static class AdminUserContentTypes
{
    public const string Post = "POST";
    public const string Recipe = "RECIPE";
    public const string Video = "VIDEO";
    public const string Comment = "COMMENT";
}

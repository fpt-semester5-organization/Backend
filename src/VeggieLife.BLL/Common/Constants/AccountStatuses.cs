namespace VeggieLife.BLL.Common.Constants;

/// <summary>
/// Các hằng số trạng thái tài khoản người dùng theo quy tắc nghiệp vụ BR-ADM-USR-002.
/// </summary>
public static class AccountStatuses
{
    public const string PendingActivation = "PENDING_ACTIVATION";
    public const string Active = "ACTIVE";
    public const string Quarantined = "QUARANTINED";
    public const string Suspended = "SUSPENDED";
}

/// <summary>
/// Mục đích phát hành mã OTP qua Email.
/// </summary>
public static class OtpPurposes
{
    public const string EmailVerify = "EMAIL_VERIFY";
    public const string PasswordReset = "PASSWORD_RESET";
}

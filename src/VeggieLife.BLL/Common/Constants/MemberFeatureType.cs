namespace VeggieLife.BLL.Common.Constants;

/// <summary>
/// Phân loại tính năng thành viên để phân định lý do từ chối truy cập đối với Khách vãng lai (FN-AUTH-03).
/// </summary>
public enum MemberFeatureType
{
    /// <summary>
    /// Tính năng thành viên chung (yêu cầu đăng ký tài khoản).
    /// </summary>
    General = 0,

    /// <summary>
    /// Thao tác ghi cộng đồng: thích, bình luận, đăng bài, chia sẻ (ERR_GUEST_COMMUNITY_ACTION_DENIED).
    /// </summary>
    Community = 1,

    /// <summary>
    /// Thao tác cá nhân hóa: hồ sơ sức khỏe BMI/TDEE, tủ lạnh ảo, thực đơn cá nhân (ERR_GUEST_PERSONALIZATION_RESTRICTED).
    /// </summary>
    Personalization = 2
}

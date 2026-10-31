using System;

namespace VeggieLife.BLL.DTOs.Auth;

/// <summary>
/// Dữ liệu phản hồi danh mục tài liệu và thỏa thuận pháp lý của hệ thống VeggieLife (FN-AUTH-04).
/// Cung cấp liên kết ảnh độ phân giải cao phục vụ người dùng xem khi đăng ký tài khoản.
/// </summary>
public class LegalDocumentsResponseData
{
    /// <summary>
    /// Đường dẫn ảnh văn bản Bản Cam Kết Và Thỏa Thuận Đăng Ký Người Dùng.
    /// </summary>
    public string UserAgreementUrl { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn ảnh văn bản Chính Sách Bảo Mật Dữ Liệu VeggieLife.
    /// </summary>
    public string PrivacyPolicyUrl { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn ảnh văn bản Điều Khoản Sử Dụng Dịch Vụ VeggieLife.
    /// </summary>
    public string TermsOfServiceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn ảnh văn bản Quy Chuẩn Ẩm Thực Chay Và Miễn Trừ Y Tế VeggieLife.
    /// </summary>
    public string VeganDisclaimerUrl { get; set; } = string.Empty;

    /// <summary>
    /// Khuyến nghị miễn trừ y khoa chuẩn hệ thống theo quy định D108.
    /// </summary>
    public string MedicalDisclaimer { get; set; } = "Khuyến nghị mang tính chất tham khảo dinh dưỡng, vui lòng tham vấn ý kiến bác sĩ chuyên khoa đối với các bệnh lý nền.";

    /// <summary>
    /// Thời điểm cập nhật tài liệu gần nhất (UTC).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}


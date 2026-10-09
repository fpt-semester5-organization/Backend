namespace VeggieLife.BLL.Common.Constants;

/// <summary>
/// Các phân loại hạn mức dùng thử dành cho Khách vãng lai (FN-AUTH-02).
/// </summary>
public enum GuestTrialType
{
    /// <summary>
    /// Dùng thử tính năng AI (Chatbot, gợi ý món, nhận diện tủ lạnh, xem STT video): tối đa 03 lượt/phiên.
    /// </summary>
    Ai = 1,

    /// <summary>
    /// Tra cứu sản phẩm đóng gói bằng mã vạch camera: tối đa 03 lần/ngày.
    /// </summary>
    BarcodeDaily = 2,

    /// <summary>
    /// Tìm kiếm quán chay trên bản đồ GPS: tối đa 03 lần.
    /// </summary>
    MapSearch = 3
}

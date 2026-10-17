namespace VeggieLife.BLL.Common.Constants;

/// <summary>
/// Các phân loại hạn mức dùng thử dành cho Khách vãng lai (FN-AUTH-02).
/// </summary>
public enum GuestTrialType
{
    /// <summary>
    /// Dùng thử trợ lý Chatbot AI tư vấn dinh dưỡng: tối đa 03 lượt/phiên.
    /// </summary>
    Ai = 1,
    Chatbot = 1,

    /// <summary>
    /// Tra cứu sản phẩm đóng gói bằng mã vạch camera: tối đa 03 lần/ngày.
    /// </summary>
    BarcodeDaily = 2,

    /// <summary>
    /// Tìm kiếm quán chay trên bản đồ GPS: tối đa 03 lần.
    /// </summary>
    MapSearch = 3,

    /// <summary>
    /// Dùng thử quét ảnh nhận diện nguyên liệu tủ lạnh (Vision AI): tối đa 03 lượt/phiên.
    /// </summary>
    FridgeVision = 4,

    /// <summary>
    /// Dùng thử xem bản phiên âm / bóc tách công thức từ video (Whisper STT): tối đa 03 lượt/phiên.
    /// </summary>
    VideoStt = 5
}

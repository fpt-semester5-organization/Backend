using System;

namespace VeggieLife.DAL.External.Cloudinary;

/// <summary>
/// Ngoại lệ phát sinh trong quá trình giao tiếp với dịch vụ Cloudinary CDN tại tầng DAL.
/// Được tầng BLL bắt và chuyển đổi thành AppException với mã lỗi 502 (ERR_EXT_CLOUDINARY_FAILED).
/// </summary>
public class CloudinaryException : Exception
{
    public CloudinaryException(string message) : base(message)
    {
    }

    public CloudinaryException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

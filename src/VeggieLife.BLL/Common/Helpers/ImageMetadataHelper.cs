using System;
using System.IO;

namespace VeggieLife.BLL.Common.Helpers;

/// <summary>
/// Tiện ích đọc siêu dữ liệu tệp hình ảnh (định dạng, kích thước width x height, magic bytes)
/// không phụ thuộc vào thư viện ngoài, tối ưu hiệu năng và an toàn bộ nhớ.
/// </summary>
public static class ImageMetadataHelper
{
    /// <summary>
    /// Kiểm tra chữ ký magic bytes của tệp ảnh (JPEG, PNG, WEBP).
    /// </summary>
    public static bool IsValidImageSignature(byte[] bytes, string extension)
    {
        if (bytes == null || bytes.Length < 12)
        {
            return false;
        }

        var ext = extension.Trim().ToLowerInvariant();

        // JPEG: FF D8 FF
        if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return ext == ".jpg" || ext == ".jpeg";
        }

        // PNG: 89 50 4E 47 (0x89 'P' 'N' 'G')
        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
        {
            return ext == ".png";
        }

        // WEBP: "RIFF" ở bytes 0..3 và "WEBP" ở bytes 8..11
        if (bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
            bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
        {
            return ext == ".webp";
        }

        return false;
    }

    /// <summary>
    /// Trích xuất kích thước chiều rộng (Width) và chiều cao (Height) từ luồng nhị phân của ảnh (PNG, JPEG, WEBP).
    /// </summary>
    public static bool TryGetImageDimensions(Stream stream, out int width, out int height)
    {
        width = 0;
        height = 0;

        if (stream == null || !stream.CanSeek || stream.Length < 16)
        {
            return false;
        }

        var originalPosition = stream.Position;
        try
        {
            stream.Position = 0;
            var header = new byte[16];
            var bytesRead = stream.Read(header, 0, header.Length);
            if (bytesRead < 16)
            {
                return false;
            }

            // 1. Định dạng PNG
            if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
            {
                stream.Position = 16;
                var dimBuffer = new byte[8];
                if (stream.Read(dimBuffer, 0, 8) == 8)
                {
                    width = (dimBuffer[0] << 24) | (dimBuffer[1] << 16) | (dimBuffer[2] << 8) | dimBuffer[3];
                    height = (dimBuffer[4] << 24) | (dimBuffer[5] << 16) | (dimBuffer[6] << 8) | dimBuffer[7];
                    return width > 0 && height > 0;
                }
                return false;
            }

            // 2. Định dạng WEBP
            if (header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
            {
                stream.Position = 12;
                var chunkHeader = new byte[18];
                var readChunk = stream.Read(chunkHeader, 0, chunkHeader.Length);
                if (readChunk < 18)
                {
                    return false;
                }

                var chunkType = System.Text.Encoding.ASCII.GetString(chunkHeader, 0, 4);

                // VP8 (Lossy)
                if (chunkType == "VP8 ")
                {
                    // Frame tag và start code 0x9D 0x01 0x2A ở offset 11..13 từ chunk data
                    if (chunkHeader.Length >= 18 && chunkHeader[11] == 0x9D && chunkHeader[12] == 0x01 && chunkHeader[13] == 0x2A)
                    {
                        width = (chunkHeader[14] | (chunkHeader[15] << 8)) & 0x3FFF;
                        height = (chunkHeader[16] | (chunkHeader[17] << 8)) & 0x3FFF;
                        return width > 0 && height > 0;
                    }
                }
                // VP8L (Lossless)
                else if (chunkType == "VP8L")
                {
                    if (chunkHeader[8] == 0x2F) // 1-byte signature
                    {
                        var b0 = chunkHeader[9];
                        var b1 = chunkHeader[10];
                        var b2 = chunkHeader[11];
                        var b3 = chunkHeader[12];
                        width = 1 + (b0 | ((b1 & 0x3F) << 8));
                        height = 1 + (((b1 >> 6) | (b2 << 2) | ((b3 & 0x0F) << 10)));
                        return width > 0 && height > 0;
                    }
                }
                // VP8X (Extended)
                else if (chunkType == "VP8X")
                {
                    var b12 = chunkHeader[12];
                    var b13 = chunkHeader[13];
                    var b14 = chunkHeader[14];
                    var b15 = chunkHeader[15];
                    var b16 = chunkHeader[16];
                    var b17 = chunkHeader[17];
                    width = 1 + (b12 | (b13 << 8) | (b14 << 16));
                    height = 1 + (b15 | (b16 << 8) | (b17 << 16));
                    return width > 0 && height > 0;
                }
            }

            // 3. Định dạng JPEG
            if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            {
                stream.Position = 2;
                while (stream.Position < stream.Length)
                {
                    int b = stream.ReadByte();
                    while (b == 0xFF && stream.Position < stream.Length)
                    {
                        b = stream.ReadByte();
                    }

                    if (b == -1) break;

                    // Các marker SOF (Start of Frame) chứa kích thước ảnh
                    // SOF0 (0xC0), SOF1 (0xC1), SOF2 (0xC2), SOF3 (0xC3), SOF5 (0xC5), SOF6 (0xC6), SOF7 (0xC7),
                    // SOF9 (0xC9), SOF10 (0xCA), SOF11 (0xCB), SOF13 (0xCD), SOF14 (0xCE), SOF15 (0xCF)
                    var isSof = (b >= 0xC0 && b <= 0xC3) || (b >= 0xC5 && b <= 0xC7) ||
                                (b >= 0xC9 && b <= 0xCB) || (b >= 0xCD && b <= 0xCF);

                    if (isSof)
                    {
                        var sofBuf = new byte[7];
                        if (stream.Read(sofBuf, 0, 7) == 7)
                        {
                            // sofBuf[0..1]: length
                            // sofBuf[2]: precision
                            // sofBuf[3..4]: height (big-endian)
                            // sofBuf[5..6]: width (big-endian)
                            height = (sofBuf[3] << 8) | sofBuf[4];
                            width = (sofBuf[5] << 8) | sofBuf[6];
                            return width > 0 && height > 0;
                        }
                        break;
                    }

                    // Nếu gặp SOS (Start of Scan - 0xDA) hoặc EOI (End of Image - 0xD9) thì dừng
                    if (b == 0xDA || b == 0xD9)
                    {
                        break;
                    }

                    // Bỏ qua payload của marker khác
                    var lenB1 = stream.ReadByte();
                    var lenB2 = stream.ReadByte();
                    if (lenB1 == -1 || lenB2 == -1) break;

                    var segmentLength = (lenB1 << 8) | lenB2;
                    if (segmentLength < 2) break;

                    stream.Seek(segmentLength - 2, SeekOrigin.Current);
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }
}

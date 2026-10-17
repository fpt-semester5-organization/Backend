using System;
using System.Buffers.Binary;
using System.IO;

namespace VeggieLife.BLL.Services.Media;

/// <summary>
/// Tiện ích đọc siêu dữ liệu tệp video định dạng MP4 / MOV (ISO BMFF).
/// Cho phép trích xuất sớm thời lượng video từ atom moov -> mvhd mà không cần thư viện ngoài.
/// </summary>
public static class Mp4MetadataHelper
{
    public static double? TryGetDurationSeconds(Stream stream)
    {
        if (stream == null || !stream.CanSeek || stream.Length < 16)
        {
            return null;
        }

        var originalPosition = stream.Position;
        try
        {
            stream.Position = 0;
            Span<byte> headerBuffer = stackalloc byte[8];
            Span<byte> extSizeBuf = stackalloc byte[8];

            while (stream.Position + 8 <= stream.Length)
            {
                if (stream.Read(headerBuffer) < 8)
                {
                    break;
                }

                var boxSize = BinaryPrimitives.ReadUInt32BigEndian(headerBuffer[..4]);
                var boxType = System.Text.Encoding.ASCII.GetString(headerBuffer.Slice(4, 4));

                long actualBoxSize = boxSize;
                long headerLength = 8;

                if (boxSize == 1) // 64-bit extended size
                {
                    if (stream.Read(extSizeBuf) < 8) break;
                    actualBoxSize = (long)BinaryPrimitives.ReadUInt64BigEndian(extSizeBuf);
                    headerLength = 16;
                }
                else if (boxSize == 0) // box extends to end of file
                {
                    actualBoxSize = stream.Length - (stream.Position - headerLength);
                }

                if (actualBoxSize < headerLength)
                {
                    break;
                }

                if (boxType == "moov")
                {
                    var moovPayloadLength = actualBoxSize - headerLength;
                    return ParseMoovAtom(stream, moovPayloadLength);
                }

                var skipBytes = actualBoxSize - headerLength;
                if (stream.Position + skipBytes > stream.Length)
                {
                    break;
                }
                stream.Seek(skipBytes, SeekOrigin.Current);
            }

            return null;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (stream.CanSeek)
            {
                stream.Position = originalPosition;
            }
        }
    }

    private static double? ParseMoovAtom(Stream stream, long moovLength)
    {
        var endPosition = stream.Position + moovLength;
        Span<byte> subHeader = stackalloc byte[8];

        while (stream.Position + 8 <= endPosition)
        {
            if (stream.Read(subHeader) < 8)
            {
                break;
            }

            var subSize = BinaryPrimitives.ReadUInt32BigEndian(subHeader[..4]);
            var subType = System.Text.Encoding.ASCII.GetString(subHeader.Slice(4, 4));

            if (subSize < 8) break;

            if (subType == "mvhd")
            {
                var payloadLength = (int)(subSize - 8);
                if (payloadLength < 20) return null;

                var mvhdData = new byte[payloadLength];
                if (stream.Read(mvhdData, 0, payloadLength) < payloadLength)
                {
                    return null;
                }

                byte version = mvhdData[0];
                if (version == 0)
                {
                    // Version 0:
                    // 0: version (1 byte)
                    // 1..3: flags (3 bytes)
                    // 4..7: creation_time (4 bytes)
                    // 8..11: modification_time (4 bytes)
                    // 12..15: timescale (4 bytes)
                    // 16..19: duration (4 bytes)
                    if (mvhdData.Length >= 20)
                    {
                        var timescale = BinaryPrimitives.ReadUInt32BigEndian(mvhdData.AsSpan(12, 4));
                        var duration = BinaryPrimitives.ReadUInt32BigEndian(mvhdData.AsSpan(16, 4));
                        if (timescale > 0)
                        {
                            return (double)duration / timescale;
                        }
                    }
                }
                else if (version == 1)
                {
                    // Version 1:
                    // 0: version (1 byte)
                    // 1..3: flags (3 bytes)
                    // 4..11: creation_time (8 bytes)
                    // 12..19: modification_time (8 bytes)
                    // 20..23: timescale (4 bytes)
                    // 24..31: duration (8 bytes)
                    if (mvhdData.Length >= 32)
                    {
                        var timescale = BinaryPrimitives.ReadUInt32BigEndian(mvhdData.AsSpan(20, 4));
                        var duration = BinaryPrimitives.ReadUInt64BigEndian(mvhdData.AsSpan(24, 8));
                        if (timescale > 0)
                        {
                            return (double)duration / timescale;
                        }
                    }
                }

                return null;
            }

            var skip = subSize - 8;
            if (stream.Position + skip > endPosition) break;
            stream.Seek(skip, SeekOrigin.Current);
        }

        return null;
    }
}

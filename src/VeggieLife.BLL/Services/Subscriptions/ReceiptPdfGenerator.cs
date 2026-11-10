using System;
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VeggieLife.BLL.DTOs.Subscriptions.Responses;

namespace VeggieLife.BLL.Services.Subscriptions;

/// <summary>
/// Dựng biên lai thanh toán VIP dạng PDF (FN-SUB-04) từ <see cref="PaymentReceiptDto"/>.
/// Dùng chung cho endpoint tải biên lai và tệp đính kèm email sau khi thanh toán thành công.
/// </summary>
public static class ReceiptPdfGenerator
{
    private const string PrimaryColor = "#3F5B45";
    private const string MutedColor = "#6B7280";
    private static readonly CultureInfo Vi = new("vi-VN");

    static ReceiptPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Tên tệp gợi ý khi tải về hoặc đính kèm email, ví dụ "VeggieLife-REC-20261105-000123.pdf".
    /// </summary>
    public static string FileName(PaymentReceiptDto receipt) => $"VeggieLife-{receipt.ReceiptNumber}.pdf";

    /// <summary>
    /// Tạo nội dung PDF của biên lai.
    /// </summary>
    public static byte[] Generate(PaymentReceiptDto receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(28);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor("#111827"));

                page.Header().Column(col =>
                {
                    col.Item().Text("VeggieLife").FontSize(20).Bold().FontColor(PrimaryColor);
                    col.Item().Text("Ẩm thực chay").FontSize(9).FontColor(MutedColor);
                    col.Item().PaddingTop(10).Text("BIÊN LAI THANH TOÁN").FontSize(14).Bold();
                    col.Item().Text($"Số biên lai: {receipt.ReceiptNumber}").FontSize(9).FontColor(MutedColor);
                });

                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Spacing(6);
                    Row(col, "Khách hàng", receipt.FullName);
                    Row(col, "Email", receipt.Email);
                    Row(col, "Gói dịch vụ", receipt.PlanName);
                    Row(col, "Thời hạn", $"{receipt.DurationDays} ngày");
                    Row(col, "Quyền lợi VIP", $"{receipt.VipStartsAt:dd/MM/yyyy} – {receipt.VipEndsAt:dd/MM/yyyy}");
                    Row(col, "Mã giao dịch", $"#{receipt.TransactionId}");
                    Row(col, "Mã đơn hàng", receipt.OrderCode);
                    Row(col, "Cổng thanh toán", receipt.Gateway);
                    Row(col, "Thời điểm thanh toán", ToVietnamTime(receipt.PaymentDate).ToString("dd/MM/yyyy HH:mm", Vi));

                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#E5E7EB");
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Tổng thanh toán").Bold().FontSize(12);
                        r.AutoItem().Text(string.Format(Vi, "{0:N0} đ", receipt.AmountVnd)).Bold().FontSize(14).FontColor(PrimaryColor);
                    });
                    col.Item().Text(receipt.Status == "SUCCESS" ? "Trạng thái: Đã thanh toán thành công" : $"Trạng thái: {receipt.Status}")
                        .FontSize(9).FontColor(MutedColor);
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Cảm ơn bạn đã sử dụng VeggieLife. Biên lai điện tử này được lập tự động, không cần chữ ký.")
                        .FontSize(8).FontColor(MutedColor);
                });
            });
        }).GeneratePdf();
    }

    private static void Row(QuestPDF.Fluent.ColumnDescriptor col, string label, string value)
    {
        col.Item().Row(r =>
        {
            r.ConstantItem(120).Text(label).FontColor(MutedColor);
            r.RelativeItem().Text(string.IsNullOrWhiteSpace(value) ? "-" : value);
        });
    }

    private static DateTime ToVietnamTime(DateTime utc)
    {
        var asUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(asUtc, tz);
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
                return TimeZoneInfo.ConvertTimeFromUtc(asUtc, tz);
            }
            catch (TimeZoneNotFoundException)
            {
                return asUtc.AddHours(7);
            }
        }
    }
}

using System;
using System.IO;
using System.Text;
using FluentAssertions;
using VeggieLife.BLL.DTOs.Subscriptions.Responses;
using VeggieLife.BLL.Services.Subscriptions;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Subscriptions;

public class ReceiptPdfGeneratorTests
{
    private static PaymentReceiptDto Sample() => new()
    {
        ReceiptNumber = "REC-20261105-000123",
        TransactionId = 123,
        OrderCode = "VL_1793820953562",
        Gateway = "VIETQR",
        Status = "SUCCESS",
        PaymentDate = new DateTime(2026, 11, 5, 3, 30, 0, DateTimeKind.Utc),
        FullName = "Nguyễn Văn Minh Tâm",
        Email = "nvmtamm@gmail.com",
        PlanId = 2,
        PlanName = "Gói VIP 1 Tháng - Mở Khóa AI Dinh Dưỡng",
        DurationDays = 30,
        AmountVnd = 49000m,
        VipStartsAt = new DateTime(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc),
        VipEndsAt = new DateTime(2026, 12, 5, 23, 59, 59, DateTimeKind.Utc)
    };

    [Fact]
    public void Generate_ReturnsValidPdfBytes()
    {
        var bytes = ReceiptPdfGenerator.Generate(Sample());

        bytes.Length.Should().BeGreaterThan(1000);
        Encoding.ASCII.GetString(bytes, 0, 5).Should().Be("%PDF-");
    }

    [Fact]
    public void FileName_UsesReceiptNumber()
    {
        ReceiptPdfGenerator.FileName(Sample()).Should().Be("VeggieLife-REC-20261105-000123.pdf");
    }

    [Fact]
    public void Generate_WritesSampleForManualCheck_WhenEnvSet()
    {
        var path = Environment.GetEnvironmentVariable("RECEIPT_PDF_SAMPLE_PATH");
        if (string.IsNullOrEmpty(path)) return;
        File.WriteAllBytes(path, ReceiptPdfGenerator.Generate(Sample()));
    }
}

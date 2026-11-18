using System.Collections.Generic;
using FluentAssertions;
using VeggieLife.BLL.Common.Helpers;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Fridge;

/// <summary>
/// Bước 1 — Kế hoạch cải tiến nhận diện tủ lạnh: tên AI chỉ được khớp với danh mục khi trùng hẳn
/// hoặc danh mục CHUNG HƠN; không đổi sang tên cụ thể hơn (lỗi "Chuối" → "Hoa chuối thái mỏng").
/// </summary>
public class CatalogMatchTests
{
    private sealed record Ing(int Id, string Name);

    // Danh mục mô phỏng các tên thật đã gây lỗi trên ảnh mẫu
    private static readonly List<Ing> Catalog = new()
    {
        new(1, "Hoa chuối thái mỏng"),
        new(2, "Nước ép cam"),
        new(3, "Phô mai sợi Mozzarella"),
        new(4, "Hành Boa-rô (Tỏi tây / Leek)"),
        new(5, "Ớt chuông"),
        new(6, "Bông cải xanh (Broccoli)"),
        new(7, "Cà chua"),
        new(8, "Thịt đỏ"),
        new(9, "Cà tím dài"),
        new(10, "Ớt hiểm tươi"),
        new(11, "Nấm rơm"),
        new(12, "Bí đỏ (Bí ngô)"),
        new(13, "Ớt chuông đỏ"),
    };

    [Theory]
    [InlineData("Chuối")]
    [InlineData("Cam")]
    [InlineData("Phô mai")]
    [InlineData("Tỏi")]
    [InlineData("Thịt")]
    [InlineData("Cà tím")]
    [InlineData("Ớt")]
    [InlineData("Ca chua")] // thiếu dấu: chưa chốt, mặc định KHÔNG khớp cho an toàn
    [InlineData("Rau rừng lạ không có trong từ điển")]
    public void AiName_WithoutSafeCatalogEntry_IsNotMatched(string aiName)
    {
        var (item, match) = StringSimilarityHelper.FindBestCatalogMatch(aiName, Catalog, i => i.Name);

        match.IsMatch.Should().BeFalse($"'{aiName}' không được đổi thành '{item?.Name}'");
        item.Should().BeNull();
    }

    [Theory]
    [InlineData("Cà chua", 7, CatalogMatchKind.Exact)]
    [InlineData("cà chua", 7, CatalogMatchKind.Exact)]
    [InlineData("Cà chua", 7, CatalogMatchKind.Exact)]                // Unicode NFD từ mô hình
    [InlineData("Bông cải xanh", 6, CatalogMatchKind.Exact)]                // trùng tên chính, bỏ phần ngoặc
    [InlineData("Broccoli", 6, CatalogMatchKind.Exact)]                     // trùng biệt danh trong ngoặc
    [InlineData("Bí ngô", 12, CatalogMatchKind.Exact)]
    [InlineData("Tỏi tây", 4, CatalogMatchKind.Exact)]                      // biệt danh "Tỏi tây" đúng nghĩa
    [InlineData("Ớt chuông vàng", 5, CatalogMatchKind.Generalization)]      // danh mục chung hơn
    [InlineData("Nấm rơm tươi", 11, CatalogMatchKind.Generalization)]
    [InlineData("Cà chua bi", 7, CatalogMatchKind.Generalization)]
    [InlineData("Ớt chuông đỏ", 13, CatalogMatchKind.Exact)]                // trùng hẳn thắng "Ớt chuông" chung hơn
    [InlineData("Ớt chuông đỏ ngọt", 13, CatalogMatchKind.Generalization)]  // chọn tên chung hơn nhưng sát nghĩa nhất
    public void AiName_MatchesExactOrMoreGeneralCatalogEntry(string aiName, int expectedId, CatalogMatchKind expectedKind)
    {
        var (item, match) = StringSimilarityHelper.FindBestCatalogMatch(aiName, Catalog, i => i.Name);

        item.Should().NotBeNull();
        item!.Id.Should().Be(expectedId);
        match.Kind.Should().Be(expectedKind);
    }

    [Fact]
    public void SameWordCount_SmallTypo_IsMatchedAsTypo()
    {
        var m = StringSimilarityHelper.ClassifyCatalogMatch("Bông cải xamh", "Bông cải xanh");

        m.Kind.Should().Be(CatalogMatchKind.Typo);
    }

    [Theory]
    [InlineData(null, "Cà chua")]
    [InlineData("Cà chua", null)]
    [InlineData("  ", "Cà chua")]
    public void EmptyNames_AreNotMatched(string? ai, string? catalog)
    {
        StringSimilarityHelper.ClassifyCatalogMatch(ai, catalog).IsMatch.Should().BeFalse();
    }
}

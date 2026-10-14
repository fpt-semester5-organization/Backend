using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.MealPlans;

/// <summary>
/// Bộ tạo tài liệu PDF xuất thực đơn và danh sách đi chợ (FN-MEAL-08).
/// Sử dụng font nhúng hiển thị chuẩn tiếng Việt có dấu, tạo trực tiếp trong bộ nhớ (không lưu tệp).
/// </summary>
public static class MealPlanPdfExporter
{
    private static readonly object FontLock = new();
    private static bool _fontRegistered;

    static MealPlanPdfExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        EnsureFontRegistered();
    }

    public static void EnsureFontRegistered()
    {
        if (_fontRegistered)
        {
            return;
        }

        lock (FontLock)
        {
            if (_fontRegistered)
            {
                return;
            }

            try
            {
                var assembly = typeof(MealPlanPdfExporter).Assembly;
                using var stream = assembly.GetManifestResourceStream("VeggieLife.BLL.Resources.Fonts.arial.ttf");
                if (stream != null)
                {
                    FontManager.RegisterFontFromStream(stream);
                    _fontRegistered = true;
                    return;
                }

                if (File.Exists(@"C:\Windows\Fonts\arial.ttf"))
                {
                    using var fs = File.OpenRead(@"C:\Windows\Fonts\arial.ttf");
                    FontManager.RegisterFontFromStream(fs);
                    _fontRegistered = true;
                }
            }
            catch
            {
                // Nếu font đã được nạp trước đó hoặc fallback sang system fonts
                _fontRegistered = true;
            }
        }
    }

    public static byte[] GeneratePdf(
        MealPlan plan,
        ShoppingList? shoppingList,
        bool includePlan,
        bool includeShopping)
    {
        EnsureFontRegistered();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                page.Header().Element(c => ComposeHeader(c, plan));
                page.Content().Element(c => ComposeContent(c, plan, shoppingList, includePlan, includeShopping));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, MealPlan plan)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(titleCol =>
                {
                    titleCol.Item().Text("VeggieLife — Sống Khỏe Mỗi Ngày")
                        .Bold().FontSize(16).FontColor(Colors.Green.Darken3);
                    titleCol.Item().Text($"THỰC ĐƠN TUẦN: {plan.PlanName.ToUpper()}")
                        .SemiBold().FontSize(12).FontColor(Colors.Grey.Darken3);
                });

                row.ConstantItem(120).AlignRight().Column(metaCol =>
                {
                    metaCol.Item().Text($"Từ: {plan.StartDate:dd/MM/yyyy}").FontSize(9).FontColor(Colors.Grey.Darken2);
                    metaCol.Item().Text($"Đến: {plan.EndDate:dd/MM/yyyy}").FontSize(9).FontColor(Colors.Grey.Darken2);
                    metaCol.Item().Text($"Calo: {plan.TargetDailyKcal:N0} kcal/ngày").FontSize(9).Bold().FontColor(Colors.Green.Darken2);
                });
            });

            col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Green.Lighten2);
        });
    }

    private static void ComposeContent(
        IContainer container,
        MealPlan plan,
        ShoppingList? shoppingList,
        bool includePlan,
        bool includeShopping)
    {
        container.PaddingVertical(10).Column(col =>
        {
            if (includePlan)
            {
                col.Item().Text("1. KẾ HOẠCH BỮA ĂN 7 NGÀY")
                    .Bold().FontSize(12).FontColor(Colors.Green.Darken3);

                col.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(80);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.ConstantColumn(60);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Green.Lighten4).Padding(4).Text("Thứ / Ngày").Bold();
                        header.Cell().Background(Colors.Green.Lighten4).Padding(4).Text("Bữa Sáng").Bold();
                        header.Cell().Background(Colors.Green.Lighten4).Padding(4).Text("Bữa Trưa").Bold();
                        header.Cell().Background(Colors.Green.Lighten4).Padding(4).Text("Bữa Tối").Bold();
                        header.Cell().Background(Colors.Green.Lighten4).Padding(4).AlignRight().Text("Tổng Calo").Bold();
                    });

                    var dayNames = new[] { "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN" };
                    var dayLabels = new Dictionary<string, string>
                    {
                        ["MON"] = "Thứ Hai",
                        ["TUE"] = "Thứ Ba",
                        ["WED"] = "Thứ Tư",
                        ["THU"] = "Thứ Năm",
                        ["FRI"] = "Thứ Sáu",
                        ["SAT"] = "Thứ Bảy",
                        ["SUN"] = "Chủ Nhật"
                    };

                    int dayIdx = 0;
                    foreach (var day in dayNames)
                    {
                        var slots = plan.MealSlots
                            .Where(s => string.Equals(s.DayOfWeek, day, StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        var breakfast = slots.FirstOrDefault(s => string.Equals(s.MealType, "BREAKFAST", StringComparison.OrdinalIgnoreCase));
                        var lunch = slots.FirstOrDefault(s => string.Equals(s.MealType, "LUNCH", StringComparison.OrdinalIgnoreCase));
                        var dinner = slots.FirstOrDefault(s => string.Equals(s.MealType, "DINNER", StringComparison.OrdinalIgnoreCase));

                        var dayKcal = slots.Sum(s => s.Recipe?.CaloriesKcal ?? 0m);
                        var bg = (dayIdx % 2 == 0) ? Colors.White : Colors.Grey.Lighten4;

                        table.Cell().Background(bg).Padding(4).Text(dayLabels.GetValueOrDefault(day, day)).SemiBold();
                        table.Cell().Background(bg).Padding(4).Text(breakfast?.Recipe?.Title ?? "—").FontSize(9);
                        table.Cell().Background(bg).Padding(4).Text(lunch?.Recipe?.Title ?? "—").FontSize(9);
                        table.Cell().Background(bg).Padding(4).Text(dinner?.Recipe?.Title ?? "—").FontSize(9);
                        table.Cell().Background(bg).Padding(4).AlignRight().Text($"{dayKcal:N0} kcal").FontSize(9).Bold();

                        dayIdx++;
                    }
                });
            }

            if (includeShopping && shoppingList != null && shoppingList.ShoppingListItems.Count > 0)
            {
                if (includePlan)
                {
                    col.Item().PaddingTop(15);
                }

                col.Item().Text("2. DANH SÁCH NGUYÊN LIỆU ĐI CHỢ THEO QUẦY HÀNG")
                    .Bold().FontSize(12).FontColor(Colors.Green.Darken3);

                var aisleNames = new[] { "PRODUCE", "CONDIMENTS", "DRY_GOODS", "PLANT_PROTEIN" };
                var aisleLabels = new Dictionary<string, string>
                {
                    ["PRODUCE"] = "Rau củ quả tươi",
                    ["CONDIMENTS"] = "Gia vị & Nước sốt",
                    ["DRY_GOODS"] = "Đồ khô & Ngũ cốc",
                    ["PLANT_PROTEIN"] = "Đậu hạt & Đạm thực vật"
                };

                // Dò nguồn công thức của từng nguyên liệu
                var recipeSourceMap = new Dictionary<int, List<string>>();
                foreach (var slot in plan.MealSlots)
                {
                    if (slot.Recipe == null)
                    {
                        continue;
                    }

                    foreach (var ri in slot.Recipe.RecipeIngredients)
                    {
                        if (!recipeSourceMap.TryGetValue(ri.IngredientId, out var sources))
                        {
                            sources = new List<string>();
                            recipeSourceMap[ri.IngredientId] = sources;
                        }
                        if (!sources.Contains(slot.Recipe.Title))
                        {
                            sources.Add(slot.Recipe.Title);
                        }
                    }
                }

                foreach (var aisle in aisleNames)
                {
                    var items = shoppingList.ShoppingListItems
                        .Where(i => string.Equals(NormalizeAisle(i.Ingredient?.DefaultStallType), aisle, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(i => i.Ingredient?.IngredientName ?? string.Empty)
                        .ToList();

                    if (items.Count == 0)
                    {
                        continue;
                    }

                    col.Item().PaddingTop(8).Text($"• Quầy {aisleLabels.GetValueOrDefault(aisle, aisle)}")
                        .SemiBold().FontSize(10).FontColor(Colors.Green.Darken2);

                    col.Item().PaddingTop(3).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(30);
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(4);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("[ ]").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Tên nguyên liệu").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Khối lượng").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Nguồn món ăn").Bold();
                        });

                        foreach (var itm in items)
                        {
                            recipeSourceMap.TryGetValue(itm.IngredientId, out var sources);
                            var sourceText = sources != null && sources.Count > 0 ? string.Join(", ", sources) : "Thực đơn";

                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(3).Text("[ ]").FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(3).Text(itm.Ingredient?.IngredientName ?? "Nguyên liệu").FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(3).Text($"{itm.AggregatedQuantity:G29} {itm.Unit}").FontSize(9).Bold();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(3).Text(sourceText).FontSize(8).FontColor(Colors.Grey.Darken2);
                        }
                    });
                }
            }
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
            col.Item().PaddingTop(3).Row(row =>
            {
                row.RelativeItem().Text("Khuyến nghị mang tính chất tham khảo dinh dưỡng, vui lòng tham vấn ý kiến bác sĩ chuyên khoa đối với các bệnh lý nền.")
                    .FontSize(7).Italic().FontColor(Colors.Grey.Darken1);
                row.ConstantItem(60).AlignRight().Text(x =>
                {
                    x.Span("Trang ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });
    }

    private static string NormalizeAisle(string? stallType)
    {
        if (string.IsNullOrWhiteSpace(stallType))
        {
            return "PRODUCE";
        }

        var upper = stallType.Trim().ToUpperInvariant();
        if (upper == "VEG") return "PRODUCE";
        if (upper == "DRY") return "DRY_GOODS";
        if (upper == "CONDIMENT") return "CONDIMENTS";
        if (upper == "PROTEIN") return "PLANT_PROTEIN";

        return upper switch
        {
            "PRODUCE" => "PRODUCE",
            "CONDIMENTS" => "CONDIMENTS",
            "DRY_GOODS" => "DRY_GOODS",
            "PLANT_PROTEIN" => "PLANT_PROTEIN",
            _ => "PRODUCE"
        };
    }
}

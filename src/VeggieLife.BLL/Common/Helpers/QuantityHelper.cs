using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace VeggieLife.BLL.Common.Helpers;

/// <summary>
/// Cung cấp các tiện ích phân tích, chuẩn hóa định lượng và so sánh định lượng nguyên liệu
/// giữa tủ lạnh ảo và công thức nấu ăn món ăn (FN-VIS-01, FN-VIS-03).
/// </summary>
public static class QuantityHelper
{
    private static readonly Regex NumberPattern = new(@"(\d+(?:[.,]\d+)?)", RegexOptions.Compiled);
    private static readonly Regex ParenthesisPattern = new(@"\(([^)]+)\)", RegexOptions.Compiled);

    /// <summary>
    /// Phân tích chuỗi tên tùy chỉnh hoặc định lượng thô để tách tên nguyên liệu, định lượng, số lượng và đơn vị.
    /// Ví dụ: "Bí đỏ (nửa trái)" -> Name="Bí đỏ", Quantity="nửa trái", Amount=0.5, Unit="trái"
    /// </summary>
    public static (string CleanName, string? Quantity, decimal? Amount, string? Unit) ParseItemNameAndQuantity(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return (string.Empty, null, null, null);
        }

        var text = rawText.Trim();
        var parenMatch = ParenthesisPattern.Match(text);

        if (parenMatch.Success)
        {
            var quantityText = parenMatch.Groups[1].Value.Trim();
            var cleanName = text.Replace(parenMatch.Value, string.Empty).Trim();
            var (amount, unit) = ParseQuantity(quantityText);
            return (cleanName, quantityText, amount, unit);
        }

        // Nếu không có ngoặc đơn, thử kiểm tra ký tự gạch ngang "-" (ví dụ: "Bí đỏ - nửa trái")
        var dashIndex = text.IndexOf('-');
        if (dashIndex > 0 && dashIndex < text.Length - 1)
        {
            var potentialName = text[..dashIndex].Trim();
            var potentialQty = text[(dashIndex + 1)..].Trim();
            var (amount, unit) = ParseQuantity(potentialQty);
            if (amount.HasValue || !string.IsNullOrWhiteSpace(unit))
            {
                return (potentialName, potentialQty, amount, unit);
            }
        }

        // Tên đơn thuần
        return (text, null, null, null);
    }

    /// <summary>
    /// Phân tích chuỗi định lượng tiếng Việt thành số lượng số học và đơn vị chuẩn.
    /// Ví dụ: "nửa trái" -> (0.5, "trái"), "3 ký" -> (3.0, "kg"), "500g" -> (500.0, "g")
    /// </summary>
    public static (decimal? Amount, string? Unit) ParseQuantity(string? quantityText)
    {
        if (string.IsNullOrWhiteSpace(quantityText))
        {
            return (null, null);
        }

        var lower = quantityText.Trim().ToLowerInvariant();
        decimal? amount = null;
        string? unit = null;

        // 1. Nhận biết các từ định lượng đặc biệt tiếng Việt
        if (lower.Contains("nửa") || lower.Contains("1/2"))
        {
            amount = 0.5m;
        }
        else if (lower.Contains("1/4"))
        {
            amount = 0.25m;
        }
        else if (lower.Contains("1/3"))
        {
            amount = 0.33m;
        }
        else if (lower.Contains("một rưỡi") || lower.Contains("1.5") || lower.Contains("1,5"))
        {
            amount = 1.5m;
        }
        else if (lower.Contains("hai rưỡi") || lower.Contains("2.5") || lower.Contains("2,5"))
        {
            amount = 2.5m;
        }
        else
        {
            var numMatch = NumberPattern.Match(lower);
            if (numMatch.Success)
            {
                var numStr = numMatch.Groups[1].Value.Replace(',', '.');
                if (decimal.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                {
                    amount = parsed;
                }
            }
        }

        // 2. Nhận biết đơn vị đo
        if (lower.Contains("kg") || lower.Contains("ký") || lower.Contains("kí") || lower.Contains("kilo"))
        {
            unit = "kg";
        }
        else if (lower.Contains("gam") || lower.Contains("gram") || lower.Contains("gr") || Regex.IsMatch(lower, @"\bg\b|\d+g"))
        {
            unit = "g";
        }
        else if (lower.Contains("ml") || lower.Contains("mililit"))
        {
            unit = "ml";
        }
        else if (lower.Contains("lít") || lower.Contains("lit") || Regex.IsMatch(lower, @"\bl\b|\d+l"))
        {
            unit = "lít";
        }
        else if (lower.Contains("trái") || lower.Contains("quả"))
        {
            unit = "trái";
        }
        else if (lower.Contains("củ"))
        {
            unit = "củ";
        }
        else if (lower.Contains("bó"))
        {
            unit = "bó";
        }
        else if (lower.Contains("cây"))
        {
            unit = "cây";
        }
        else if (lower.Contains("miếng") || lower.Contains("lát"))
        {
            unit = "miếng";
        }
        else if (lower.Contains("hộp"))
        {
            unit = "hộp";
        }
        else if (lower.Contains("gói"))
        {
            unit = "gói";
        }
        else if (lower.Contains("tép") || lower.Contains("nhánh"))
        {
            unit = "tép";
        }

        return (amount, unit);
    }

    /// <summary>
    /// Chuẩn hóa đơn vị đo lường.
    /// </summary>
    public static string NormalizeUnit(string? unit)
    {
        if (string.IsNullOrWhiteSpace(unit)) return string.Empty;
        var lower = unit.Trim().ToLowerInvariant();

        if (lower is "kg" or "ký" or "kí" or "kilo" or "kilogram") return "kg";
        if (lower is "g" or "gam" or "gram" or "gr") return "g";
        if (lower is "ml" or "mililit") return "ml";
        if (lower is "lít" or "lit" or "l") return "lít";
        if (lower is "trái" or "quả") return "trái";
        if (lower is "củ") return "củ";
        if (lower is "bó") return "bó";
        if (lower is "cây") return "cây";
        if (lower is "miếng" or "lát") return "miếng";
        if (lower is "hộp") return "hộp";
        if (lower is "gói") return "gói";
        return lower;
    }

    /// <summary>
    /// Định dạng tên nguyên liệu kèm định lượng để lưu vào FridgeInventory.CustomItemName.
    /// </summary>
    public static string FormatWithQuantity(string name, string? quantity)
    {
        if (string.IsNullOrWhiteSpace(quantity))
        {
            return name.Trim();
        }

        var clean = name.Trim();
        var parenMatch = ParenthesisPattern.Match(clean);
        if (parenMatch.Success)
        {
            clean = clean.Replace(parenMatch.Value, string.Empty).Trim();
        }

        var combined = $"{clean} ({quantity.Trim()})";
        return combined.Length > 100 ? combined[..100] : combined;
    }

    /// <summary>
    /// So sánh định lượng đang có trong tủ lạnh so với định lượng công thức yêu cầu.
    /// Trả về: IsSufficient (đủ hay không), MissingAmount (số lượng còn thiếu), MissingDisplay (chuỗi hiển thị).
    /// </summary>
    public static (bool IsSufficient, decimal MissingAmount, string MissingDisplay) CheckQuantitySufficiency(
        decimal? availableAmount,
        string? availableUnit,
        decimal requiredAmount,
        string requiredUnit,
        string ingredientName)
    {
        // Nếu tủ lạnh không xác định được số lượng -> Mặc định coi là đủ (tránh chặn người dùng nếu AI không đọc được số lượng)
        if (!availableAmount.HasValue || availableAmount.Value <= 0)
        {
            return (true, 0, string.Empty);
        }

        var normAvailUnit = NormalizeUnit(availableUnit);
        var normReqUnit = NormalizeUnit(requiredUnit);

        // Trường hợp 1: Cùng đơn vị đo (ví dụ: cả hai đều là "trái", "củ", "bó", "g", "kg"...)
        if (string.Equals(normAvailUnit, normReqUnit, StringComparison.OrdinalIgnoreCase))
        {
            if (availableAmount.Value >= requiredAmount)
            {
                return (true, 0, string.Empty);
            }

            var missing = requiredAmount - availableAmount.Value;
            var display = $"{missing:G29} {normReqUnit}";
            return (false, missing, display);
        }

        // Trường hợp 2: Quy đổi giữa kg và g
        if (normAvailUnit == "kg" && normReqUnit == "g")
        {
            var availInG = availableAmount.Value * 1000m;
            if (availInG >= requiredAmount)
            {
                return (true, 0, string.Empty);
            }

            var missing = requiredAmount - availInG;
            return (false, missing, $"{missing:G29} g");
        }

        if (normAvailUnit == "g" && normReqUnit == "kg")
        {
            var reqInG = requiredAmount * 1000m;
            if (availableAmount.Value >= reqInG)
            {
                return (true, 0, string.Empty);
            }

            var missingG = reqInG - availableAmount.Value;
            return (false, missingG / 1000m, $"{missingG:G29} g ({missingG / 1000m:G29} kg)");
        }

        // Trường hợp 3: Quy đổi giữa lít và ml
        if (normAvailUnit == "lít" && normReqUnit == "ml")
        {
            var availInMl = availableAmount.Value * 1000m;
            if (availInMl >= requiredAmount)
            {
                return (true, 0, string.Empty);
            }

            var missing = requiredAmount - availInMl;
            return (false, missing, $"{missing:G29} ml");
        }

        if (normAvailUnit == "ml" && normReqUnit == "lít")
        {
            var reqInMl = requiredAmount * 1000m;
            if (availableAmount.Value >= reqInMl)
            {
                return (true, 0, string.Empty);
            }

            var missingMl = reqInMl - availableAmount.Value;
            return (false, missingMl / 1000m, $"{missingMl:G29} ml");
        }

        // Trường hợp 4: Khác đơn vị không quy đổi trực tiếp được (ví dụ trong tủ có "trái", công thức yêu cầu "g")
        // Ước lượng thông dụng:
        // Nếu trong tủ có ít nhất 1 trái/quả/củ thì coi là tạm đủ nếu yêu cầu nhỏ (< 300g)
        if (normAvailUnit is "trái" or "quả" or "củ")
        {
            if (normReqUnit == "g")
            {
                // Ước lượng trung bình 1 quả/củ ~ 150g - 500g
                var estimatedWeightG = availableAmount.Value * 200m;
                if (estimatedWeightG >= requiredAmount)
                {
                    return (true, 0, string.Empty);
                }

                var missing = requiredAmount - estimatedWeightG;
                return (false, missing, $"khoảng {missing:G29} g (hiện có {availableAmount.Value:G29} {normAvailUnit})");
            }
        }

        // Không cùng loại đơn vị: nếu số lượng hiện có >= yêu cầu số học thì coi là tạm đủ
        if (availableAmount.Value >= requiredAmount)
        {
            return (true, 0, string.Empty);
        }

        var genericMissing = requiredAmount - availableAmount.Value;
        return (false, genericMissing, $"{genericMissing:G29} {normReqUnit}");
    }
}

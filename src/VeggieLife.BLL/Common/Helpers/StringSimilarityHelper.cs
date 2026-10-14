using System;
using System.Linq;

namespace VeggieLife.BLL.Common.Helpers;

/// <summary>
/// Tiện ích tính toán độ tương đồng giữa hai chuỗi ký tự tên nguyên liệu.
/// Kết hợp khoảng cách biên tập Levenshtein chuẩn hóa và chỉ số tương đồng từ khóa (Token Jaccard)
/// để đối chiếu tên nguyên liệu nhận diện với danh mục Ingredients theo ngưỡng >= 80% (FN-VIS-01).
/// </summary>
public static class StringSimilarityHelper
{
    /// <summary>
    /// Tính độ tương đồng giữa hai chuỗi từ 0.0 (hoàn toàn khác) đến 1.0 (trùng khớp hoàn toàn).
    /// </summary>
    public static double CalculateSimilarity(string? source, string? target)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
        {
            return 0.0;
        }

        var s = source.Trim().ToLowerInvariant();
        var t = target.Trim().ToLowerInvariant();

        if (s == t)
        {
            return 1.0;
        }

        // 1. Khoảng cách Levenshtein chuẩn hóa
        var distance = LevenshteinDistance(s, t);
        var maxLen = Math.Max(s.Length, t.Length);
        var levSim = maxLen > 0 ? 1.0 - ((double)distance / maxLen) : 1.0;

        // 2. Độ tương đồng Token Jaccard và Sørensen-Dice
        var sTokens = s.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();
        var tTokens = t.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();

        var intersection = sTokens.Intersect(tTokens).Count();
        var union = sTokens.Union(tTokens).Count();
        var tokenSim = union > 0 ? (double)intersection / union : 0.0;
        var diceSim = (sTokens.Count + tTokens.Count) > 0 ? (2.0 * intersection) / (sTokens.Count + tTokens.Count) : 0.0;

        // 3. Nếu một chuỗi chứa chuỗi kia như cụm từ con hoàn chỉnh (ví dụ "Nấm rơm" trong "Nấm rơm tươi")
        var substringSim = 0.0;
        if (s.Contains(t) || t.Contains(s))
        {
            var minTokens = Math.Min(sTokens.Count, tTokens.Count);
            var tokenContainment = minTokens > 0 ? (double)intersection / minTokens : 0.0;
            if (tokenContainment >= 1.0)
            {
                substringSim = Math.Max(diceSim, 0.80);
            }
            else
            {
                var minLen = Math.Min(s.Length, t.Length);
                substringSim = (double)minLen / maxLen;
            }
        }

        return Math.Max(levSim, Math.Max(tokenSim, Math.Max(diceSim, substringSim)));
    }

    /// <summary>
    /// Thuật toán quy hoạch động tính khoảng cách Levenshtein giữa 2 chuỗi.
    /// </summary>
    public static int LevenshteinDistance(string s, string t)
    {
        var n = s.Length;
        var m = t.Length;

        if (n == 0) return m;
        if (m == 0) return n;

        var d = new int[n + 1, m + 1];

        for (var i = 0; i <= n; i++) d[i, 0] = i;
        for (var j = 0; j <= m; j++) d[0, j] = j;

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }
}

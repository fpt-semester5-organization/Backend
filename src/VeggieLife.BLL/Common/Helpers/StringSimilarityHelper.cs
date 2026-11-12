using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace VeggieLife.BLL.Common.Helpers;

/// <summary>
/// Tiện ích tính toán độ tương đồng giữa hai chuỗi ký tự tên nguyên liệu.
/// Kết hợp khoảng cách biên tập Levenshtein chuẩn hóa, chỉ số tương đồng từ khóa (Token Jaccard/Dice)
/// và bóc tách biệt danh/tên phụ trong ngoặc đơn (Alias extraction)
/// để đối chiếu tên nguyên liệu nhận diện với danh mục Ingredients theo ngưỡng >= 80% (FN-VIS-01).
/// </summary>
public static class StringSimilarityHelper
{
    private static readonly char[] PunctuationSeparators = new[]
    {
        '(', ')', '[', ']', '{', '}', ',', '.', ';', ':', '-', '_', '/', '\\', '\"', '\'', '!', '?', '+'
    };

    /// <summary>Ngưỡng Levenshtein để coi là sai chính tả nhẹ (cùng số từ).</summary>
    public const double TypoSimilarityThreshold = 0.90;

    /// <summary>
    /// Phân loại mức khớp an toàn giữa tên AI nhận diện và tên trong danh mục Ingredients (FN-VIS-01).
    /// Tên tiếng Việt đặt danh từ chính trước, từ bổ nghĩa sau, nên chỉ chấp nhận danh mục trùng hẳn
    /// hoặc CHUNG HƠN (là phần đầu của tên AI: "Ớt chuông" ⊂ "Ớt chuông vàng").
    /// Danh mục CỤ THỂ HƠN tên AI ("Chuối" → "Hoa chuối thái mỏng", "Cam" → "Nước ép cam") bị từ chối.
    /// </summary>
    public static CatalogMatch ClassifyCatalogMatch(string? aiName, string? catalogName)
    {
        if (string.IsNullOrWhiteSpace(aiName) || string.IsNullOrWhiteSpace(catalogName))
        {
            return CatalogMatch.None;
        }

        var aiVariants = ExtractVariants(aiName).Select(Tokenize).Where(t => t.Length > 0).ToList();
        var catVariants = ExtractVariants(catalogName).Select(Tokenize).Where(t => t.Length > 0).ToList();
        var best = CatalogMatch.None;

        foreach (var a in aiVariants)
        {
            foreach (var c in catVariants)
            {
                CatalogMatch m;
                if (a.SequenceEqual(c))
                {
                    m = new CatalogMatch(CatalogMatchKind.Exact, 1.0, c.Length);
                }
                else if (c.Length < a.Length && a.Take(c.Length).SequenceEqual(c))
                {
                    // Danh mục là phần đầu của tên AI → tên chung hơn, chấp nhận; càng nhiều từ càng sát nghĩa
                    m = new CatalogMatch(CatalogMatchKind.Generalization, (double)c.Length / a.Length, c.Length);
                }
                else if (c.Length == a.Length)
                {
                    var sa = string.Join(' ', a);
                    var sc = string.Join(' ', c);
                    var lev = 1.0 - (double)LevenshteinDistance(sa, sc) / Math.Max(sa.Length, sc.Length);
                    m = lev >= TypoSimilarityThreshold
                        ? new CatalogMatch(CatalogMatchKind.Typo, lev, c.Length)
                        : CatalogMatch.None;
                }
                else
                {
                    m = CatalogMatch.None;
                }

                if (m.IsBetterThan(best))
                {
                    best = m;
                }
            }
        }

        return best;
    }

    /// <summary>
    /// Chọn nguyên liệu danh mục khớp an toàn nhất với tên AI: Trùng hẳn &gt; Chung hơn (nhiều từ nhất) &gt; Sai chính tả nhẹ.
    /// Trả về null nếu không có ứng viên an toàn (khi đó giữ nguyên tên AI).
    /// </summary>
    public static (T? Item, CatalogMatch Match) FindBestCatalogMatch<T>(string? aiName, IEnumerable<T> catalog, Func<T, string?> nameOf)
        where T : class
    {
        T? bestItem = null;
        var best = CatalogMatch.None;
        foreach (var item in catalog)
        {
            var m = ClassifyCatalogMatch(aiName, nameOf(item));
            if (m.IsBetterThan(best))
            {
                best = m;
                bestItem = item;
            }
        }

        return (bestItem, best);
    }

    private static string[] Tokenize(string text)
    {
        var s = CleanPunctuation(text.Normalize(NormalizationForm.FormC).Trim().ToLowerInvariant());
        return s.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// Tính độ tương đồng giữa hai chuỗi từ 0.0 (hoàn toàn khác) đến 1.0 (trùng khớp hoàn toàn).
    /// Tự động tách và so sánh các biến thể (tên chính, biệt danh trong ngoặc đơn) giữa 2 chuỗi.
    /// Lưu ý: điểm cao khi tên ngắn nằm trọn trong tên dài → KHÔNG dùng để khớp tên AI với danh mục
    /// (dùng <see cref="FindBestCatalogMatch{T}"/>).
    /// </summary>
    public static double CalculateSimilarity(string? source, string? target)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
        {
            return 0.0;
        }

        var sourceVariants = ExtractVariants(source);
        var targetVariants = ExtractVariants(target);

        var maxSimilarity = 0.0;

        foreach (var sVar in sourceVariants)
        {
            foreach (var tVar in targetVariants)
            {
                var sim = ComputeCoreSimilarity(sVar, tVar);
                if (sim > maxSimilarity)
                {
                    maxSimilarity = sim;
                }

                if (maxSimilarity >= 1.0)
                {
                    return 1.0;
                }
            }
        }

        return maxSimilarity;
    }

    /// <summary>
    /// Bóc tách các biến thể tên gọi từ một chuỗi (ví dụ: "Bí đỏ hạt đậu (Bí ngô)" -> ["Bí đỏ hạt đậu (Bí ngô)", "Bí đỏ hạt đậu", "Bí ngô"]).
    /// Hỗ trợ cả dấu ngoặc đơn chứa nhiều biệt danh cách nhau bởi dấu '/' hoặc ','.
    /// </summary>
    public static List<string> ExtractVariants(string text)
    {
        var variants = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var trimmed = text.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return new List<string>();
        }

        variants.Add(trimmed);

        // Bóc tách nội dung bên trong dấu ngoặc đơn: "Bí đỏ hạt đậu (Bí ngô)" -> main = "Bí đỏ hạt đậu", alias = "Bí ngô"
        var openIdx = trimmed.IndexOf('(');
        var closeIdx = trimmed.LastIndexOf(')');

        if (openIdx >= 0 && closeIdx > openIdx)
        {
            var mainPart = trimmed.Substring(0, openIdx).Trim();
            if (!string.IsNullOrEmpty(mainPart))
            {
                variants.Add(mainPart);
            }

            var aliasPart = trimmed.Substring(openIdx + 1, closeIdx - openIdx - 1).Trim();
            if (!string.IsNullOrEmpty(aliasPart))
            {
                var aliases = aliasPart.Split(new[] { '/', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var alias in aliases)
                {
                    if (!string.IsNullOrEmpty(alias))
                    {
                        variants.Add(alias);
                    }
                }
            }
        }

        return variants.ToList();
    }

    /// <summary>
    /// Tính độ tương đồng cốt lõi giữa 2 chuỗi con đã được bóc tách.
    /// </summary>
    private static double ComputeCoreSimilarity(string source, string target)
    {
        var s = source.Trim().ToLowerInvariant();
        var t = target.Trim().ToLowerInvariant();

        if (s == t)
        {
            return 1.0;
        }

        // Chuẩn hóa loại bỏ dấu câu để so sánh sạch
        var cleanS = CleanPunctuation(s);
        var cleanT = CleanPunctuation(t);

        if (cleanS == cleanT)
        {
            return 1.0;
        }

        if (string.IsNullOrWhiteSpace(cleanS) || string.IsNullOrWhiteSpace(cleanT))
        {
            return 0.0;
        }

        // 1. Khoảng cách Levenshtein chuẩn hóa
        var distance = LevenshteinDistance(cleanS, cleanT);
        var maxLen = Math.Max(cleanS.Length, cleanT.Length);
        var levSim = maxLen > 0 ? 1.0 - ((double)distance / maxLen) : 1.0;

        // 2. Độ tương đồng Token Jaccard và Sørensen-Dice
        var sTokens = cleanS.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();
        var tTokens = cleanT.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();

        var intersection = sTokens.Intersect(tTokens).Count();
        var union = sTokens.Union(tTokens).Count();
        var tokenSim = union > 0 ? (double)intersection / union : 0.0;
        var diceSim = (sTokens.Count + tTokens.Count) > 0 ? (2.0 * intersection) / (sTokens.Count + tTokens.Count) : 0.0;

        // 3. Nếu một chuỗi chứa chuỗi kia như cụm từ con hoàn chỉnh
        var substringSim = 0.0;
        if (cleanS.Contains(cleanT) || cleanT.Contains(cleanS))
        {
            var minTokens = Math.Min(sTokens.Count, tTokens.Count);
            var tokenContainment = minTokens > 0 ? (double)intersection / minTokens : 0.0;
            if (tokenContainment >= 1.0)
            {
                substringSim = Math.Max(diceSim, 0.85);
            }
            else
            {
                var minLen = Math.Min(cleanS.Length, cleanT.Length);
                substringSim = (double)minLen / maxLen;
            }
        }

        return Math.Max(levSim, Math.Max(tokenSim, Math.Max(diceSim, substringSim)));
    }

    private static string CleanPunctuation(string input)
    {
        var chars = input.Select(c => PunctuationSeparators.Contains(c) ? ' ' : c).ToArray();
        return Regex.Replace(new string(chars), @"\s+", " ").Trim();
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

/// <summary>Mức khớp giữa tên AI và tên danh mục.</summary>
public enum CatalogMatchKind
{
    None = 0,
    /// <summary>Sai chính tả nhẹ, cùng số từ (Levenshtein ≥ 0.90).</summary>
    Typo = 1,
    /// <summary>Tên danh mục là phần đầu (chung hơn) của tên AI.</summary>
    Generalization = 2,
    /// <summary>Trùng hẳn (không phân biệt hoa thường, dấu câu, hoặc trùng biệt danh trong ngoặc).</summary>
    Exact = 3,
}

/// <summary>Kết quả khớp: loại, điểm trong cùng loại, số từ của tên danh mục được khớp.</summary>
public readonly record struct CatalogMatch(CatalogMatchKind Kind, double Score, int MatchedTokens)
{
    public static readonly CatalogMatch None = new(CatalogMatchKind.None, 0, 0);

    public bool IsMatch => Kind != CatalogMatchKind.None;

    public bool IsBetterThan(CatalogMatch other)
    {
        if (Kind != other.Kind) return Kind > other.Kind;
        if (MatchedTokens != other.MatchedTokens) return MatchedTokens > other.MatchedTokens;
        return Score > other.Score;
    }
}

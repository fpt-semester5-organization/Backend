using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace VeggieLife.BLL.Common.Utils;

/// <summary>
/// Tiện ích lọc và che giấu (Sanitize/Mask) dữ liệu nhạy cảm trong AuditLogs (FN-ADM-17, BR-ADM-SYS-002).
/// Đảm bảo tuyệt đối không làm lộ mật khẩu, mã hash, OTP, khóa API, token JWT hoặc thông tin bí mật.
/// </summary>
public static class AuditLogSanitizer
{
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "passwordhash",
        "pass",
        "pwd",
        "token",
        "accesstoken",
        "refreshtoken",
        "idtoken",
        "jwt",
        "secret",
        "secretkey",
        "clientsecret",
        "apisecret",
        "apikey",
        "key",
        "otp",
        "verificationcode",
        "pin",
        "cvv",
        "cardnumber",
        "creditcard",
        "authorization",
        "signaturehash"
    };

    private static readonly Regex JwtRegex = new(
        @"\beyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\b",
        RegexOptions.Compiled);

    private static readonly Regex PlainTextSensitiveRegex = new(
        @"(?i)(password|passwordhash|token|secret|otp|pin|apikey)\s*[:=]\s*(?:""([^""]*)""|'([^']*)'|([^\s,;]+))",
        RegexOptions.Compiled);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    /// <summary>
    /// Che giấu dữ liệu nhạy cảm từ chuỗi JSON hoặc chuỗi văn bản thuần trong Old_Value / New_Value.
    /// </summary>
    public static string? MaskSensitiveData(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return rawValue;
        }

        var trimmed = rawValue.Trim();

        // Thử parse và xử lý theo cấu trúc JSON
        if ((trimmed.StartsWith('{') && trimmed.EndsWith('}')) ||
            (trimmed.StartsWith('[') && trimmed.EndsWith(']')))
        {
            try
            {
                var jsonNode = JsonNode.Parse(trimmed);
                if (jsonNode != null)
                {
                    MaskJsonNode(jsonNode);
                    return jsonNode.ToJsonString(SerializerOptions);
                }
            }
            catch
            {
                // Nếu không parse được JSON hợp lệ, fallback sang xử lý regex text
            }
        }

        // Xử lý text thuần: che JWT và các cặp key-value nhạy cảm
        var sanitized = JwtRegex.Replace(trimmed, "[REDACTED_JWT]");
        sanitized = PlainTextSensitiveRegex.Replace(sanitized, match =>
        {
            var key = match.Groups[1].Value;
            return $"{key}: \"***\"";
        });

        return sanitized;
    }

    private static void MaskJsonNode(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            var propertyNames = new List<string>();
            foreach (var kvp in obj)
            {
                propertyNames.Add(kvp.Key);
            }

            foreach (var propName in propertyNames)
            {
                if (SensitiveKeys.Contains(propName))
                {
                    obj[propName] = "***";
                }
                else if (obj[propName] != null)
                {
                    var child = obj[propName]!;
                    if (child is JsonValue jVal && jVal.TryGetValue<string>(out var strVal) && !string.IsNullOrEmpty(strVal))
                    {
                        if (JwtRegex.IsMatch(strVal))
                        {
                            obj[propName] = JwtRegex.Replace(strVal, "[REDACTED_JWT]");
                        }
                    }
                    else
                    {
                        MaskJsonNode(child);
                    }
                }
            }
        }
        else if (node is JsonArray arr)
        {
            for (var i = 0; i < arr.Count; i++)
            {
                var item = arr[i];
                if (item != null)
                {
                    if (item is JsonValue jVal && jVal.TryGetValue<string>(out var strVal) && !string.IsNullOrEmpty(strVal))
                    {
                        if (JwtRegex.IsMatch(strVal))
                        {
                            arr[i] = JwtRegex.Replace(strVal, "[REDACTED_JWT]");
                        }
                    }
                    else
                    {
                        MaskJsonNode(item);
                    }
                }
            }
        }
    }
}

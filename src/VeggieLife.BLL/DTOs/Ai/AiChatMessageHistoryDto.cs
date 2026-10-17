using System.Text.Json.Serialization;

namespace VeggieLife.BLL.DTOs.Ai;

/// <summary>
/// Tin nhắn lịch sử gửi kèm trong ngữ cảnh sang AI Service (role: "user" | "model", text).
/// </summary>
public class AiChatMessageHistoryDto
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonIgnore]
    public string SenderType
    {
        get => Role == "model" ? "ASSISTANT" : "USER";
        set => Role = (value == "ASSISTANT" ? "model" : "user");
    }

    [JsonIgnore]
    public string Content
    {
        get => Text;
        set => Text = value;
    }
}

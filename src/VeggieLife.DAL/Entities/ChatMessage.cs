using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class ChatMessage
{
    public long MessageId { get; set; }

    public long ChatSessionId { get; set; }

    public string SenderType { get; set; } = null!;

    public string Content { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string? MetadataJson { get; set; }

    public virtual ChatSession ChatSession { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class ChatSession
{
    public long ChatSessionId { get; set; }

    public int? UserId { get; set; }

    public int? ProfilesId { get; set; }

    public string? GuestUuid { get; set; }

    public string SessionTitle { get; set; } = null!;

    public string ContextType { get; set; } = null!;

    public DateTime StartedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public virtual HealthProfile? Profiles { get; set; }

    public virtual ICollection<Survey> Surveys { get; set; } = new List<Survey>();

    public virtual User? User { get; set; }
}

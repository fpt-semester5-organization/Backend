using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class Survey
{
    public long SurveyId { get; set; }

    public int UserId { get; set; }

    public long? SessionId { get; set; }

    public int RatingScore { get; set; }

    public string ContextQuestion { get; set; } = null!;

    public string? FeedbackContent { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ChatSession? Session { get; set; }

    public virtual User User { get; set; } = null!;
}

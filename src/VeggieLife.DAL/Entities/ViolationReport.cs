using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class ViolationReport
{
    public int ReportId { get; set; }

    public int? ReporterUserId { get; set; }

    public int? AdminId { get; set; }

    public int? RecipeId { get; set; }

    public int? CommentId { get; set; }

    public int? PostId { get; set; }

    public int? VideoId { get; set; }

    public string ViolationType { get; set; } = null!;

    public string EvidenceSummary { get; set; } = null!;

    public string ReportSource { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Comment? Comment { get; set; }

    public virtual Post? Post { get; set; }

    public virtual Recipe? Recipe { get; set; }

    public virtual Video? Video { get; set; }
}

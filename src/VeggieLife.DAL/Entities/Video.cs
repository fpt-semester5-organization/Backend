using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class Video
{
    public int VideoId { get; set; }

    public int? PostId { get; set; }

    public int? RecipeId { get; set; }

    public string Title { get; set; } = null!;

    public string VideoUrl { get; set; } = null!;

    public string? ThumbnailUrl { get; set; }

    public int DurationSeconds { get; set; }

    public int ViewCount { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Post? Post { get; set; }

    public virtual Recipe? Recipe { get; set; }

    public virtual VideoTranscript? VideoTranscript { get; set; }

    public virtual ICollection<ViolationReport> ViolationReports { get; set; } = new List<ViolationReport>();
}

using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class Post
{
    public int PostId { get; set; }

    public int UserId { get; set; }

    public int? RecipeId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string? ImageUrl { get; set; }

    public string Visibility { get; set; } = null!;

    public int ViewCount { get; set; }

    public string ModerationStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual Recipe? Recipe { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual ICollection<Video> Videos { get; set; } = new List<Video>();

    public virtual ICollection<ViolationReport> ViolationReports { get; set; } = new List<ViolationReport>();

    public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();
}

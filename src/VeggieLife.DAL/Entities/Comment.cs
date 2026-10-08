using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class Comment
{
    public int CommentId { get; set; }

    public int UserId { get; set; }

    public int? ParentCommentId { get; set; }

    public int? PostId { get; set; }

    public string Content { get; set; } = null!;

    public bool IsSoftDeleted { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Comment> InverseParentComment { get; set; } = new List<Comment>();

    public virtual Comment? ParentComment { get; set; }

    public virtual Post? Post { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual ICollection<ViolationReport> ViolationReports { get; set; } = new List<ViolationReport>();
}

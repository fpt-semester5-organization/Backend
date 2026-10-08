using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class Vote
{
    public long VoteId { get; set; }

    public int UserId { get; set; }

    public int PostId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}

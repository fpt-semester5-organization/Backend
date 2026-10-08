using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class ProductEnumber
{
    public int ProductId { get; set; }

    public string Code { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual EnumberDictionary CodeNavigation { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}

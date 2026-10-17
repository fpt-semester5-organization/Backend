using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class EnumberDictionary
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? CategoryType { get; set; }

    public string OriginSource { get; set; } = null!;

    public string VeganStatus { get; set; } = null!;

    public string? HealthRiskNotes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<ProductEnumber> ProductEnumbers { get; set; } = new List<ProductEnumber>();
}

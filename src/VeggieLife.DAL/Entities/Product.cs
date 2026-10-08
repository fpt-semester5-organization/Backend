using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class Product
{
    public int ProductId { get; set; }

    public string Barcode { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public string? Brand { get; set; }

    public string? IngredientsText { get; set; }

    public string VeganVerdict { get; set; } = null!;

    public bool AdminVerified { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastSyncedAt { get; set; }

    public virtual ICollection<ProductAllergen> ProductAllergens { get; set; } = new List<ProductAllergen>();

    public virtual ICollection<ProductEnumber> ProductEnumbers { get; set; } = new List<ProductEnumber>();

    public virtual ICollection<UserScanHistory> UserScanHistories { get; set; } = new List<UserScanHistory>();
}

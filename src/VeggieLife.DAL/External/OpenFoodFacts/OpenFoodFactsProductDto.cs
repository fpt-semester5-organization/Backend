using System.Collections.Generic;

namespace VeggieLife.DAL.External.OpenFoodFacts
{
    public class OpenFoodFactsProductDto
    {
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? Brand { get; set; }
        public string? IngredientsText { get; set; }
        public string VeganVerdict { get; set; } = "DOUBTFUL";
        public string VerdictText { get; set; } = string.Empty;
        public List<string> Additives { get; set; } = new();
        public List<string> Allergens { get; set; } = new();
        public string? ImageUrl { get; set; }
    }
}

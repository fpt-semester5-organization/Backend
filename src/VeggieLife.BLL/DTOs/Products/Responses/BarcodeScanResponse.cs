using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Products.Responses
{
    public class BarcodeScanResponse
    {
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string VeganVerdict { get; set; } = string.Empty;
        public string VerdictText { get; set; } = string.Empty;
        public List<DetectedAdditiveDto> DetectedAdditives { get; set; } = new();
        public bool PersonalAllergyCheck { get; set; }
        public List<string> AllergenWarnings { get; set; } = new();
        public ScanQuotaDto Quota { get; set; } = null!;
    }

    public class DetectedAdditiveDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string VeganStatus { get; set; } = string.Empty;
    }

    public class ScanQuotaDto
    {
        public int Used { get; set; }
        public int Limit { get; set; }
        public int Remaining { get; set; }
    }
}

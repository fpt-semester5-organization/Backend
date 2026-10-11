using System;

namespace VeggieLife.BLL.DTOs.Products.Responses
{
    public class UserScanHistoryResponse
    {
        public int ScanId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string VeganVerdict { get; set; } = string.Empty;
        public bool IsAllergenTriggered { get; set; }
        public DateTime ScannedAt { get; set; }
    }
}

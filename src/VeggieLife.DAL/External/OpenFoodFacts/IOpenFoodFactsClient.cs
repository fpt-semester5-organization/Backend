using System.Threading;
using System.Threading.Tasks;

namespace VeggieLife.DAL.External.OpenFoodFacts
{
    public interface IOpenFoodFactsClient
    {
        Task<OpenFoodFactsProductDto?> GetProductByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    }
}

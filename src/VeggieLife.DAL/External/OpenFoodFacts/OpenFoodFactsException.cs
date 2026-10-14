using System;

namespace VeggieLife.DAL.External.OpenFoodFacts;

/// <summary>
/// Ngoại lệ nảy sinh khi giao tiếp với Open Food Facts API (timeout, lỗi mạng, máy chủ 5xx).
/// </summary>
public class OpenFoodFactsException : Exception
{
    public OpenFoodFactsException(string message) : base(message)
    {
    }

    public OpenFoodFactsException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

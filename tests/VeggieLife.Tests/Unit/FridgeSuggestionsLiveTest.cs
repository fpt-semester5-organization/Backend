using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VeggieLife.BLL.Services.Fridge;
using VeggieLife.DAL.Context;
using Xunit;
using Xunit.Abstractions;

namespace VeggieLife.Tests.Unit;

public class FridgeSuggestionsLiveTest
{
    private readonly ITestOutputHelper _output;

    public FridgeSuggestionsLiveTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task TestGetRecipeSuggestionsForUser107()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=VeggieLifeDb;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;

        using var db = new AppDbContext(options);
        if (!db.Database.CanConnect())
        {
            _output.WriteLine("Local SQL Server VeggieLifeDb is not reachable. Skipping test.");
            return;
        }

        var fridgeService = new FridgeService(db, null!, null!, null!, null!);

        var suggestions = await fridgeService.GetRecipeSuggestionsAsync(107, minSimilarity: 0.7, limit: 10);

        _output.WriteLine($"Suggestions count: {suggestions.Count}");
        foreach (var s in suggestions)
        {
            _output.WriteLine($"Recipe #{s.RecipeId}: {s.Title} | Similarity: {s.Similarity:P0} | UsesNonFresh: {s.UsesNonFreshItems}");
            if (s.MissingIngredients != null && s.MissingIngredients.Count > 0)
            {
                _output.WriteLine($"   Missing: {string.Join(", ", s.MissingIngredients.Select(m => m.Name))}");
            }
        }

        Assert.NotEmpty(suggestions);
    }
}

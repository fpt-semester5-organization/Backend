using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using VeggieLife.DAL.Context;
using Xunit;
using Xunit.Abstractions;

namespace VeggieLife.Tests.Unit;

public class DbEncodingTest
{
    private readonly ITestOutputHelper _output;

    public DbEncodingTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TestReadEnumberData()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=VeggieLifeDb;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;

        using var db = new AppDbContext(options);
        var items = db.EnumberDictionaries.Take(5).ToList();
        foreach (var item in items)
        {
            _output.WriteLine($"Code: {item.Code} | Name: {item.Name} | Category: {item.CategoryType} | Origin: {item.OriginSource}");
        }

        Assert.NotEmpty(items);
        Assert.Contains("nghệ", items.First(x => x.Code == "E100").OriginSource);
    }
}

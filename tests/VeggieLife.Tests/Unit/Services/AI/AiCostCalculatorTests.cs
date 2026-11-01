using System;
using VeggieLife.BLL.Services.AI;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.AI;

public class AiCostCalculatorTests
{
    [Fact]
    public void Calculate_ZeroTokens_ReturnsZero()
    {
        var cost = AiCostCalculator.Calculate(0, 0);
        Assert.Equal(0.0000m, cost);
    }

    [Fact]
    public void Calculate_NegativeTokens_ReturnsZero()
    {
        var cost = AiCostCalculator.Calculate(-10, -5);
        Assert.Equal(0.0000m, cost);
    }

    [Fact]
    public void Calculate_MinimalTokens_ReturnsFloorOfOneTenThousandth()
    {
        // 10 prompt + 5 completion -> raw is 0.00000225 USD
        // Should hit minimum floor of 0.0001m
        var cost = AiCostCalculator.Calculate(10, 5);
        Assert.Equal(0.0001m, cost);
    }

    [Fact]
    public void Calculate_RealisticChatQuery_RoundsCorrectly()
    {
        // 800 prompt * 0.075 = 60
        // 350 completion * 0.300 = 105
        // Total = 165 / 1,000,000 = 0.000165 USD
        // Round to 4 decimal places away from zero -> 0.0002m
        var cost = AiCostCalculator.Calculate(800, 350);
        Assert.Equal(0.0002m, cost);
    }

    [Fact]
    public void Calculate_LargeRagPrompt_ComputesAccurateCost()
    {
        // 50,000 prompt * 0.075 = 3,750
        // 1,000 completion * 0.300 = 300
        // Total = 4,050 / 1,000,000 = 0.004050 USD
        // Round to 4 decimal places -> 0.0041m
        var cost = AiCostCalculator.Calculate(50_000, 1_000);
        Assert.Equal(0.0041m, cost);
    }

    [Fact]
    public void CalculateFromTotal_Zero_ReturnsZero()
    {
        var cost = AiCostCalculator.CalculateFromTotal(0);
        Assert.Equal(0.0000m, cost);
    }

    [Fact]
    public void CalculateFromTotal_TypicalRecipeTokens_ReturnsPositiveCost()
    {
        // 1000 tokens estimated 70/30 (700 prompt, 300 completion)
        // 700 * 0.075 = 52.5
        // 300 * 0.300 = 90
        // Total = 142.5 / 1,000,000 = 0.0001425 USD
        // Round to 4 decimals -> 0.0001m
        var cost = AiCostCalculator.CalculateFromTotal(1_000);
        Assert.Equal(0.0001m, cost);
    }
}

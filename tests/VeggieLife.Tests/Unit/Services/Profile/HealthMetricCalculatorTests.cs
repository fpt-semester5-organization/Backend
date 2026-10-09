using System;
using FluentAssertions;
using VeggieLife.BLL.Services.Profile;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Profile;

/// <summary>
/// Bộ kiểm thử độc lập đối chiếu công thức tính toán chỉ số sức khỏe với kết quả tính tay (BR-USR-02, BR-USR-NUT-03, BR-SCI-03).
/// </summary>
public class HealthMetricCalculatorTests
{
    [Fact]
    public void CalculateBmi_ShouldMatchHandCalculation()
    {
        // 54.5 kg / (1.62 m)^2 = 54.5 / 2.6244 = 20.7666... -> 20.8
        var bmi = HealthMetricCalculator.CalculateBmi(162m, 54.5m);
        bmi.Should().Be(20.8m);
    }

    [Theory]
    [InlineData(18.4, "UNDERWEIGHT")]
    [InlineData(18.5, "NORMAL")]
    [InlineData(20.8, "NORMAL")]
    [InlineData(22.9, "NORMAL")]
    [InlineData(23.0, "OVERWEIGHT")]
    [InlineData(24.9, "OVERWEIGHT")]
    [InlineData(25.0, "OBESE")]
    [InlineData(32.5, "OBESE")]
    public void GetBmiCategory_ShouldMatchAsianWproThresholds(decimal bmi, string expectedCategory)
    {
        var category = HealthMetricCalculator.GetBmiCategory(bmi);
        category.Should().Be(expectedCategory);
    }

    [Fact]
    public void CalculateBmr_Female_ShouldMatchMifflinStJeorFormula()
    {
        // Nữ: 10 * 54.5 + 6.25 * 162 - 5 * 19 - 161 = 545 + 1012.5 - 95 - 161 = 1301.5
        var bmr = HealthMetricCalculator.CalculateBmr(162m, 54.5m, 19, "FEMALE");
        bmr.Should().Be(1301.5m);
    }

    [Fact]
    public void CalculateBmr_Male_ShouldMatchMifflinStJeorFormula()
    {
        // Nam: 10 * 70 + 6.25 * 175 - 5 * 30 + 5 = 700 + 1093.75 - 150 + 5 = 1648.75 -> 1648.8
        var bmr = HealthMetricCalculator.CalculateBmr(175m, 70m, 30, "MALE");
        bmr.Should().Be(1648.8m);
    }

    [Fact]
    public void CalculateTdee_ShouldMultiplyBmrByOnePointTwo()
    {
        // 1301.5 * 1.2 = 1561.8
        var tdee = HealthMetricCalculator.CalculateTdee(1301.5m);
        tdee.Should().Be(1561.8m);
    }

    [Fact]
    public void CalculateTargetDailyCalories_Female_LoseWeight_ShouldEnforceFloor1200()
    {
        // TDEE = 1561.8. LOSE_WEIGHT: 1561.8 - 400 = 1161.8 < Sàn 1200 Nữ -> Kết quả phải là 1200.0
        var targetCalories = HealthMetricCalculator.CalculateTargetDailyCalories(1561.8m, "LOSE_WEIGHT", "FEMALE");
        targetCalories.Should().Be(1200.0m);
    }

    [Fact]
    public void CalculateTargetDailyCalories_Male_LoseWeight_ShouldEnforceFloor1500()
    {
        // TDEE = 1700. LOSE_WEIGHT: 1700 - 400 = 1300 < Sàn 1500 Nam -> Kết quả phải là 1500.0
        var targetCalories = HealthMetricCalculator.CalculateTargetDailyCalories(1700.0m, "LOSE_WEIGHT", "MALE");
        targetCalories.Should().Be(1500.0m);
    }

    [Fact]
    public void CalculateTargetDailyCalories_Maintain_ShouldEqualTdee()
    {
        var targetCalories = HealthMetricCalculator.CalculateTargetDailyCalories(2000.0m, "MAINTAIN", "MALE");
        targetCalories.Should().Be(2000.0m);
    }

    [Fact]
    public void CalculateTargetDailyCalories_GainMuscle_ShouldAdd400()
    {
        var targetCalories = HealthMetricCalculator.CalculateTargetDailyCalories(1800.0m, "GAIN_MUSCLE", "FEMALE");
        targetCalories.Should().Be(2200.0m);
    }

    [Fact]
    public void CalculateAge_ShouldAccuratelyDetermineAge()
    {
        var dob = new DateOnly(2000, 5, 15);
        var asOfBeforeBirthday = new DateOnly(2026, 5, 14);
        var asOfOnBirthday = new DateOnly(2026, 5, 15);
        var asOfAfterBirthday = new DateOnly(2026, 5, 16);

        HealthMetricCalculator.CalculateAge(dob, asOfBeforeBirthday).Should().Be(25);
        HealthMetricCalculator.CalculateAge(dob, asOfOnBirthday).Should().Be(26);
        HealthMetricCalculator.CalculateAge(dob, asOfAfterBirthday).Should().Be(26);
    }
}

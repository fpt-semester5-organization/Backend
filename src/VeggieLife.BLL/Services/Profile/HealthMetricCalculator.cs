using System;

namespace VeggieLife.BLL.Services.Profile;

/// <summary>
/// Bộ tính toán các chỉ số sức khỏe và dinh dưỡng tự động theo chuẩn WHO/WPRO Châu Á và Mifflin-St Jeor.
/// Tuân thủ nghiêm ngặt các quy tắc nghiệp vụ: BR-USR-02, BR-USR-NUT-03, BR-SCI-03.
/// </summary>
public static class HealthMetricCalculator
{
    /// <summary>
    /// Tính tuổi hiện tại từ ngày sinh (BR-USR-01: tuổi ≥ 10).
    /// </summary>
    public static int CalculateAge(DateOnly dateOfBirth, DateOnly? asOfDate = null)
    {
        var today = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth > today.AddYears(-age))
        {
            age--;
        }
        return age;
    }

    /// <summary>
    /// Tính chỉ số khối cơ thể (BMI) = Cân nặng (kg) / (Chiều cao (m))^2 (BR-USR-02).
    /// Làm tròn 1 chữ số thập phân.
    /// </summary>
    public static decimal CalculateBmi(decimal heightCm, decimal weightKg)
    {
        if (heightCm <= 0 || weightKg <= 0)
        {
            return 0;
        }

        var heightM = heightCm / 100m;
        var bmi = weightKg / (heightM * heightM);
        return Math.Round(bmi, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Phân loại mức độ thể trạng theo thang đo WHO/WPRO dành riêng cho người Châu Á (BR-SCI-03, BR-USR-02):
    /// - Thiếu cân (UNDERWEIGHT): BMI &lt; 18.5
    /// - Bình thường (NORMAL): 18.5 ≤ BMI ≤ 22.9
    /// - Tiền béo phì (OVERWEIGHT): 23.0 ≤ BMI ≤ 24.9
    /// - Béo phì (OBESE): BMI ≥ 25.0
    /// </summary>
    public static string GetBmiCategory(decimal bmi)
    {
        if (bmi < 18.5m)
        {
            return "UNDERWEIGHT";
        }

        if (bmi <= 22.9m)
        {
            return "NORMAL";
        }

        if (bmi <= 24.9m)
        {
            return "OVERWEIGHT";
        }

        return "OBESE";
    }

    /// <summary>
    /// Tính tỷ lệ trao đổi chất cơ bản (BMR) theo công thức Mifflin-St Jeor (BR-USR-NUT-03):
    /// - Nam: 10 × Cân nặng (kg) + 6.25 × Chiều cao (cm) − 5 × Tuổi + 5
    /// - Nữ: 10 × Cân nặng (kg) + 6.25 × Chiều cao (cm) − 5 × Tuổi − 161
    /// Làm tròn 1 chữ số thập phân.
    /// </summary>
    public static decimal CalculateBmr(decimal heightCm, decimal weightKg, int age, string gender)
    {
        var isMale = string.Equals(gender, "MALE", StringComparison.OrdinalIgnoreCase);
        var genderConstant = isMale ? 5m : -161m;

        var bmr = (10m * weightKg) + (6.25m * heightCm) - (5m * age) + genderConstant;
        return Math.Round(bmr, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Tính tổng năng lượng tiêu hao hàng ngày (TDEE) = BMR × 1.2 (BR-USR-NUT-03).
    /// Hệ thống không thu thập mức độ vận động (D61), áp dụng hệ số ít vận động 1.2.
    /// Làm tròn 1 chữ số thập phân.
    /// </summary>
    public static decimal CalculateTdee(decimal bmr)
    {
        var tdee = bmr * 1.2m;
        return Math.Round(tdee, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Tính năng lượng calo mục tiêu mỗi ngày theo mục tiêu sức khỏe (BR-USR-NUT-03):
    /// - Giảm cân (LOSE_WEIGHT): TDEE − 400 kcal
    /// - Giữ cân (MAINTAIN): TDEE kcal
    /// - Tăng cơ / tăng cân (GAIN_MUSCLE): TDEE + 400 kcal
    /// Ràng buộc sàn tối thiểu: Không thấp hơn 1200 kcal đối với Nữ, không thấp hơn 1500 kcal đối với Nam.
    /// Làm tròn 1 chữ số thập phân.
    /// </summary>
    public static decimal CalculateTargetDailyCalories(decimal tdee, string healthGoal, string gender)
    {
        var targetCalories = healthGoal.ToUpperInvariant() switch
        {
            "LOSE_WEIGHT" => tdee - 400m,
            "GAIN_MUSCLE" => tdee + 400m,
            _ => tdee // MAINTAIN hoặc mặc định
        };

        var isMale = string.Equals(gender, "MALE", StringComparison.OrdinalIgnoreCase);
        var minCalories = isMale ? 1500m : 1200m;

        if (targetCalories < minCalories)
        {
            targetCalories = minCalories;
        }

        return Math.Round(targetCalories, 1, MidpointRounding.AwayFromZero);
    }
}

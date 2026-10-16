using VeggieLife.BLL.Validators.Auth;
using Xunit;

namespace VeggieLife.Tests.Unit.Services;

/// <summary>
/// Bảng kiểm thử chính sách an toàn mật khẩu theo chuẩn BR-AUTH-001 và SEC-1:
/// Độ dài tối thiểu 8 ký tự, bao gồm ít nhất:
/// - 01 chữ cái viết hoa (A-Z)
/// - 01 chữ cái viết thường (a-z)
/// - 01 chữ số (0-9)
/// - 01 ký tự đặc biệt (ví dụ: !@#$%^&*...)
/// </summary>
public class PasswordValidatorTests
{
    [Theory]
    // --- 1. Ca kiểm thử HỢP LỆ (Valid Passwords) ---
    [InlineData("Veggie@2026", true, null)]                           // Mật khẩu chuẩn mặc định của hệ thống
    [InlineData("NewPass@1", true, null)]                            // Vừa đúng 8 ký tự (Boundary min: 8 chars)
    [InlineData("Abcdef1!", true, null)]                             // Đúng 8 ký tự tối thiểu
    [InlineData("P@ssw0rd2026!", true, null)]                        // Độ dài trung bình, nhiều ký tự đặc biệt
    [InlineData("VeggieLife#Vip_2026$", true, null)]                 // Mật khẩu dài (20 ký tự)
    [InlineData("A1!a" + "b" + "c" + "d" + "e", true, null)]         // Đúng 8 ký tự có đủ 4 loại
    [InlineData("SuperSecurePassword@2026_WithManyCharacters#99", true, null)] // Rất dài (> 40 ký tự)
    [InlineData("A1!aaaaa", true, null)]                             // Biên 8 ký tự
    [InlineData("Z9#zzzzz", true, null)]                             // Biên 8 ký tự
    [InlineData("Ab1#567890", true, null)]                           // 10 ký tự
    [InlineData("Test@1234", true, null)]                            // 9 ký tự

    // --- 2. Ca kiểm thử ĐỂ TRỐNG / NULL / KHOẢNG TRẮNG ---
    [InlineData(null, false, "Mật khẩu không được để trống.")]
    [InlineData("", false, "Mật khẩu không được để trống.")]
    [InlineData("   ", false, "Mật khẩu không được để trống.")]
    [InlineData("\t\n", false, "Mật khẩu không được để trống.")]

    // --- 3. Ca kiểm thử ĐỘ DÀI DƯỚI 8 KÝ TỰ (Length < 8) ---
    [InlineData("A1!a", false, "Mật khẩu phải có độ dài tối thiểu 8 ký tự.")]        // 4 ký tự
    [InlineData("Abc1!", false, "Mật khẩu phải có độ dài tối thiểu 8 ký tự.")]       // 5 ký tự
    [InlineData("Abcd1!", false, "Mật khẩu phải có độ dài tối thiểu 8 ký tự.")]      // 6 ký tự
    [InlineData("Abcde1!", false, "Mật khẩu phải có độ dài tối thiểu 8 ký tự.")]     // 7 ký tự (Boundary: ngay dưới 8)

    // --- 4. Ca kiểm thử THIẾU CHỮ HOA (Missing Uppercase) ---
    [InlineData("veggie@2026", false, "Mật khẩu phải chứa ít nhất 01 chữ cái viết hoa (A-Z).")]
    [InlineData("password123!", false, "Mật khẩu phải chứa ít nhất 01 chữ cái viết hoa (A-Z).")]
    [InlineData("abcdef1#", false, "Mật khẩu phải chứa ít nhất 01 chữ cái viết hoa (A-Z).")]

    // --- 5. Ca kiểm thử THIẾU CHỮ THƯỜNG (Missing Lowercase) ---
    [InlineData("VEGGIE@2026", false, "Mật khẩu phải chứa ít nhất 01 chữ cái viết thường (a-z).")]
    [InlineData("PASSWORD123!", false, "Mật khẩu phải chứa ít nhất 01 chữ cái viết thường (a-z).")]
    [InlineData("ABCDEF1#", false, "Mật khẩu phải chứa ít nhất 01 chữ cái viết thường (a-z).")]

    // --- 6. Ca kiểm thử THIẾU CHỮ SỐ (Missing Digit) ---
    [InlineData("VeggieLife@Vegan", false, "Mật khẩu phải chứa ít nhất 01 chữ số (0-9).")]
    [InlineData("Password@Special", false, "Mật khẩu phải chứa ít nhất 01 chữ số (0-9).")]
    [InlineData("Abcdefgh!", false, "Mật khẩu phải chứa ít nhất 01 chữ số (0-9).")]

    // --- 7. Ca kiểm thử THIẾU KÝ TỰ ĐẶC BIỆT (Missing Special Character) ---
    [InlineData("VeggieLife2026", false, "Mật khẩu phải chứa ít nhất 01 ký tự đặc biệt (ví dụ: !@#$%^&*...).")]
    [InlineData("Password12345", false, "Mật khẩu phải chứa ít nhất 01 ký tự đặc biệt (ví dụ: !@#$%^&*...).")]
    [InlineData("Abcdefgh12", false, "Mật khẩu phải chứa ít nhất 01 ký tự đặc biệt (ví dụ: !@#$%^&*...).")]
    public void Validate_BR_AUTH_001_PasswordPolicy_Matrix(string? password, bool expectedIsValid, string? expectedErrorPart)
    {
        // ACT
        var (isValid, errorMessage) = PasswordValidator.Validate(password);

        // ASSERT
        Assert.Equal(expectedIsValid, isValid);

        if (expectedIsValid)
        {
            Assert.Null(errorMessage);
        }
        else
        {
            Assert.NotNull(errorMessage);
            Assert.Contains(expectedErrorPart!, errorMessage);
        }
    }

    [Theory]
    [InlineData("Veggie!2026", "!")]
    [InlineData("Veggie@2026", "@")]
    [InlineData("Veggie#2026", "#")]
    [InlineData("Veggie$2026", "$")]
    [InlineData("Veggie%2026", "%")]
    [InlineData("Veggie^2026", "^")]
    [InlineData("Veggie&2026", "&")]
    [InlineData("Veggie*2026", "*")]
    [InlineData("Veggie(2026", "(")]
    [InlineData("Veggie)2026", ")")]
    [InlineData("Veggie_2026", "_")]
    [InlineData("Veggie-2026", "-")]
    [InlineData("Veggie+2026", "+")]
    [InlineData("Veggie=2026", "=")]
    [InlineData("Veggie[2026", "[")]
    [InlineData("Veggie]2026", "]")]
    [InlineData("Veggie{2026", "{")]
    [InlineData("Veggie}2026", "}")]
    [InlineData("Veggie|2026", "|")]
    [InlineData("Veggie;2026", ";")]
    [InlineData("Veggie:2026", ":")]
    [InlineData("Veggie,2026", ",")]
    [InlineData("Veggie.2026", ".")]
    [InlineData("Veggie<2026", "<")]
    [InlineData("Veggie>2026", ">")]
    [InlineData("Veggie?2026", "?")]
    [InlineData("Veggie/2026", "/")]
    [InlineData("Veggie~2026", "~")]
    public void Validate_AcceptsVariousSpecialCharacters(string password, string specialChar)
    {
        // ACT
        var (isValid, errorMessage) = PasswordValidator.Validate(password);

        // ASSERT
        Assert.True(isValid, $"Mật khẩu chứa ký tự đặc biệt '{specialChar}' phải được chấp nhận hợp lệ.");
        Assert.Null(errorMessage);
    }
}

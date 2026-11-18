using System;
using System.Text.Json;
using FluentAssertions;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.ShoppingLists.Requests;
using Xunit;

namespace VeggieLife.Tests.Unit.Contract;

/// <summary>
/// BỘ KIỂM THỬ TƯƠNG THÍCH HỢP ĐỒNG API & JSON SCHEMA (API CONTRACT TESTING)
/// </summary>
public class ApiContractCompatibilityTestSuite
{
    [Fact]
    public void Contract_AppException_SerializationStructure_MustFollowStandardErrorEnvelope()
    {
        // ARRANGE: Tạo một ngoại lệ chuẩn hệ thống
        var appEx = AppException.BadRequest(ErrorCodes.ErrValidation, "Dữ liệu không hợp lệ");

        // ACT & ASSERT: Kiểm tra cấu trúc thông điệp
        appEx.StatusCode.Should().Be(400);
        appEx.ErrorCode.Should().Be("ERR_VALIDATION");
        appEx.Message.Should().Be("Dữ liệu không hợp lệ");
    }

    [Fact]
    public void Contract_ShoppingListRequest_JsonCamelCaseMapping()
    {
        // ARRANGE: JSON camelCase từ Frontend gửi sang
        var json = "{\"servingsMultiplier\": 3.5, \"targetShoppingDate\": \"2026-11-20\", \"daysCount\": 7}";
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // ACT
        var dto = JsonSerializer.Deserialize<CreateMealPlanShoppingListRequest>(json, options);

        // ASSERT
        dto.Should().NotBeNull();
        dto!.ServingsMultiplier.Should().Be(3.5m);
        dto.TargetShoppingDate.Should().Be(new DateOnly(2026, 11, 20));
        dto.DaysCount.Should().Be(7);
    }
}

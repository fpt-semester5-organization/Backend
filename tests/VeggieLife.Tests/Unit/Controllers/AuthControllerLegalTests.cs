using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VeggieLife.API.Controllers;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Auth;
using VeggieLife.BLL.Services.Auth;
using VeggieLife.BLL.Services.Guest;
using Xunit;

namespace VeggieLife.Tests.Unit.Controllers;

public class AuthControllerLegalTests
{
    private readonly Mock<IAuthService> _mockAuthService = new();
    private readonly Mock<IGuestService> _mockGuestService = new();
    private readonly Mock<IWebHostEnvironment> _mockEnv = new();
    private readonly AuthController _controller;

    public AuthControllerLegalTests()
    {
        _controller = new AuthController(
            _mockAuthService.Object,
            _mockGuestService.Object,
            _mockEnv.Object);
    }

    [Fact]
    public async Task GetLegalDocuments_WhenCalled_ReturnsOkApiResponseWithLegalDocuments()
    {
        // Arrange
        var expectedData = new LegalDocumentsResponseData
        {
            UserAgreementUrl = "https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Ban_Cam_Ket_Va_Thoa_Thuan_Dang_Ky_Nguoi_Dung.png",
            PrivacyPolicyUrl = "https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Chinh_Sach_Bao_Mat_Du_Lieu_VeggieLife.png",
            TermsOfServiceUrl = "https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Dieu_Khoan_Su_Dung_Dich_Vu_VeggieLife.png",
            VeganDisclaimerUrl = "https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Quy_Chuan_Am_Thuc_Chay_Va_Mien_Tru_Y_Te_VeggieLife.png",
            MedicalDisclaimer = "Khuyến nghị mang tính chất tham khảo dinh dưỡng, vui lòng tham vấn ý kiến bác sĩ chuyên khoa đối với các bệnh lý nền.",
            UpdatedAt = DateTime.UtcNow
        };

        _mockAuthService.Setup(s => s.GetLegalDocumentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedData);

        // Act
        var actionResult = await _controller.GetLegalDocuments(CancellationToken.None);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<LegalDocumentsResponseData>>().Subject;

        apiResponse.StatusCode.Should().Be(200);
        apiResponse.Message.Should().Be("Tra cứu tài liệu pháp lý thành công.");
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.UserAgreementUrl.Should().Be(expectedData.UserAgreementUrl);
        apiResponse.Data.PrivacyPolicyUrl.Should().Be(expectedData.PrivacyPolicyUrl);
        apiResponse.Data.TermsOfServiceUrl.Should().Be(expectedData.TermsOfServiceUrl);
        apiResponse.Data.VeganDisclaimerUrl.Should().Be(expectedData.VeganDisclaimerUrl);
        apiResponse.Data.MedicalDisclaimer.Should().Be(expectedData.MedicalDisclaimer);
    }
}


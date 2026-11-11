using System.Collections.Generic;
using FluentAssertions;
using VeggieLife.DAL.External.OpenFoodFacts;
using Xunit;

namespace VeggieLife.Tests.Unit.DAL;

/// <summary>
/// Kiểm thử đơn vị cho OpenFoodFactsClient và hàm trích xuất ảnh sản phẩm (FN-BAR-01).
/// Đảm bảo hỗ trợ đầy đủ các định dạng ảnh từ Open Food Facts:
/// image_front_url, image_url, image_front_small_url, selected_images, images fallback và chuẩn hóa HTTPS.
/// </summary>
public class OpenFoodFactsClientTests
{
    [Fact]
    public void ParseOpenFoodFactsResponse_ValidDirectImageFrontUrl_ReturnsCorrectImageUrl()
    {
        // Arrange
        var barcode = "8934563138165";
        var json = """
        {
            "status": 1,
            "product": {
                "product_name": "Hảo Hảo Mì Tôm Chua Cay",
                "brands": "Acecook",
                "image_front_url": "https://images.openfoodfacts.org/images/products/893/456/313/8165/front_vi.38.400.jpg",
                "ingredients_text": "Bột mì, dầu cọ, muối, tôm",
                "ingredients_analysis_tags": ["en:non-vegan"]
            }
        }
        """;

        // Act
        var result = OpenFoodFactsClient.ParseOpenFoodFactsResponse(barcode, json);

        // Assert
        result.Should().NotBeNull();
        result!.Barcode.Should().Be(barcode);
        result.ProductName.Should().Be("Hảo Hảo Mì Tôm Chua Cay");
        result.Brand.Should().Be("Acecook");
        result.ImageUrl.Should().Be("https://images.openfoodfacts.org/images/products/893/456/313/8165/front_vi.38.400.jpg");
        result.VeganVerdict.Should().Be("NON_VEGAN");
    }

    [Fact]
    public void ParseOpenFoodFactsResponse_NullImageUrlWithValidImageFrontUrl_FallsBackToFrontUrl()
    {
        // Arrange - Trường hợp phổ biến: trường image_url là null hoặc rỗng, nhưng image_front_url có giá trị
        var barcode = "3017620422003";
        var json = """
        {
            "status": 1,
            "product": {
                "product_name": "Nutella",
                "image_url": null,
                "image_front_url": "https://images.openfoodfacts.org/images/products/301/762/042/2003/front_en.879.400.jpg"
            }
        }
        """;

        // Act
        var result = OpenFoodFactsClient.ParseOpenFoodFactsResponse(barcode, json);

        // Assert
        result.Should().NotBeNull();
        result!.ImageUrl.Should().Be("https://images.openfoodfacts.org/images/products/301/762/042/2003/front_en.879.400.jpg");
    }

    [Fact]
    public void ParseOpenFoodFactsResponse_EmptyImageUrlWithValidImageFrontUrl_FallsBackToFrontUrl()
    {
        // Arrange - image_url là chuỗi rỗng
        var barcode = "3017620422003";
        var json = """
        {
            "status": 1,
            "product": {
                "product_name": "Nutella",
                "image_url": "   ",
                "image_front_url": "https://images.openfoodfacts.org/images/products/301/762/042/2003/front_en.879.400.jpg"
            }
        }
        """;

        // Act
        var result = OpenFoodFactsClient.ParseOpenFoodFactsResponse(barcode, json);

        // Assert
        result.Should().NotBeNull();
        result!.ImageUrl.Should().Be("https://images.openfoodfacts.org/images/products/301/762/042/2003/front_en.879.400.jpg");
    }

    [Fact]
    public void ParseOpenFoodFactsResponse_SelectedImagesFrontDisplayVi_ExtractsVietnameseImage()
    {
        // Arrange - Trích xuất từ selected_images.front.display.vi
        var barcode = "8935049501503";
        var json = """
        {
            "status": 1,
            "product": {
                "product_name_vi": "Coca-Cola Không Đường",
                "selected_images": {
                    "front": {
                        "display": {
                            "vi": "https://images.openfoodfacts.org/images/products/893/504/950/1503/front_vi.400.jpg",
                            "en": "https://images.openfoodfacts.org/images/products/893/504/950/1503/front_en.400.jpg"
                        }
                    }
                }
            }
        }
        """;

        // Act
        var result = OpenFoodFactsClient.ParseOpenFoodFactsResponse(barcode, json);

        // Assert
        result.Should().NotBeNull();
        result!.ProductName.Should().Be("Coca-Cola Không Đường");
        result.ImageUrl.Should().Be("https://images.openfoodfacts.org/images/products/893/504/950/1503/front_vi.400.jpg");
    }

    [Fact]
    public void ParseOpenFoodFactsResponse_SelectedImagesFrontSmall_ExtractsWhenDisplayMissing()
    {
        // Arrange
        var barcode = "8935049501503";
        var json = """
        {
            "status": 1,
            "product": {
                "product_name": "Nước ngọt",
                "selected_images": {
                    "front": {
                        "small": {
                            "en": "https://images.openfoodfacts.org/images/products/893/504/950/1503/front_en.200.jpg"
                        }
                    }
                }
            }
        }
        """;

        // Act
        var result = OpenFoodFactsClient.ParseOpenFoodFactsResponse(barcode, json);

        // Assert
        result.Should().NotBeNull();
        result!.ImageUrl.Should().Be("https://images.openfoodfacts.org/images/products/893/504/950/1503/front_en.200.jpg");
    }

    [Fact]
    public void ParseOpenFoodFactsResponse_ImagesObjectFallback_BuildsCorrectCdnUrl()
    {
        // Arrange - Không có direct url hay selected_images, chỉ có object images
        var barcode = "8934563138165";
        var json = """
        {
            "status": 1,
            "product": {
                "product_name": "Mì ăn liền",
                "images": {
                    "front_vi": { "uploader": "user1" },
                    "1": { "uploader": "user2" }
                }
            }
        }
        """;

        // Act
        var result = OpenFoodFactsClient.ParseOpenFoodFactsResponse(barcode, json);

        // Assert
        result.Should().NotBeNull();
        result!.ImageUrl.Should().Be("https://images.openfoodfacts.org/images/products/893/456/313/8165/front_vi.400.jpg");
    }

    [Fact]
    public void ParseOpenFoodFactsResponse_ShortBarcode_ImagesFallbackBuildsCorrectPath()
    {
        // Arrange - Mã vạch ngắn EAN-8
        var barcode = "12345678";
        var json = """
        {
            "status": 1,
            "product": {
                "product_name": "Kẹo gum",
                "images": {
                    "front": {}
                }
            }
        }
        """;

        // Act
        var result = OpenFoodFactsClient.ParseOpenFoodFactsResponse(barcode, json);

        // Assert
        result.Should().NotBeNull();
        result!.ImageUrl.Should().Be("https://images.openfoodfacts.org/images/products/12345678/front.400.jpg");
    }

    [Fact]
    public void ParseOpenFoodFactsResponse_HttpUrl_UpgradesToHttps()
    {
        // Arrange
        var barcode = "3017620422003";
        var json = """
        {
            "status": 1,
            "product": {
                "product_name": "Nutella",
                "image_front_url": "http://images.openfoodfacts.org/images/products/301/762/042/2003/front.jpg"
            }
        }
        """;

        // Act
        var result = OpenFoodFactsClient.ParseOpenFoodFactsResponse(barcode, json);

        // Assert
        result.Should().NotBeNull();
        result!.ImageUrl.Should().Be("https://images.openfoodfacts.org/images/products/301/762/042/2003/front.jpg");
    }

    [Fact]
    public void ParseOpenFoodFactsResponse_StatusAsString_ParsesCorrectly()
    {
        // Arrange - Open Food Facts đôi khi trả status dưới dạng chuỗi "1"
        var barcode = "8934563138165";
        var json = """
        {
            "status": "1",
            "product": {
                "product_name": "Mì Hảo Hảo",
                "image_front_url": "https://images.openfoodfacts.org/img.jpg"
            }
        }
        """;

        // Act
        var result = OpenFoodFactsClient.ParseOpenFoodFactsResponse(barcode, json);

        // Assert
        result.Should().NotBeNull();
        result!.ProductName.Should().Be("Mì Hảo Hảo");
        result.ImageUrl.Should().Be("https://images.openfoodfacts.org/img.jpg");
    }

    [Fact]
    public void ParseOpenFoodFactsResponse_StatusZero_ReturnsNull()
    {
        // Arrange - Sản phẩm không tồn tại (status = 0)
        var barcode = "9999999999999";
        var json = """
        {
            "status": 0,
            "status_verbose": "product not found"
        }
        """;

        // Act
        var result = OpenFoodFactsClient.ParseOpenFoodFactsResponse(barcode, json);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ParseOpenFoodFactsResponse_TruncatedDefaultIngredients_UsesMoreCompleteLatinLanguage()
    {
        // Arrange: Nutella — ingredients_text tiếng Anh chỉ còn "sugar, palm oil"
        var json = """
        {
            "status": 1,
            "product": {
                "product_name": "Nutella",
                "ingredients_text": "sugar, palm oil",
                "ingredients_text_en": "sugar, palm oil",
                "ingredients_text_fr": "Sucre, huile de palme, NOISETTES 13%, cacao maigre 7,4%, LAIT écrémé en poudre 6,6%, LACTOSERUM en poudre",
                "ingredients_text_ar": "سكر ، زيت نخيل ، بندق 13٪ ، كاكاو قليل الدهن 7.4٪ ، مسحوق حليب منزوع الدسم 6.6٪ ، مسحوق WHEY ، مستحلبات ليسيثين الصويا"
            }
        }
        """;

        // Act
        var result = OpenFoodFactsClient.ParseOpenFoodFactsResponse("3017620422003", json);

        // Assert: chọn bản tiếng Pháp đầy đủ (chữ Latinh) thay vì bản tiếng Anh bị cụt hay bản tiếng Ả Rập
        result!.IngredientsText.Should().StartWith("Sucre, huile de palme");
    }

    [Fact]
    public void ParseOpenFoodFactsResponse_VietnameseIngredientsPresent_KeepsVietnamese()
    {
        var json = """
        {
            "status": 1,
            "product": {
                "product_name": "Mì chay",
                "ingredients_text_vi": "Bột mì, dầu cọ, muối, rau củ sấy",
                "ingredients_text_en": "Wheat flour, palm oil, salt, dried vegetables"
            }
        }
        """;

        var result = OpenFoodFactsClient.ParseOpenFoodFactsResponse("8930000000020", json);

        result!.IngredientsText.Should().Be("Bột mì, dầu cọ, muối, rau củ sấy");
    }
}

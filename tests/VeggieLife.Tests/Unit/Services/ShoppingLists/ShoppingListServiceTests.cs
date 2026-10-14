using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.ShoppingLists.Requests;
using VeggieLife.BLL.Services.ShoppingLists;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.ShoppingLists;

/// <summary>
/// Bộ kiểm thử đơn vị cho ShoppingListService (FN-MEAL-05, FN-MEAL-06).
/// Kiểm tra:
/// 1. Sinh danh sách đi chợ từ thực đơn tuần ACTIVE, cộng dồn nguyên liệu trùng, chuyển đổi đơn vị và khớp 100% với tính tay.
/// 2. Bắt lỗi khi thực đơn chưa ACTIVE (ERR_MEAL_PLAN_NOT_ACTIVE) hoặc đã có danh sách (ERR_SHOPPING_LIST_EXISTS).
/// 3. Xem danh sách đi chợ theo 4 quầy, tính đúng tiến độ đã mua / tổng số.
/// 4. Đánh dấu món đã mua; tự động cập nhật COMPLETED khi mua hết, quay lại PENDING khi bỏ chọn.
/// 5. Cập nhật trạng thái danh sách COMPLETED / CANCELLED và chặn trạng thái không hợp lệ.
/// </summary>
public class ShoppingListServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ShoppingListService _service;

    public ShoppingListServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new ShoppingListService(_db);

        SeedCommonData();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private void SeedCommonData()
    {
        _db.Ingredients.AddRange(
            new Ingredient
            {
                IngredientId = 1,
                IngredientName = "Nấm rơm tươi",
                DefaultStallType = "PRODUCE",
                StandardUnit = "g",
                CaloriesPer100g = 31m,
                FoodGroup = "PLANT"
            },
            new Ingredient
            {
                IngredientId = 2,
                IngredientName = "Đậu hũ non",
                DefaultStallType = "PLANT_PROTEIN",
                StandardUnit = "g",
                CaloriesPer100g = 55m,
                FoodGroup = "PLANT"
            },
            new Ingredient
            {
                IngredientId = 3,
                IngredientName = "Nước tương",
                DefaultStallType = "CONDIMENTS",
                StandardUnit = "ml",
                CaloriesPer100g = 60m,
                FoodGroup = "PLANT"
            },
            new Ingredient
            {
                IngredientId = 4,
                IngredientName = "Nấm hương khô",
                DefaultStallType = "DRY_GOODS",
                StandardUnit = "g",
                CaloriesPer100g = 296m,
                FoodGroup = "PLANT"
            }
        );

        _db.SaveChanges();
    }

    [Fact]
    public async Task CreateMealPlanShoppingListAsync_ActivePlan_ShouldAggregateIngredientsAndGroupAisles_MatchManualCalculation()
    {
        // ARRANGE: Người dùng 10 có thực đơn ACTIVE gồm 2 món ăn
        var userId = 10;
        var planId = 100;

        var recipe1 = new Recipe
        {
            RecipeId = 201,
            Title = "Canh nấm đậu phụ",
            Instructions = "[]",
            Status = "APPROVED_ACTIVE",
            DifficultyLevel = "EASY",
            Source = "USER",
            Servings = 2,
            CaloriesKcal = 350m,
            RecipeIngredients = new List<RecipeIngredient>
            {
                new() { IngredientId = 1, QuantityNeeded = 200m, Unit = "g" }, // Nấm rơm: 200g
                new() { IngredientId = 2, QuantityNeeded = 150m, Unit = "g" }  // Đậu hũ non: 150g
            }
        };

        var recipe2 = new Recipe
        {
            RecipeId = 202,
            Title = "Nấm kho tiêu",
            Instructions = "[]",
            Status = "APPROVED_ACTIVE",
            DifficultyLevel = "EASY",
            Source = "USER",
            Servings = 2,
            CaloriesKcal = 250m,
            RecipeIngredients = new List<RecipeIngredient>
            {
                new() { IngredientId = 1, QuantityNeeded = 100m, Unit = "g" },      // Nấm rơm: 100g
                new() { IngredientId = 3, QuantityNeeded = 50m, Unit = "ml" },      // Nước tương: 50ml
                new() { IngredientId = 4, QuantityNeeded = 0.05m, Unit = "kg" }     // Nấm hương khô: 0.05kg -> 50g
            }
        };

        _db.Recipes.AddRange(recipe1, recipe2);

        var plan = new MealPlan
        {
            PlanId = planId,
            UserId = userId,
            PlanName = "Thực đơn tuần 42",
            StartDate = new DateOnly(2026, 10, 19),
            EndDate = new DateOnly(2026, 10, 25),
            PlanStatus = "ACTIVE",
            BudgetTier = "STANDARD",
            MealSlots = new List<MealSlot>
            {
                new() { SlotId = 1001, PlanId = planId, DayOfWeek = "MON", MealType = "LUNCH", Recipe = recipe1 },
                new() { SlotId = 1002, PlanId = planId, DayOfWeek = "TUE", MealType = "DINNER", Recipe = recipe2 }
            }
        };

        _db.MealPlans.Add(plan);
        await _db.SaveChangesAsync();

        // Multiplier = 2.0x (nhân đôi khẩu phần cho cả tuần)
        var request = new CreateMealPlanShoppingListRequest
        {
            ServingsMultiplier = 2.0m,
            TargetShoppingDate = new DateOnly(2026, 10, 18)
        };

        // ACT
        var result = await _service.CreateMealPlanShoppingListAsync(planId, userId, request);

        // ASSERT: So sánh với ví dụ tính tay:
        // - Nấm rơm (PRODUCE): (200 + 100) * 2 = 600g
        // - Đậu hũ non (PLANT_PROTEIN): 150 * 2 = 300g
        // - Nước tương (CONDIMENTS): 50 * 2 = 100ml
        // - Nấm hương khô (DRY_GOODS): 0.05kg * 1000 * 2 = 100g
        result.Should().NotBeNull();
        result.PlanId.Should().Be(planId);
        result.Status.Should().Be("PENDING");
        result.Aisles.Should().HaveCount(4);

        var produceAisle = result.Aisles.FirstOrDefault(a => a.Aisle == "PRODUCE");
        produceAisle.Should().NotBeNull();
        produceAisle!.Items.Should().HaveCount(1);
        produceAisle.Items[0].Name.Should().Be("Nấm rơm tươi");
        produceAisle.Items[0].Quantity.Should().Be(600m);
        produceAisle.Items[0].Unit.Should().Be("g");
        produceAisle.Items[0].IsPurchased.Should().BeFalse();
        produceAisle.Items[0].RecipeSources.Should().Contain(new[] { "Canh nấm đậu phụ", "Nấm kho tiêu" });

        var proteinAisle = result.Aisles.FirstOrDefault(a => a.Aisle == "PLANT_PROTEIN");
        proteinAisle.Should().NotBeNull();
        proteinAisle!.Items[0].Name.Should().Be("Đậu hũ non");
        proteinAisle.Items[0].Quantity.Should().Be(300m);
        proteinAisle.Items[0].Unit.Should().Be("g");

        var condimentsAisle = result.Aisles.FirstOrDefault(a => a.Aisle == "CONDIMENTS");
        condimentsAisle.Should().NotBeNull();
        condimentsAisle!.Items[0].Name.Should().Be("Nước tương");
        condimentsAisle.Items[0].Quantity.Should().Be(100m);
        condimentsAisle.Items[0].Unit.Should().Be("ml");

        var dryGoodsAisle = result.Aisles.FirstOrDefault(a => a.Aisle == "DRY_GOODS");
        dryGoodsAisle.Should().NotBeNull();
        dryGoodsAisle!.Items[0].Name.Should().Be("Nấm hương khô");
        dryGoodsAisle.Items[0].Quantity.Should().Be(100m);
        dryGoodsAisle.Items[0].Unit.Should().Be("g");

        // Kiểm tra dữ liệu thực tế đã lưu trong DB
        var savedList = await _db.ShoppingLists.Include(s => s.ShoppingListItems).FirstOrDefaultAsync(s => s.PlanId == planId);
        savedList.Should().NotBeNull();
        savedList!.Status.Should().Be("PENDING");
        savedList.ShoppingListItems.Should().HaveCount(4);
    }

    [Fact]
    public async Task CreateMealPlanShoppingListAsync_PlanNotActive_ShouldThrow409Conflict_MealPlanNotActive()
    {
        // ARRANGE: Thực đơn đang ở trạng thái DRAFT
        var userId = 11;
        var planId = 101;
        _db.MealPlans.Add(new MealPlan
        {
            PlanId = planId,
            UserId = userId,
            PlanName = "Thực đơn nháp",
            StartDate = new DateOnly(2026, 10, 19),
            EndDate = new DateOnly(2026, 10, 25),
            PlanStatus = "DRAFT",
            BudgetTier = "STANDARD"
        });
        await _db.SaveChangesAsync();

        // ACT & ASSERT: Báo lỗi 409 ERR_MEAL_PLAN_NOT_ACTIVE
        var act = () => _service.CreateMealPlanShoppingListAsync(planId, userId, new CreateMealPlanShoppingListRequest());
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(409);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrMealPlanNotActive);
        ex.Which.Message.Should().Contain("kích hoạt");
    }

    [Fact]
    public async Task CreateMealPlanShoppingListAsync_ShoppingListAlreadyExists_ShouldThrow409Conflict()
    {
        // ARRANGE: Thực đơn ACTIVE nhưng đã có shopping list
        var userId = 12;
        var planId = 102;
        _db.MealPlans.Add(new MealPlan
        {
            PlanId = planId,
            UserId = userId,
            PlanName = "Thực đơn đã có list",
            StartDate = new DateOnly(2026, 10, 19),
            EndDate = new DateOnly(2026, 10, 25),
            PlanStatus = "ACTIVE",
            BudgetTier = "STANDARD"
        });
        _db.ShoppingLists.Add(new ShoppingList
        {
            ListId = 50,
            PlanId = planId,
            ListTitle = "Danh sách cũ",
            TargetShoppingDate = new DateOnly(2026, 10, 19),
            Status = "PENDING"
        });
        await _db.SaveChangesAsync();

        // ACT & ASSERT: Báo lỗi 409 ERR_SHOPPING_LIST_EXISTS
        var act = () => _service.CreateMealPlanShoppingListAsync(planId, userId, new CreateMealPlanShoppingListRequest());
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(409);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrShoppingListExists);
    }

    [Fact]
    public async Task CreateMealPlanShoppingListAsync_NonExistentOrNotOwner_ShouldThrow404NotFound()
    {
        // ARRANGE
        var act = () => _service.CreateMealPlanShoppingListAsync(planId: 9999, userId: 1, new CreateMealPlanShoppingListRequest());
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrMealPlanNotFound);
    }

    [Fact]
    public async Task CreateMealPlanShoppingListAsync_InvalidServingsMultiplier_ShouldThrow400BadRequest()
    {
        var planId = 103;
        _db.MealPlans.Add(new MealPlan
        {
            PlanId = planId,
            UserId = 13,
            PlanName = "Test",
            StartDate = new DateOnly(2026, 10, 19),
            EndDate = new DateOnly(2026, 10, 25),
            PlanStatus = "ACTIVE",
            BudgetTier = "STANDARD"
        });
        await _db.SaveChangesAsync();

        // Multiplier < 1.0 hoặc > 10.0
        var actTooSmall = () => _service.CreateMealPlanShoppingListAsync(planId, 13, new CreateMealPlanShoppingListRequest { ServingsMultiplier = 0.5m });
        var ex1 = await actTooSmall.Should().ThrowAsync<AppException>();
        ex1.Which.StatusCode.Should().Be(400);
        ex1.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);

        var actTooLarge = () => _service.CreateMealPlanShoppingListAsync(planId, 13, new CreateMealPlanShoppingListRequest { ServingsMultiplier = 15.0m });
        var ex2 = await actTooLarge.Should().ThrowAsync<AppException>();
        ex2.Which.StatusCode.Should().Be(400);
        ex2.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task GetShoppingListByIdAsync_ShouldReturnProgressAndGroupedAisles()
    {
        // ARRANGE
        var userId = 20;
        var plan = new MealPlan
        {
            PlanId = 200,
            UserId = userId,
            PlanName = "Thực đơn có sẵn list",
            StartDate = new DateOnly(2026, 10, 19),
            EndDate = new DateOnly(2026, 10, 25),
            PlanStatus = "ACTIVE",
            BudgetTier = "STANDARD"
        };
        _db.MealPlans.Add(plan);

        var list = new ShoppingList
        {
            ListId = 60,
            PlanId = 200,
            ListTitle = "Danh sách đi chợ 60",
            TargetShoppingDate = new DateOnly(2026, 10, 19),
            Status = "PENDING",
            ShoppingListItems = new List<ShoppingListItem>
            {
                new() { ItemId = 601, IngredientId = 1, AggregatedQuantity = 300m, Unit = "g", IsPurchased = true },
                new() { ItemId = 602, IngredientId = 2, AggregatedQuantity = 200m, Unit = "g", IsPurchased = false },
                new() { ItemId = 603, IngredientId = 3, AggregatedQuantity = 100m, Unit = "ml", IsPurchased = false }
            }
        };
        _db.ShoppingLists.Add(list);
        await _db.SaveChangesAsync();

        // ACT
        var result = await _service.GetShoppingListByIdAsync(60, userId);

        // ASSERT: 1 đã mua, tổng 3 món
        result.Should().NotBeNull();
        result.ListId.Should().Be(60);
        result.Status.Should().Be("PENDING");
        result.Progress.Purchased.Should().Be(1);
        result.Progress.Total.Should().Be(3);
        result.Aisles.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PatchShoppingListItemAsync_MarkPurchased_ShouldUpdateItemAndAutoStatus()
    {
        // ARRANGE: Danh sách có 2 món
        var userId = 21;
        var plan = new MealPlan
        {
            PlanId = 201,
            UserId = userId,
            PlanName = "Plan 201",
            StartDate = new DateOnly(2026, 10, 19),
            EndDate = new DateOnly(2026, 10, 25),
            PlanStatus = "ACTIVE",
            BudgetTier = "STANDARD"
        };
        _db.MealPlans.Add(plan);

        var list = new ShoppingList
        {
            ListId = 70,
            PlanId = 201,
            ListTitle = "List 70",
            TargetShoppingDate = new DateOnly(2026, 10, 19),
            Status = "PENDING",
            ShoppingListItems = new List<ShoppingListItem>
            {
                new() { ItemId = 701, IngredientId = 1, AggregatedQuantity = 100m, Unit = "g", IsPurchased = false },
                new() { ItemId = 702, IngredientId = 2, AggregatedQuantity = 100m, Unit = "g", IsPurchased = true }
            }
        };
        _db.ShoppingLists.Add(list);
        await _db.SaveChangesAsync();

        // ACT 1: Đánh dấu món 701 là đã mua -> Lúc này cả 701 và 702 đều mua -> listStatus tự chuyển COMPLETED
        var res1 = await _service.PatchShoppingListItemAsync(70, 701, userId, new PatchShoppingListItemRequest { IsPurchased = true });
        res1.ItemId.Should().Be(701);
        res1.IsPurchased.Should().BeTrue();
        res1.ListStatus.Should().Be("COMPLETED");

        // ACT 2: Bỏ chọn món 702 (IsPurchased = false) -> listStatus tự chuyển về PENDING
        var res2 = await _service.PatchShoppingListItemAsync(70, 702, userId, new PatchShoppingListItemRequest { IsPurchased = false });
        res2.ItemId.Should().Be(702);
        res2.IsPurchased.Should().BeFalse();
        res2.ListStatus.Should().Be("PENDING");
    }

    [Fact]
    public async Task PatchShoppingListAsync_UpdateStatus_ShouldSucceed()
    {
        // ARRANGE
        var userId = 22;
        var plan = new MealPlan
        {
            PlanId = 202,
            UserId = userId,
            PlanName = "Plan 202",
            StartDate = new DateOnly(2026, 10, 19),
            EndDate = new DateOnly(2026, 10, 25),
            PlanStatus = "ACTIVE",
            BudgetTier = "STANDARD"
        };
        _db.MealPlans.Add(plan);

        var list = new ShoppingList
        {
            ListId = 80,
            PlanId = 202,
            ListTitle = "List 80",
            TargetShoppingDate = new DateOnly(2026, 10, 19),
            Status = "PENDING"
        };
        _db.ShoppingLists.Add(list);
        await _db.SaveChangesAsync();

        // ACT 1: Chuyển CANCELLED
        var resCancel = await _service.PatchShoppingListAsync(80, userId, new PatchShoppingListRequest { Status = "CANCELLED" });
        resCancel.Status.Should().Be("CANCELLED");

        // ACT 2: Chuyển COMPLETED
        var resComplete = await _service.PatchShoppingListAsync(80, userId, new PatchShoppingListRequest { Status = "COMPLETED" });
        resComplete.Status.Should().Be("COMPLETED");

        // ACT 3: Trạng thái không hợp lệ -> Báo lỗi 400 ERR_VALIDATION
        var actInvalid = () => _service.PatchShoppingListAsync(80, userId, new PatchShoppingListRequest { Status = "INVALID_STATUS" });
        var ex = await actInvalid.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VeggieLife.BLL.DTOs.ShoppingLists.Requests;
using VeggieLife.BLL.Services.ShoppingLists;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Workflow;

/// <summary>
/// BỘ KIỂM THỬ LUỒNG NGHIỆP VỤ LIÊN TỤC (END-TO-END WORKFLOW TESTING)
/// Luồng: Thực đơn Tuần Active -> Tự động sinh Danh sách đi chợ -> Đánh dấu mua từng món -> Hoàn tất danh sách.
/// </summary>
public class FullUserJourneyWorkflowTestSuite : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ShoppingListService _shoppingListService;

    public FullUserJourneyWorkflowTestSuite()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("WorkflowTestDb_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _shoppingListService = new ShoppingListService(_db);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Workflow_CompleteMealPlanToShoppingListLifecycle()
    {
        // BƯỚC 1: Chuẩn bị nguyên liệu & thực đơn tuần
        var user = new User { UserId = 88, Email = "workflow@veggielife.vn", FullName = "Nguyễn Văn Chay", AccountStatus = "ACTIVE", Role = "MEMBER" };
        var ingredient = new Ingredient { IngredientId = 50, IngredientName = "Hạt sen Huế", DefaultStallType = "DRY_GOODS", StandardUnit = "g", CaloriesPer100g = 350m, FoodGroup = "PLANT" };
        var recipe = new Recipe
        {
            RecipeId = 500,
            Title = "Chè hạt sen long nhãn",
            Instructions = "[]",
            Status = "APPROVED_ACTIVE",
            DifficultyLevel = "EASY",
            Source = "USER",
            Servings = 2,
            CaloriesKcal = 300m,
            RecipeIngredients = new List<RecipeIngredient>
            {
                new() { IngredientId = 50, QuantityNeeded = 100m, Unit = "g", Ingredient = ingredient }
            }
        };

        var plan = new MealPlan
        {
            PlanId = 888,
            UserId = user.UserId,
            PlanName = "Thực đơn giải nhiệt",
            StartDate = new DateOnly(2026, 11, 1),
            EndDate = new DateOnly(2026, 11, 7),
            PlanStatus = "ACTIVE",
            BudgetTier = "STANDARD",
            MealSlots = new List<MealSlot>
            {
                new() { SlotId = 801, PlanId = 888, DayOfWeek = "MON", MealType = "DINNER", Recipe = recipe }
            }
        };

        _db.Users.Add(user);
        _db.Ingredients.Add(ingredient);
        _db.Recipes.Add(recipe);
        _db.MealPlans.Add(plan);
        await _db.SaveChangesAsync();

        // BƯỚC 2: Người dùng sinh danh sách đi chợ cho 2 người (ServingsMultiplier = 2)
        var createRequest = new CreateMealPlanShoppingListRequest { ServingsMultiplier = 2.0m };
        var shoppingListResult = await _shoppingListService.CreateMealPlanShoppingListAsync(plan.PlanId, user.UserId, createRequest);

        shoppingListResult.Should().NotBeNull();
        shoppingListResult.Status.Should().Be("PENDING");
        shoppingListResult.Aisles.Should().NotBeEmpty();

        // BƯỚC 3: Kiểm tra dữ liệu được lưu trong cơ sở dữ liệu
        var savedList = await _db.ShoppingLists
            .Include(s => s.ShoppingListItems)
            .FirstOrDefaultAsync(s => s.PlanId == plan.PlanId);

        savedList.Should().NotBeNull();
        savedList!.Status.Should().Be("PENDING");
        savedList.ShoppingListItems.Should().HaveCount(1);
        var firstItem = savedList.ShoppingListItems.First();
        firstItem.IngredientId.Should().Be(50);
        firstItem.IsPurchased.Should().BeFalse();

        // BƯỚC 4: Người dùng đánh dấu món đã mua qua PatchShoppingListItemAsync
        var patchRequest = new PatchShoppingListItemRequest { IsPurchased = true };
        var patchResult = await _shoppingListService.PatchShoppingListItemAsync(savedList.ListId, firstItem.ItemId, user.UserId, patchRequest);

        patchResult.IsPurchased.Should().BeTrue();
    }
}

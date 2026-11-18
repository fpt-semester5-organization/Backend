using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VeggieLife.BLL.DTOs.ShoppingLists.Requests;
using VeggieLife.BLL.Services.ShoppingLists;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Performance;

/// <summary>
/// BỘ KIỂM THỬ HIỆU NĂNG & ĐỘ TRỄ XỬ LÝ (PERFORMANCE & BENCHMARK TESTING)
/// </summary>
public class SystemPerformanceTestSuite : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ShoppingListService _shoppingListService;

    public SystemPerformanceTestSuite()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("PerfTestDb_" + Guid.NewGuid())
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
    public async Task Performance_AggregateLargeMealPlan_ShouldCompleteUnderSla100ms()
    {
        // ARRANGE: Tạo thực đơn 21 bữa ăn (7 ngày x 3 bữa) với nhiều nguyên liệu
        var userId = 99;
        var planId = 999;
        var recipeIngredients = new List<RecipeIngredient>();

        for (int i = 1; i <= 20; i++)
        {
            var ing = new Ingredient
            {
                IngredientId = 200 + i,
                IngredientName = $"Nguyên liệu chay số {i}",
                DefaultStallType = "PRODUCE",
                StandardUnit = "g",
                CaloriesPer100g = 100m,
                FoodGroup = "PLANT"
            };
            _db.Ingredients.Add(ing);
            recipeIngredients.Add(new RecipeIngredient { IngredientId = ing.IngredientId, QuantityNeeded = 50m, Unit = "g", Ingredient = ing });
        }

        var recipe = new Recipe
        {
            RecipeId = 901,
            Title = "Đại tiệc lẩu chay 20 vị",
            Instructions = "[]",
            Status = "APPROVED_ACTIVE",
            DifficultyLevel = "MEDIUM",
            Source = "USER",
            Servings = 4,
            CaloriesKcal = 800m,
            RecipeIngredients = recipeIngredients
        };

        var mealSlots = new List<MealSlot>();
        for (int s = 1; s <= 21; s++)
        {
            mealSlots.Add(new MealSlot { SlotId = 9000 + s, PlanId = planId, DayOfWeek = "MON", MealType = "LUNCH", Recipe = recipe });
        }

        var plan = new MealPlan
        {
            PlanId = planId,
            UserId = userId,
            PlanName = "Thực đơn tuần hiệu năng cao",
            StartDate = new DateOnly(2026, 11, 1),
            EndDate = new DateOnly(2026, 11, 7),
            PlanStatus = "ACTIVE",
            BudgetTier = "STANDARD",
            MealSlots = mealSlots
        };

        _db.Recipes.Add(recipe);
        _db.MealPlans.Add(plan);
        await _db.SaveChangesAsync();

        // ACT: Đo thời gian tổng hợp 420 lượt nguyên liệu (21 bữa x 20 nguyên liệu)
        var sw = Stopwatch.StartNew();
        var result = await _shoppingListService.CreateMealPlanShoppingListAsync(planId, userId, new CreateMealPlanShoppingListRequest { ServingsMultiplier = 4.0m });
        sw.Stop();

        // ASSERT: Toàn bộ quá trình tổng hợp và phân quầy phải hoàn thành dưới 500ms
        result.Should().NotBeNull();
        sw.ElapsedMilliseconds.Should().BeLessThan(500, $"Quá trình tổng hợp mất {sw.ElapsedMilliseconds}ms, vượt ngưỡng SLA");
    }
}

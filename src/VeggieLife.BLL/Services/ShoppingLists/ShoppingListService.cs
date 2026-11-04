using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.ShoppingLists.Requests;
using VeggieLife.BLL.DTOs.ShoppingLists.Responses;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.ShoppingLists;

/// <summary>
/// Dịch vụ tổng hợp và quản lý danh sách đi chợ từ thực đơn tuần (FN-MEAL-05, FN-MEAL-06).
/// Tuân thủ quy tắc BR-USR-13, BR-USR-07, D42, D74.
/// </summary>
public class ShoppingListService : IShoppingListService
{
    private readonly AppDbContext _db;

    private static readonly string[] StandardAisles = { "PRODUCE", "CONDIMENTS", "DRY_GOODS", "PLANT_PROTEIN" };

    public ShoppingListService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CreateMealPlanShoppingListResponseData> CreateMealPlanShoppingListAsync(
        int planId,
        int userId,
        CreateMealPlanShoppingListRequest request,
        CancellationToken ct = default)
    {
        if (planId <= 0)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Mã thực đơn phải lớn hơn 0.");
        }

        request ??= new CreateMealPlanShoppingListRequest();

        if (request.ServingsMultiplier < 1.0m || request.ServingsMultiplier > 10.0m)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Hệ số nhân khẩu phần phải từ 1.0 đến 10.0.");
        }

        var plan = await _db.MealPlans
            .Include(p => p.MealSlots)
                .ThenInclude(s => s.Recipe)
                    .ThenInclude(r => r.RecipeIngredients)
                        .ThenInclude(ri => ri.Ingredient)
            .Include(p => p.MealSlots)
                .ThenInclude(s => s.Recipe)
                    .ThenInclude(r => r.Food)
            .FirstOrDefaultAsync(p => p.PlanId == planId && p.UserId == userId, ct);

        if (plan == null)
        {
            throw AppException.NotFound(
                ErrorCodes.ErrMealPlanNotFound,
                "Thực đơn không tồn tại hoặc không thuộc về bạn.");
        }

        if (!string.Equals(plan.PlanStatus, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            throw AppException.Conflict(
                ErrorCodes.ErrMealPlanNotActive,
                "Cần kích hoạt thực đơn trước.");
        }

        var exists = await _db.ShoppingLists.AnyAsync(s => s.PlanId == planId, ct);
        if (exists)
        {
            throw AppException.Conflict(
                ErrorCodes.ErrShoppingListExists,
                "Thực đơn này đã có danh sách đi chợ.");
        }

        // Tổng hợp nguyên liệu theo số ngày được chọn (hoặc toàn bộ thực đơn)
        var aggregatedDict = new Dictionary<int, AggregatedIngredientCollector>();

        var sortedSlots = plan.MealSlots.OrderBy(s => s.SlotId).ToList();
        var effectiveSlots = (request.DaysCount.HasValue && request.DaysCount.Value > 0)
            ? sortedSlots.Take(request.DaysCount.Value * 3).ToList()
            : sortedSlots;

        for (int i = 0; i < effectiveSlots.Count; i++)
        {
            var slot = effectiveSlots[i];
            if (slot.Recipe == null)
            {
                continue;
            }

            int dayIndex = i / 3;
            var dayKey = dayIndex.ToString();
            var recipeServings = slot.Recipe.Servings > 0 ? (decimal)slot.Recipe.Servings : 2m;

            foreach (var ri in slot.Recipe.RecipeIngredients)
            {
                if (ri.Ingredient == null)
                {
                    continue;
                }

                // Định lượng tổng hợp (cho số người = request.ServingsMultiplier, công thức chuẩn DB dành cho recipeServings người)
                var perPersonQty = (ri.QuantityNeeded / recipeServings) * request.ServingsMultiplier;
                var (normQty, normUnit) = NormalizeUnit(perPersonQty, ri.Unit, ri.Ingredient.StandardUnit);

                if (!aggregatedDict.TryGetValue(ri.IngredientId, out var collector))
                {
                    collector = new AggregatedIngredientCollector
                    {
                        IngredientId = ri.IngredientId,
                        IngredientName = ri.Ingredient.IngredientName,
                        Aisle = NormalizeAisle(ri.Ingredient.DefaultStallType),
                        Unit = normUnit
                    };
                    aggregatedDict[ri.IngredientId] = collector;
                }

                collector.TotalQuantity += normQty;
                collector.RecipeSources.Add(slot.Recipe.Title);
                if (slot.Recipe.Food != null && !string.IsNullOrWhiteSpace(slot.Recipe.Food.FoodName))
                {
                    collector.RecipeSources.Add(slot.Recipe.Food.FoodName);
                }

                // Định lượng cơ sở cho 1 người trong ngày
                var baseSingleQty = ri.QuantityNeeded / recipeServings;
                var (singleNormQty, _) = NormalizeUnit(baseSingleQty, ri.Unit, ri.Ingredient.StandardUnit);
                collector.DayQuantities.TryGetValue(dayKey, out var curDayQty);
                collector.DayQuantities[dayKey] = Math.Round(curDayQty + singleNormQty, 2);
            }
        }

        var effectiveDays = request.DaysCount.HasValue && request.DaysCount.Value > 0
            ? Math.Min(request.DaysCount.Value, Math.Max(1, (int)Math.Ceiling(sortedSlots.Count / 3.0)))
            : Math.Max(1, (int)Math.Ceiling(sortedSlots.Count / 3.0));

        var shoppingList = new ShoppingList
        {
            PlanId = plan.PlanId,
            ListTitle = $"Danh sách đi chợ ({effectiveDays} ngày) - {plan.PlanName}",
            TargetShoppingDate = request.TargetShoppingDate ?? plan.StartDate,
            Status = "PENDING",
            IsOfflineSynced = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.ShoppingLists.Add(shoppingList);
        await _db.SaveChangesAsync(ct);

        var createdItems = new List<ShoppingListItem>();
        foreach (var entry in aggregatedDict.Values.OrderBy(x => x.IngredientName))
        {
            var item = new ShoppingListItem
            {
                ListId = shoppingList.ListId,
                IngredientId = entry.IngredientId,
                AggregatedQuantity = Math.Round(entry.TotalQuantity, 2),
                Unit = entry.Unit,
                IsPurchased = false
            };
            _db.ShoppingListItems.Add(item);
            createdItems.Add(item);
        }

        await _db.SaveChangesAsync(ct);

        // Xây dựng response gom theo 4 quầy
        var aislesList = BuildAislesList(createdItems, aggregatedDict);

        return new CreateMealPlanShoppingListResponseData
        {
            ListId = shoppingList.ListId,
            PlanId = shoppingList.PlanId,
            Status = shoppingList.Status,
            Aisles = aislesList
        };
    }

    public async Task<GetShoppingListResponseData> GetShoppingListByIdAsync(
        int listId,
        int userId,
        CancellationToken ct = default)
    {
        if (listId <= 0)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Mã danh sách đi chợ phải lớn hơn 0.");
        }

        var list = await _db.ShoppingLists
            .Include(s => s.Plan)
                .ThenInclude(p => p.MealSlots)
                    .ThenInclude(slot => slot.Recipe)
                        .ThenInclude(r => r.RecipeIngredients)
            .Include(s => s.Plan)
                .ThenInclude(p => p.MealSlots)
                    .ThenInclude(slot => slot.Recipe)
                        .ThenInclude(r => r.Food)
            .Include(s => s.ShoppingListItems)
                .ThenInclude(item => item.Ingredient)
            .FirstOrDefaultAsync(s => s.ListId == listId, ct);

        if (list == null || list.Plan == null || list.Plan.UserId != userId)
        {
            throw AppException.NotFound(
                ErrorCodes.ErrMealPlanNotFound,
                "Thực đơn không tồn tại hoặc không thuộc về bạn.");
        }

        var purchasedCount = list.ShoppingListItems.Count(i => i.IsPurchased);
        var totalCount = list.ShoppingListItems.Count;

        // Tập hợp nguồn công thức và định lượng chính xác theo từng ngày (chia cho Servings của công thức gốc)
        var sortedSlots = list.Plan.MealSlots.OrderBy(s => s.SlotId).ToList();
        var dayQuantitiesMap = new Dictionary<int, Dictionary<string, decimal>>();
        var recipeSourceMap = new Dictionary<int, HashSet<string>>();

        for (int i = 0; i < sortedSlots.Count; i++)
        {
            var slot = sortedSlots[i];
            if (slot.Recipe == null)
            {
                continue;
            }

            int dayIndex = i / 3;
            var dayKey = dayIndex.ToString();
            var recipeServings = slot.Recipe.Servings > 0 ? (decimal)slot.Recipe.Servings : 2m;

            foreach (var ri in slot.Recipe.RecipeIngredients)
            {
                if (ri.Ingredient == null)
                {
                    continue;
                }

                if (!recipeSourceMap.TryGetValue(ri.IngredientId, out var sources))
                {
                    sources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    recipeSourceMap[ri.IngredientId] = sources;
                }
                sources.Add(slot.Recipe.Title);
                if (slot.Recipe.Food != null && !string.IsNullOrWhiteSpace(slot.Recipe.Food.FoodName))
                {
                    sources.Add(slot.Recipe.Food.FoodName);
                }

                // Công thức chuẩn cho 2 người (Servings = 2), định lượng cho 1 người = QuantityNeeded / Servings
                var perPersonQty = ri.QuantityNeeded / recipeServings;
                var (normQty, _) = NormalizeUnit(perPersonQty, ri.Unit, ri.Ingredient.StandardUnit);

                if (!dayQuantitiesMap.TryGetValue(ri.IngredientId, out var dayDict))
                {
                    dayDict = new Dictionary<string, decimal>();
                    dayQuantitiesMap[ri.IngredientId] = dayDict;
                }

                dayDict.TryGetValue(dayKey, out var curDayQty);
                dayDict[dayKey] = Math.Round(curDayQty + normQty, 2);
            }
        }

        var aislesDict = new Dictionary<string, List<ShoppingListItemDto>>(StringComparer.OrdinalIgnoreCase);
        foreach (var aisle in StandardAisles)
        {
            aislesDict[aisle] = new List<ShoppingListItemDto>();
        }

        foreach (var item in list.ShoppingListItems.OrderBy(i => i.Ingredient?.IngredientName ?? string.Empty))
        {
            var aisle = NormalizeAisle(item.Ingredient?.DefaultStallType);
            recipeSourceMap.TryGetValue(item.IngredientId, out var sources);
            dayQuantitiesMap.TryGetValue(item.IngredientId, out var dayDict);

            // Tự động chuẩn hóa tổng số lượng theo định lượng chuẩn 1 người từ các bữa ăn
            decimal totalQuantity = item.AggregatedQuantity;
            if (dayDict != null && dayDict.Count > 0)
            {
                totalQuantity = dayDict.Values.Sum();
            }

            var itemDto = new ShoppingListItemDto
            {
                ItemId = item.ItemId,
                IngredientId = item.IngredientId,
                Name = item.Ingredient?.IngredientName ?? "Nguyên liệu",
                Quantity = totalQuantity,
                Unit = item.Unit,
                IsPurchased = item.IsPurchased,
                RecipeSources = sources != null ? sources.OrderBy(s => s).ToList() : new List<string>(),
                DayQuantities = dayDict ?? new Dictionary<string, decimal>()
            };

            if (!aislesDict.TryGetValue(aisle, out var listInAisle))
            {
                listInAisle = new List<ShoppingListItemDto>();
                aislesDict[aisle] = listInAisle;
            }
            listInAisle.Add(itemDto);
        }

        var aislesResult = new List<ShoppingListAisleDto>();
        foreach (var aisle in StandardAisles)
        {
            if (aislesDict.TryGetValue(aisle, out var items) && items.Count > 0)
            {
                aislesResult.Add(new ShoppingListAisleDto
                {
                    Aisle = aisle,
                    Items = items
                });
            }
        }

        return new GetShoppingListResponseData
        {
            ListId = list.ListId,
            PlanId = list.PlanId,
            Status = list.Status,
            Progress = new ShoppingListProgressDto
            {
                Purchased = purchasedCount,
                Total = totalCount
            },
            Aisles = aislesResult
        };
    }

    public async Task<PatchShoppingListItemResponseData> PatchShoppingListItemAsync(
        int listId,
        int itemId,
        int userId,
        PatchShoppingListItemRequest request,
        CancellationToken ct = default)
    {
        if (listId <= 0 || itemId <= 0)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Mã danh sách và mã món hàng phải lớn hơn 0.");
        }

        if (request == null)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Dữ liệu đầu vào không hợp lệ.");
        }

        var list = await _db.ShoppingLists
            .Include(s => s.Plan)
            .Include(s => s.ShoppingListItems)
            .FirstOrDefaultAsync(s => s.ListId == listId, ct);

        if (list == null || list.Plan == null || list.Plan.UserId != userId)
        {
            throw AppException.NotFound(
                ErrorCodes.ErrMealPlanNotFound,
                "Thực đơn không tồn tại hoặc không thuộc về bạn.");
        }

        var item = list.ShoppingListItems.FirstOrDefault(i => i.ItemId == itemId);
        if (item == null)
        {
            throw AppException.NotFound(
                ErrorCodes.ErrMealPlanNotFound,
                "Món hàng không tồn tại trong danh sách đi chợ.");
        }

        item.IsPurchased = request.IsPurchased;

        // Nếu tất cả item đã mua thì ShoppingLists.Status = COMPLETED; nếu bỏ tích thì quay về PENDING
        if (list.ShoppingListItems.All(i => i.IsPurchased))
        {
            list.Status = "COMPLETED";
        }
        else if (string.Equals(list.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase) && !request.IsPurchased)
        {
            list.Status = "PENDING";
        }

        await _db.SaveChangesAsync(ct);

        return new PatchShoppingListItemResponseData
        {
            ItemId = item.ItemId,
            IsPurchased = item.IsPurchased,
            ListStatus = list.Status
        };
    }

    public async Task<PatchShoppingListResponseData> PatchShoppingListAsync(
        int listId,
        int userId,
        PatchShoppingListRequest request,
        CancellationToken ct = default)
    {
        if (listId <= 0)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Mã danh sách đi chợ phải lớn hơn 0.");
        }

        if (request == null || string.IsNullOrWhiteSpace(request.Status))
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Dữ liệu đầu vào không hợp lệ.");
        }

        var normalizedStatus = request.Status.Trim().ToUpperInvariant();
        if (normalizedStatus != "COMPLETED" && normalizedStatus != "CANCELLED")
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Trạng thái chỉ có thể là COMPLETED hoặc CANCELLED.");
        }

        var list = await _db.ShoppingLists
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.ListId == listId, ct);

        if (list == null || list.Plan == null || list.Plan.UserId != userId)
        {
            throw AppException.NotFound(
                ErrorCodes.ErrMealPlanNotFound,
                "Thực đơn không tồn tại hoặc không thuộc về bạn.");
        }

        list.Status = normalizedStatus;
        await _db.SaveChangesAsync(ct);

        return new PatchShoppingListResponseData
        {
            ListId = list.ListId,
            Status = list.Status
        };
    }

    private static (decimal Quantity, string Unit) NormalizeUnit(decimal quantity, string? unit, string? fallbackUnit)
    {
        var rawUnit = (unit ?? fallbackUnit ?? "g").Trim().ToLowerInvariant();

        if (rawUnit == "kg")
        {
            return (quantity * 1000m, "g");
        }

        if (rawUnit == "l" || rawUnit == "lit" || rawUnit == "lít")
        {
            return (quantity * 1000m, "ml");
        }

        return (quantity, rawUnit);
    }

    private static string NormalizeAisle(string? stallType)
    {
        if (string.IsNullOrWhiteSpace(stallType))
        {
            return "PRODUCE";
        }

        var upper = stallType.Trim().ToUpperInvariant();
        return StandardAisles.Contains(upper) ? upper : "PRODUCE";
    }

    private static IReadOnlyList<ShoppingListAisleDto> BuildAislesList(
        List<ShoppingListItem> createdItems,
        Dictionary<int, AggregatedIngredientCollector> aggregatedDict)
    {
        var aislesDict = new Dictionary<string, List<ShoppingListItemDto>>(StringComparer.OrdinalIgnoreCase);
        foreach (var aisle in StandardAisles)
        {
            aislesDict[aisle] = new List<ShoppingListItemDto>();
        }

        foreach (var item in createdItems)
        {
            aggregatedDict.TryGetValue(item.IngredientId, out var collector);
            var aisle = collector?.Aisle ?? "PRODUCE";

            var itemDto = new ShoppingListItemDto
            {
                ItemId = item.ItemId,
                IngredientId = item.IngredientId,
                Name = collector?.IngredientName ?? "Nguyên liệu",
                Quantity = item.AggregatedQuantity,
                Unit = item.Unit,
                IsPurchased = item.IsPurchased,
                RecipeSources = collector?.RecipeSources.OrderBy(s => s).ToList() ?? new List<string>(),
                DayQuantities = collector?.DayQuantities ?? new Dictionary<string, decimal>()
            };

            if (!aislesDict.TryGetValue(aisle, out var listInAisle))
            {
                listInAisle = new List<ShoppingListItemDto>();
                aislesDict[aisle] = listInAisle;
            }
            listInAisle.Add(itemDto);
        }

        var result = new List<ShoppingListAisleDto>();
        foreach (var aisle in StandardAisles)
        {
            if (aislesDict.TryGetValue(aisle, out var items) && items.Count > 0)
            {
                result.Add(new ShoppingListAisleDto
                {
                    Aisle = aisle,
                    Items = items
                });
            }
        }

        return result;
    }

    private class AggregatedIngredientCollector
    {
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = null!;
        public string Aisle { get; set; } = "PRODUCE";
        public decimal TotalQuantity { get; set; }
        public string Unit { get; set; } = "g";
        public HashSet<string> RecipeSources { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, decimal> DayQuantities { get; } = new();
    }
}

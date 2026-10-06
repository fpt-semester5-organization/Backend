using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class Recipe
{
    public int RecipeId { get; set; }

    public int? UserId { get; set; }

    public int CategoryId { get; set; }

    public int DietarySchoolId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string Instructions { get; set; } = null!;

    public int PrepTimeMinutes { get; set; }

    public int CookTimeMinutes { get; set; }

    public int Servings { get; set; }

    public decimal CaloriesKcal { get; set; }

    public string DifficultyLevel { get; set; } = null!;

    public bool IsVerified { get; set; }

    public string? ImageUrl { get; set; }

    public string Status { get; set; } = null!;

    public int ViewCount { get; set; }

    public string Source { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual FoodCategory Category { get; set; } = null!;

    public virtual ICollection<MealSlot> MealSlots { get; set; } = new List<MealSlot>();

    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

    public virtual ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();

    public virtual ICollection<Video> Videos { get; set; } = new List<Video>();

    public virtual ICollection<ViolationReport> ViolationReports { get; set; } = new List<ViolationReport>();
}

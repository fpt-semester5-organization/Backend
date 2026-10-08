using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class DietarySchool
{
    public int DietarySchoolId { get; set; }

    public string SchoolName { get; set; } = null!;

    public string SchoolCode { get; set; } = null!;

    public string ForbiddenFoodRules { get; set; } = null!;

    public DateTime UpdatedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<HealthProfile> HealthProfiles { get; set; } = new List<HealthProfile>();

    public virtual ICollection<MealPlan> MealPlans { get; set; } = new List<MealPlan>();

    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
}

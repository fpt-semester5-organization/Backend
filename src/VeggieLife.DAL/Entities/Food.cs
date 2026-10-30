using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

/// <summary>
/// Đại diện cho khái niệm món ăn (VD: "Bún Huế", "Phở chay") mà nhiều công thức cụ thể có thể cùng thuộc về.
/// </summary>
public partial class Food
{
    /// <summary>Khóa chính định danh món ăn.</summary>
    public int FoodId { get; set; }

    /// <summary>Khóa ngoại tham chiếu tới danh mục món ăn (FoodCategories).</summary>
    public int CategoryId { get; set; }

    /// <summary>Tên món ăn (duy nhất trong toàn hệ thống).</summary>
    public string FoodName { get; set; } = null!;

    /// <summary>Thời điểm tạo bản ghi.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Thời điểm cập nhật bản ghi lần cuối.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Danh mục mà món ăn này thuộc về.</summary>
    public virtual FoodCategory Category { get; set; } = null!;

    /// <summary>Danh sách công thức nấu ăn cụ thể gắn với món ăn này.</summary>
    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
}


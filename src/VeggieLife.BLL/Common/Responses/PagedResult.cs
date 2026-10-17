using System;
using System.Collections.Generic;

namespace VeggieLife.BLL.Common.Responses;

/// <summary>
/// Lớp bọc kết quả phân trang chuẩn của hệ thống VeggieLife.
/// </summary>
/// <typeparam name="T">Kiểu phần tử danh sách.</typeparam>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PagedResult()
    {
    }

    public PagedResult(IReadOnlyList<T> items, int totalItems, int pageNumber, int pageSize)
    {
        Items = items ?? Array.Empty<T>();
        TotalItems = totalItems;
        PageNumber = pageNumber < 1 ? 1 : pageNumber;
        PageSize = pageSize < 1 ? 10 : pageSize;
        TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
    }

    public static PagedResult<T> Create(IReadOnlyList<T> items, int totalItems, int pageNumber, int pageSize)
    {
        return new PagedResult<T>(items, totalItems, pageNumber, pageSize);
    }
}

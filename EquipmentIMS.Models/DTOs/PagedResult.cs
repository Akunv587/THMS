namespace EquipmentIMS.Models.DTOs;

/// <summary>
/// 分页结果
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// 创建分页结果
    /// </summary>
    public static PagedResult<T> Create(List<T> items, int totalCount, int page, int pageSize) => new()
    {
        Items = items,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };
}

/// <summary>
/// 分页结果扩展
/// </summary>
public static class PagedResultExtensions
{
    public static PagedResult<TTarget> MapTo<TSource, TTarget>(
        this PagedResult<TSource> source, Func<TSource, TTarget> selector) => new()
    {
        Items = source.Items.Select(selector).ToList(),
        TotalCount = source.TotalCount,
        Page = source.Page,
        PageSize = source.PageSize
    };
}

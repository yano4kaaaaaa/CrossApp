namespace Api.Contracts;

// Відповідь списку з пагінацією: { items, total, page, pageSize }.
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
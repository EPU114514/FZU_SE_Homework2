using Homework2.Domain.Enums;

namespace Homework2.Application.Models;

public sealed record ContactMethodDto(
    Guid Id,
    ContactMethodType Type,
    string Label,
    string Value,
    bool IsPrimary);

public sealed record ContactDto(
    Guid Id,
    string Name,
    string? Company,
    string? Notes,
    bool IsFavorite,
    IReadOnlyList<ContactMethodDto> Methods,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
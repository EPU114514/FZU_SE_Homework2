namespace Homework2.Application.Models;

public sealed class ContactQuery
{
    public string? Search { get; set; }

    public bool? Favorite { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
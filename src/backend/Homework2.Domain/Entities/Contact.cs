namespace Homework2.Domain.Entities;

public sealed class Contact
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string? Company { get; set; }

    public string? Notes { get; set; }

    public bool IsFavorite { get; set; }

    public List<ContactMethod> Methods { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
using Homework2.Domain.Enums;

namespace Homework2.Domain.Entities;

public sealed class ContactMethod
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public ContactMethodType Type { get; set; }

    public string Label { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }
}
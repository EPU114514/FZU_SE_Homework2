using System.ComponentModel.DataAnnotations;
using Homework2.Domain.Enums;

namespace Homework2.Application.Models;

public sealed class ContactMethodInput
{
    [Required] public ContactMethodType Type { get; set; }

    [MaxLength(50)] public string? Label { get; set; }

    [Required] [MaxLength(500)] public string Value { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }
}

public sealed class CreateContactRequest
{
    [Required] [MaxLength(100)] public string Name { get; set; } = string.Empty;

    [MaxLength(200)] public string? Company { get; set; }

    [MaxLength(2000)] public string? Notes { get; set; }

    public bool IsFavorite { get; set; }

    public List<ContactMethodInput> Methods { get; set; } = [];
}

public sealed class UpdateContactRequest
{
    [Required] [MaxLength(100)] public string Name { get; set; } = string.Empty;

    [MaxLength(200)] public string? Company { get; set; }

    [MaxLength(2000)] public string? Notes { get; set; }

    public bool IsFavorite { get; set; }

    public List<ContactMethodInput> Methods { get; set; } = [];
}

public sealed class SetFavoriteRequest
{
    public bool IsFavorite { get; set; }
}
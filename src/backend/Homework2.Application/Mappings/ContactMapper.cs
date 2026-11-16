using Homework2.Application.Models;
using Homework2.Domain.Entities;

namespace Homework2.Application.Mappings;

public static class ContactMapper
{
    public static ContactDto ToDto(Contact contact) =>
        new(
            contact.Id,
            contact.Name,
            contact.Company,
            contact.Notes,
            contact.IsFavorite,
            contact.Methods
                .Select(method =>
                    new ContactMethodDto(method.Id, method.Type, method.Label, method.Value, method.IsPrimary))
                .ToList(),
            contact.CreatedAt,
            contact.UpdatedAt);

    public static Contact Clone(Contact contact) =>
        new()
        {
            Id = contact.Id,
            Name = contact.Name,
            Company = contact.Company,
            Notes = contact.Notes,
            IsFavorite = contact.IsFavorite,
            Methods = contact.Methods
                .Select(method => new ContactMethod
                {
                    Id = method.Id,
                    Type = method.Type,
                    Label = method.Label,
                    Value = method.Value,
                    IsPrimary = method.IsPrimary
                })
                .ToList(),
            CreatedAt = contact.CreatedAt,
            UpdatedAt = contact.UpdatedAt
        };
}
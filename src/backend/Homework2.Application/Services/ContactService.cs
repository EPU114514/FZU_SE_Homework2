using Homework2.Application.Abstractions;
using Homework2.Application.Exceptions;
using Homework2.Application.Mappings;
using Homework2.Application.Models;
using Homework2.Domain.Entities;
using Homework2.Domain.Enums;

namespace Homework2.Application.Services;

public sealed class ContactService : IContactService
{
    private const int MaxMethodsPerContact = 50;
    private readonly IContactRepository _repository;
    private readonly IExcelService _excel;

    public ContactService(IContactRepository repository, IExcelService excel)
    {
        _repository = repository;
        _excel = excel;
    }

    public async Task<PagedResult<ContactDto>> GetPagedAsync(ContactQuery query, CancellationToken ct = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;
        var (items, total) = await _repository.GetPagedAsync(query.Search?.Trim(), query.Favorite, page, pageSize, ct);

        return new PagedResult<ContactDto>(
            items.Select(ContactMapper.ToDto).ToList(),
            page,
            pageSize,
            total);
    }

    public async Task<ContactDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var contact = await _repository.GetByIdAsync(id, ct)
                      ?? throw new NotFoundException($"Contact '{id}' was not found.");

        return ContactMapper.ToDto(contact);
    }

    public async Task<ContactDto> CreateAsync(CreateContactRequest request, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var contact = new Contact
        {
            Id = Guid.NewGuid(),
            Name = NormalizeName(request.Name),
            Company = NormalizeOptional(request.Company, 200, "Company"),
            Notes = NormalizeOptional(request.Notes, 2000, "Notes"),
            IsFavorite = request.IsFavorite,
            Methods = BuildMethods(request.Methods),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _repository.AddAsync(contact, ct);
        return ContactMapper.ToDto(contact);
    }

    public async Task<ContactDto> UpdateAsync(Guid id, UpdateContactRequest request, CancellationToken ct = default)
    {
        var existing = await _repository.GetByIdAsync(id, ct)
                       ?? throw new NotFoundException($"Contact '{id}' was not found.");

        var updated = new Contact
        {
            Id = existing.Id,
            Name = NormalizeName(request.Name),
            Company = NormalizeOptional(request.Company, 200, "Company"),
            Notes = NormalizeOptional(request.Notes, 2000, "Notes"),
            IsFavorite = request.IsFavorite,
            Methods = BuildMethods(request.Methods),
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        if (!await _repository.UpdateAsync(updated, ct))
        {
            throw new NotFoundException($"Contact '{id}' was not found.");
        }

        return ContactMapper.ToDto(updated);
    }

    public async Task<ContactDto> SetFavoriteAsync(Guid id, bool isFavorite, CancellationToken ct = default)
    {
        var existing = await _repository.GetByIdAsync(id, ct)
                       ?? throw new NotFoundException($"Contact '{id}' was not found.");

        var updated = ContactMapper.Clone(existing);
        updated.IsFavorite = isFavorite;
        updated.UpdatedAt = DateTimeOffset.UtcNow;

        if (!await _repository.UpdateAsync(updated, ct))
        {
            throw new NotFoundException($"Contact '{id}' was not found.");
        }

        return ContactMapper.ToDto(updated);
    }

    public async Task<ContactDto> AddMethodAsync(Guid contactId, ContactMethodInput input,
        CancellationToken ct = default)
    {
        var existing = await _repository.GetByIdAsync(contactId, ct)
                       ?? throw new NotFoundException($"Contact '{contactId}' was not found.");

        var updated = ContactMapper.Clone(existing);
        if (updated.Methods.Count >= MaxMethodsPerContact)
        {
            throw new AppValidationException(
                $"A contact cannot have more than {MaxMethodsPerContact} contact methods.");
        }

        var method = BuildMethod(input);
        if (method.IsPrimary)
        {
            foreach (var other in updated.Methods.Where(m => m.Type == method.Type))
            {
                other.IsPrimary = false;
            }
        }

        updated.Methods.Add(method);
        updated.UpdatedAt = DateTimeOffset.UtcNow;

        if (!await _repository.UpdateAsync(updated, ct))
        {
            throw new NotFoundException($"Contact '{contactId}' was not found.");
        }

        return ContactMapper.ToDto(updated);
    }

    public async Task<ContactDto> UpdateMethodAsync(
        Guid contactId,
        Guid methodId,
        ContactMethodInput input,
        CancellationToken ct = default)
    {
        var existing = await _repository.GetByIdAsync(contactId, ct)
                       ?? throw new NotFoundException($"Contact '{contactId}' was not found.");

        var updated = ContactMapper.Clone(existing);
        var index = updated.Methods.FindIndex(m => m.Id == methodId);
        if (index < 0)
        {
            throw new NotFoundException($"Contact method '{methodId}' was not found.");
        }

        var method = BuildMethod(input);
        method.Id = methodId;

        if (method.IsPrimary)
        {
            foreach (var other in updated.Methods.Where(m => m.Type == method.Type && m.Id != methodId))
            {
                other.IsPrimary = false;
            }
        }

        updated.Methods[index] = method;
        updated.UpdatedAt = DateTimeOffset.UtcNow;

        if (!await _repository.UpdateAsync(updated, ct))
        {
            throw new NotFoundException($"Contact '{contactId}' was not found.");
        }

        return ContactMapper.ToDto(updated);
    }

    public async Task RemoveMethodAsync(Guid contactId, Guid methodId, CancellationToken ct = default)
    {
        var existing = await _repository.GetByIdAsync(contactId, ct)
                       ?? throw new NotFoundException($"Contact '{contactId}' was not found.");

        var updated = ContactMapper.Clone(existing);
        if (updated.Methods.RemoveAll(m => m.Id == methodId) == 0)
        {
            throw new NotFoundException($"Contact method '{methodId}' was not found.");
        }

        updated.UpdatedAt = DateTimeOffset.UtcNow;

        if (!await _repository.UpdateAsync(updated, ct))
        {
            throw new NotFoundException($"Contact '{contactId}' was not found.");
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (!await _repository.DeleteAsync(id, ct))
        {
            throw new NotFoundException($"Contact '{id}' was not found.");
        }
    }

    public async Task<byte[]> ExportAsync(ContactQuery query, CancellationToken ct = default)
    {
        var contacts = await _repository.GetAllAsync(query.Search?.Trim(), query.Favorite, ct);
        var ordered = contacts
            .OrderByDescending(contact => contact.IsFavorite)
            .ThenBy(contact => contact.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return _excel.Export(ordered);
    }

    public async Task<ImportResult> ImportAsync(Stream stream, bool skipDuplicates, CancellationToken ct = default)
    {
        var parsed = _excel.Parse(stream);
        var errors = parsed.Errors.ToList();
        var imported = 0;
        var skipped = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var row in parsed.Rows)
        {
            if (row.Methods.Count == 0)
            {
                errors.Add($"Row {row.RowNumber}: '{row.Name}' has no contact methods and was skipped.");
                skipped++;
                continue;
            }

            if (skipDuplicates && await _repository.ExistsByNameAsync(row.Name, ct))
            {
                skipped++;
                continue;
            }

            try
            {
                var contact = new Contact
                {
                    Id = Guid.NewGuid(),
                    Name = NormalizeName(row.Name),
                    Company = NormalizeOptional(row.Company, 200, "Company"),
                    Notes = NormalizeOptional(row.Notes, 2000, "Notes"),
                    IsFavorite = row.IsFavorite,
                    Methods = BuildImportedMethods(row.Methods),
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _repository.AddAsync(contact, ct);
                imported++;
            }
            catch (AppValidationException exception)
            {
                var message = exception.Errors.SelectMany(pair => pair.Value).FirstOrDefault()
                              ?? exception.Message;
                errors.Add($"Row {row.RowNumber}: {message}");
                skipped++;
            }
        }

        return new ImportResult(imported, skipped, errors);
    }

    private static List<ContactMethod> BuildMethods(IEnumerable<ContactMethodInput>? inputs)
    {
        var methods = new List<ContactMethod>();
        foreach (var input in inputs ?? [])
        {
            var method = BuildMethod(input);
            if (method.IsPrimary)
            {
                foreach (var other in methods.Where(m => m.Type == method.Type))
                {
                    other.IsPrimary = false;
                }
            }

            methods.Add(method);
        }

        if (methods.Count > MaxMethodsPerContact)
        {
            throw new AppValidationException(
                $"A contact cannot have more than {MaxMethodsPerContact} contact methods.");
        }

        return methods;
    }

    private static List<ContactMethod> BuildImportedMethods(IEnumerable<ContactMethodInput> inputs)
    {
        var methods = new List<ContactMethod>();
        foreach (var input in inputs)
        {
            var value = input.Value.Trim();
            if (value.Length == 0)
            {
                continue;
            }

            if (value.Length > 500)
            {
                throw new AppValidationException("Contact method value cannot exceed 500 characters.");
            }

            methods.Add(new ContactMethod
            {
                Id = Guid.NewGuid(),
                Type = input.Type,
                Label = input.Label?.Trim() ?? string.Empty,
                Value = value,
                IsPrimary = methods.All(m => m.Type != input.Type)
            });
        }

        return methods;
    }

    private static ContactMethod BuildMethod(ContactMethodInput input)
    {
        if (!Enum.IsDefined(typeof(ContactMethodType), input.Type))
        {
            throw new AppValidationException($"Contact method type '{input.Type}' is not valid.");
        }

        var value = (input.Value ?? string.Empty).Trim();
        if (value.Length == 0)
        {
            throw new AppValidationException("Contact method value cannot be empty.");
        }

        if (value.Length > 500)
        {
            throw new AppValidationException("Contact method value cannot exceed 500 characters.");
        }

        var label = (input.Label ?? string.Empty).Trim();
        if (label.Length > 50)
        {
            throw new AppValidationException("Contact method label cannot exceed 50 characters.");
        }

        return new ContactMethod
        {
            Id = Guid.NewGuid(),
            Type = input.Type,
            Label = label,
            Value = value,
            IsPrimary = input.IsPrimary
        };
    }

    private static string NormalizeName(string? name)
    {
        var value = (name ?? string.Empty).Trim();
        if (value.Length == 0)
        {
            throw new AppValidationException("Name is required.");
        }

        if (value.Length > 100)
        {
            throw new AppValidationException("Name cannot exceed 100 characters.");
        }

        return value;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string fieldName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }

        if (normalized.Length > maxLength)
        {
            throw new AppValidationException($"{fieldName} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}
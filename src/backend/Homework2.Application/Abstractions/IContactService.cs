using Homework2.Application.Models;

namespace Homework2.Application.Abstractions;

public interface IContactService
{
    Task<PagedResult<ContactDto>> GetPagedAsync(ContactQuery query, CancellationToken ct = default);

    Task<ContactDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<ContactDto> CreateAsync(CreateContactRequest request, CancellationToken ct = default);

    Task<ContactDto> UpdateAsync(Guid id, UpdateContactRequest request, CancellationToken ct = default);

    Task<ContactDto> SetFavoriteAsync(Guid id, bool isFavorite, CancellationToken ct = default);

    Task<ContactDto> AddMethodAsync(Guid contactId, ContactMethodInput input, CancellationToken ct = default);

    Task<ContactDto> UpdateMethodAsync(Guid contactId, Guid methodId, ContactMethodInput input,
        CancellationToken ct = default);

    Task RemoveMethodAsync(Guid contactId, Guid methodId, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);

    Task<byte[]> ExportAsync(ContactQuery query, CancellationToken ct = default);

    Task<ImportResult> ImportAsync(Stream stream, bool skipDuplicates, CancellationToken ct = default);
}
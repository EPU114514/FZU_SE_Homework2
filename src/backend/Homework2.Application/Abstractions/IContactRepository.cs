using Homework2.Domain.Entities;

namespace Homework2.Application.Abstractions;

public interface IContactRepository
{
    Task<(IReadOnlyList<Contact> Items, int TotalCount)> GetPagedAsync(
        string? search,
        bool? favorite,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<IReadOnlyList<Contact>> GetAllAsync(
        string? search,
        bool? favorite,
        CancellationToken ct = default);

    Task<Contact?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task AddAsync(Contact contact, CancellationToken ct = default);

    Task<bool> UpdateAsync(Contact contact, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
}
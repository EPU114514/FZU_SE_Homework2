using System.Text.RegularExpressions;
using Homework2.Application.Abstractions;
using Homework2.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Homework2.Infrastructure.Persistence;

public sealed class MongoContactRepository : IContactRepository
{
    private readonly IMongoCollection<Contact> _collection;

    public MongoContactRepository(IMongoDatabase database, string collectionName)
    {
        _collection = database.GetCollection<Contact>(collectionName);
    }

    public async Task<(IReadOnlyList<Contact> Items, int TotalCount)> GetPagedAsync(
        string? search,
        bool? favorite,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var filter = BuildFilter(search, favorite);
        var total = (int)await _collection.CountDocumentsAsync(filter, cancellationToken: ct);
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize < 1 ? 20 : pageSize;

        var items = await _collection
            .Find(filter)
            .SortByDescending(contact => contact.IsFavorite)
            .ThenBy(contact => contact.Name)
            .Skip((safePage - 1) * safePageSize)
            .Limit(safePageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IReadOnlyList<Contact>> GetAllAsync(
        string? search,
        bool? favorite,
        CancellationToken ct = default)
    {
        return await _collection
            .Find(BuildFilter(search, favorite))
            .SortByDescending(contact => contact.IsFavorite)
            .ThenBy(contact => contact.Name)
            .ToListAsync(ct);
    }

    public async Task<Contact?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _collection
            .Find(contact => contact.Id == id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(Contact contact, CancellationToken ct = default)
    {
        await _collection.InsertOneAsync(contact, cancellationToken: ct);
    }

    public async Task<bool> UpdateAsync(Contact contact, CancellationToken ct = default)
    {
        var result = await _collection.ReplaceOneAsync(
            existing => existing.Id == contact.Id,
            contact,
            cancellationToken: ct);

        return result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _collection.DeleteOneAsync(contact => contact.Id == id, ct);
        return result.DeletedCount > 0;
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
    {
        var pattern = new BsonRegularExpression($"^{Regex.Escape(name.Trim())}$", "i");
        return await _collection
            .Find(Builders<Contact>.Filter.Regex(contact => contact.Name, pattern))
            .Limit(1)
            .AnyAsync(ct);
    }

    private static FilterDefinition<Contact> BuildFilter(string? search, bool? favorite)
    {
        var builder = Builders<Contact>.Filter;
        var filters = new List<FilterDefinition<Contact>>();

        if (favorite.HasValue)
        {
            filters.Add(builder.Eq(contact => contact.IsFavorite, favorite.Value));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = new BsonRegularExpression(Regex.Escape(search.Trim()), "i");
            var methodFilter = Builders<ContactMethod>.Filter.Or(
                Builders<ContactMethod>.Filter.Regex(method => method.Value, pattern),
                Builders<ContactMethod>.Filter.Regex(method => method.Label, pattern));

            filters.Add(builder.Or(
                builder.Regex(contact => contact.Name, pattern),
                builder.Regex(contact => contact.Company, pattern),
                builder.Regex(contact => contact.Notes, pattern),
                builder.ElemMatch(contact => contact.Methods, methodFilter)));
        }

        return filters.Count == 0 ? builder.Empty : builder.And(filters);
    }
}
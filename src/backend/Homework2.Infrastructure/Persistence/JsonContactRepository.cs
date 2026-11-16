using System.Text.Json;
using System.Text.Json.Serialization;
using Homework2.Application.Abstractions;
using Homework2.Domain.Entities;

namespace Homework2.Infrastructure.Persistence;

public sealed class JsonContactRepository : IContactRepository
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private List<Contact>? _cache;

    public JsonContactRepository(string filePath)
    {
        _filePath = Path.GetFullPath(filePath);
    }

    public async Task<(IReadOnlyList<Contact> Items, int TotalCount)> GetPagedAsync(
        string? search,
        bool? favorite,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var contacts = await LoadAsync(ct);
            var filtered = ApplyFilter(contacts, search, favorite)
                .OrderByDescending(contact => contact.IsFavorite)
                .ThenBy(contact => contact.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var safePage = page < 1 ? 1 : page;
            var safePageSize = pageSize < 1 ? 20 : pageSize;
            var items = filtered
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .ToList();

            return (items, filtered.Count);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<Contact>> GetAllAsync(
        string? search,
        bool? favorite,
        CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var contacts = await LoadAsync(ct);
            return ApplyFilter(contacts, search, favorite)
                .OrderByDescending(contact => contact.IsFavorite)
                .ThenBy(contact => contact.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Contact?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var contacts = await LoadAsync(ct);
            return contacts.FirstOrDefault(contact => contact.Id == id);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task AddAsync(Contact contact, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var contacts = await LoadAsync(ct);
            contacts.Add(contact);
            await SaveAsync(ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> UpdateAsync(Contact contact, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var contacts = await LoadAsync(ct);
            var index = contacts.FindIndex(existing => existing.Id == contact.Id);
            if (index < 0)
            {
                return false;
            }

            contacts[index] = contact;
            await SaveAsync(ct);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var contacts = await LoadAsync(ct);
            if (contacts.RemoveAll(contact => contact.Id == id) == 0)
            {
                return false;
            }

            await SaveAsync(ct);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var contacts = await LoadAsync(ct);
            return contacts.Any(contact =>
                string.Equals(contact.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<Contact>> LoadAsync(CancellationToken ct)
    {
        if (_cache is not null)
        {
            return _cache;
        }

        if (!File.Exists(_filePath))
        {
            _cache = [];
            return _cache;
        }

        await using var stream = File.OpenRead(_filePath);
        _cache = await JsonSerializer.DeserializeAsync<List<Contact>>(stream, _serializerOptions, ct) ?? [];
        return _cache;
    }

    private async Task SaveAsync(CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = _filePath + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, _cache ?? [], _serializerOptions, ct);
        }

        File.Move(tempPath, _filePath, true);
    }

    private static IEnumerable<Contact> ApplyFilter(
        IEnumerable<Contact> contacts,
        string? search,
        bool? favorite)
    {
        var query = contacts;

        if (favorite.HasValue)
        {
            query = query.Where(contact => contact.IsFavorite == favorite.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(contact =>
                Contains(contact.Name, term)
                || Contains(contact.Company, term)
                || Contains(contact.Notes, term)
                || contact.Methods.Any(method => Contains(method.Value, term) || Contains(method.Label, term)));
        }

        return query;
    }

    private static bool Contains(string? value, string term) =>
        value?.Contains(term, StringComparison.OrdinalIgnoreCase) == true;
}
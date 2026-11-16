using Homework2.Domain.Enums;

namespace Homework2.Application.Models;

public sealed record ImportedContactRow(
    int RowNumber,
    string Name,
    string? Company,
    string? Notes,
    bool IsFavorite,
    IReadOnlyList<ContactMethodInput> Methods);

public sealed record ImportParseResult(
    IReadOnlyList<ImportedContactRow> Rows,
    IReadOnlyList<string> Errors);

public sealed record ImportResult(
    int Imported,
    int Skipped,
    IReadOnlyList<string> Errors);
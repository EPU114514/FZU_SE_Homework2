using Homework2.Application.Models;
using Homework2.Domain.Entities;

namespace Homework2.Application.Abstractions;

public interface IExcelService
{
    byte[] Export(IReadOnlyList<Contact> contacts);

    ImportParseResult Parse(Stream stream);
}
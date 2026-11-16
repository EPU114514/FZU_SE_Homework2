using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Homework2.Application.Abstractions;
using Homework2.Application.Exceptions;
using Homework2.Application.Models;
using Homework2.Domain.Entities;
using Homework2.Domain.Enums;

namespace Homework2.Infrastructure.Excel;

public sealed class ClosedXmlExcelService : IExcelService
{
    private static readonly ContactMethodType[] MethodTypes = Enum.GetValues<ContactMethodType>();

    private static readonly Dictionary<string, ContactMethodType> MethodAliases = new()
    {
        ["phone"] = ContactMethodType.Phone,
        ["phones"] = ContactMethodType.Phone,
        ["tel"] = ContactMethodType.Phone,
        ["telephone"] = ContactMethodType.Phone,
        ["mobile"] = ContactMethodType.Phone,
        ["cell"] = ContactMethodType.Phone,
        ["phonenumber"] = ContactMethodType.Phone,
        ["电话"] = ContactMethodType.Phone,
        ["手机"] = ContactMethodType.Phone,
        ["email"] = ContactMethodType.Email,
        ["emails"] = ContactMethodType.Email,
        ["mail"] = ContactMethodType.Email,
        ["emailaddress"] = ContactMethodType.Email,
        ["邮箱"] = ContactMethodType.Email,
        ["电子邮箱"] = ContactMethodType.Email,
        ["wechat"] = ContactMethodType.WeChat,
        ["weixin"] = ContactMethodType.WeChat,
        ["微信"] = ContactMethodType.WeChat,
        ["qq"] = ContactMethodType.QQ,
        ["address"] = ContactMethodType.Address,
        ["postaladdress"] = ContactMethodType.Address,
        ["地址"] = ContactMethodType.Address,
        ["住址"] = ContactMethodType.Address,
        ["social"] = ContactMethodType.Social,
        ["socialmedia"] = ContactMethodType.Social,
        ["社交"] = ContactMethodType.Social,
        ["other"] = ContactMethodType.Other,
        ["其他"] = ContactMethodType.Other
    };

    private static readonly HashSet<string> NameHeaders = ["name", "fullname", "contactname", "姓名", "联系人"];

    private static readonly HashSet<string> CompanyHeaders =
        ["company", "organization", "organisation", "companyname", "公司", "单位"];

    private static readonly HashSet<string> FavoriteHeaders =
        ["favorite", "favourite", "bookmark", "bookmarked", "starred", "isfavorite", "收藏", "书签"];

    private static readonly HashSet<string> NotesHeaders =
        ["notes", "note", "remark", "remarks", "comment", "comments", "备注"];

    public byte[] Export(IReadOnlyList<Contact> contacts)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Contacts");
        var maxByType = MethodTypes.ToDictionary(
            type => type,
            type => Math.Max(1,
                contacts.Count == 0
                    ? 0
                    : contacts.Max(contact => contact.Methods.Count(method => method.Type == type))));

        var column = 1;
        sheet.Cell(1, column++).Value = "Name";
        sheet.Cell(1, column++).Value = "Company";
        sheet.Cell(1, column++).Value = "Favorite";
        sheet.Cell(1, column++).Value = "Notes";

        foreach (var type in MethodTypes)
        {
            for (var index = 1; index <= maxByType[type]; index++)
            {
                sheet.Cell(1, column++).Value = $"{type} {index}";
            }
        }

        var totalColumns = column - 1;
        var row = 2;
        foreach (var contact in contacts)
        {
            column = 1;
            sheet.Cell(row, column++).Value = contact.Name;
            sheet.Cell(row, column++).Value = contact.Company ?? string.Empty;
            sheet.Cell(row, column++).Value = contact.IsFavorite ? "Yes" : "No";
            sheet.Cell(row, column++).Value = contact.Notes ?? string.Empty;

            foreach (var type in MethodTypes)
            {
                var methods = contact.Methods
                    .Where(method => method.Type == type)
                    .OrderByDescending(method => method.IsPrimary)
                    .ThenBy(method => method.Id)
                    .ToList();

                for (var index = 0; index < maxByType[type]; index++)
                {
                    sheet.Cell(row, column++).Value = index < methods.Count ? methods[index].Value : string.Empty;
                }
            }

            row++;
        }

        var headerRange = sheet.Range(1, 1, 1, totalColumns);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        sheet.SheetView.FreezeRows(1);
        sheet.RangeUsed()?.SetAutoFilter();
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public ImportParseResult Parse(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.FirstOrDefault()
                    ?? throw new AppValidationException("The workbook does not contain a worksheet.");

        var used = sheet.RangeUsed();
        if (used is null)
        {
            return new ImportParseResult([], ["The worksheet is empty."]);
        }

        var headerRow = used.FirstRow().RowNumber();
        var lastRow = used.LastRow().RowNumber();
        var firstColumn = used.FirstColumn().ColumnNumber();
        var lastColumn = used.LastColumn().ColumnNumber();

        int? nameColumn = null;
        int? companyColumn = null;
        int? favoriteColumn = null;
        int? notesColumn = null;
        var methodColumns = new Dictionary<int, ContactMethodType>();

        for (var column = firstColumn; column <= lastColumn; column++)
        {
            var header = NormalizeHeader(GetCellText(sheet, headerRow, column));
            if (header.Length == 0)
            {
                continue;
            }

            if (NameHeaders.Contains(header))
            {
                nameColumn ??= column;
            }
            else if (CompanyHeaders.Contains(header))
            {
                companyColumn ??= column;
            }
            else if (FavoriteHeaders.Contains(header))
            {
                favoriteColumn ??= column;
            }
            else if (NotesHeaders.Contains(header))
            {
                notesColumn ??= column;
            }
            else if (TryParseMethodHeader(header, out var methodType))
            {
                methodColumns[column] = methodType;
            }
        }

        if (nameColumn is null)
        {
            throw new AppValidationException("The Excel file must contain a 'Name' column.");
        }

        var rows = new List<ImportedContactRow>();
        var errors = new List<string>();

        for (var row = headerRow + 1; row <= lastRow; row++)
        {
            var name = GetCellText(sheet, row, nameColumn.Value);
            if (name.Length == 0)
            {
                continue;
            }

            var methods = new List<ContactMethodInput>();
            foreach (var (column, methodType) in methodColumns)
            {
                var value = GetCellText(sheet, row, column);
                if (value.Length == 0)
                {
                    continue;
                }

                methods.Add(new ContactMethodInput
                {
                    Type = methodType,
                    Value = value,
                    IsPrimary = false
                });
            }

            var favoriteText = favoriteColumn is null ? string.Empty : GetCellText(sheet, row, favoriteColumn.Value);
            var company = companyColumn is null ? null : GetCellText(sheet, row, companyColumn.Value);
            var notes = notesColumn is null ? null : GetCellText(sheet, row, notesColumn.Value);

            rows.Add(new ImportedContactRow(
                row,
                name,
                string.IsNullOrWhiteSpace(company) ? null : company,
                string.IsNullOrWhiteSpace(notes) ? null : notes,
                ParseBoolean(favoriteText),
                methods));
        }

        return new ImportParseResult(rows, errors);
    }

    private static string NormalizeHeader(string value) =>
        new(
            value
                .Trim()
                .Where(character => !char.IsWhiteSpace(character) && character is not '_' and not '-')
                .Select(char.ToLowerInvariant)
                .ToArray());

    private static bool TryParseMethodHeader(string header, out ContactMethodType methodType)
    {
        var withoutIndex = Regex.Replace(header, @"\d+$", string.Empty);
        return MethodAliases.TryGetValue(withoutIndex, out methodType);
    }

    private static string GetCellText(IXLWorksheet sheet, int row, int column)
    {
        var cell = sheet.Cell(row, column);
        return cell.IsEmpty() ? string.Empty : cell.GetFormattedString().Trim();
    }

    private static bool ParseBoolean(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return normalized is "true" or "yes" or "y" or "1" or "是" or "收藏";
    }
}
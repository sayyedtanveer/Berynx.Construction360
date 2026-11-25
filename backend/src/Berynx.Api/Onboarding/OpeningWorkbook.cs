using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using Berynx.Domain.Onboarding;

namespace Berynx.Api.Onboarding;
public static class OpeningWorkbook
{
    public static object Preview(OpeningItem[] rows)
    {
        var errors = new List<string>();
        for (var i = 0; i < rows.Length; i++)
        {
            try
            {
                rows[i].Validate();
            }
            catch (ValidationException e)
            {
                errors.Add($"Row {i + 2}: {e.Message}");
            }
        }

        return new
        {
            rows,
            errors,
            difference = rows.Sum(x => x.Debit - x.Credit)
        };
    }

    public static async Task<object> Read(Stream source)
    {
        using var buffer = new MemoryStream();
        byte[] part = new byte[81920];
        int count;
        while ((count = await source.ReadAsync(part)) > 0)
        {
            if (buffer.Length + count > 5_000_000)
                throw new ValidationException("Maximum workbook size is 5 MB.");
            buffer.Write(part, 0, count);
        }

        buffer.Position = 0;
        using var archive = new ZipArchive(buffer, ZipArchiveMode.Read);
        if (archive.Entries.Count > 500 || archive.Entries.Sum(e => e.Length) > 20_000_000)
            throw new ValidationException("Workbook is too large.");
        XNamespace x = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XDocument Load(ZipArchiveEntry entry)
        {
            using var stream = entry.Open();
            return XDocument.Load(stream);
        }

        var strings = archive.GetEntry("xl/sharedStrings.xml")is { } shared ? Load(shared).Descendants(x + "si").Select(si => string.Concat(si.Descendants(x + "t").Select(t => t.Value))).ToArray() : Array.Empty<string>();
        var sheet = archive.GetEntry("xl/worksheets/sheet1.xml") ?? throw new ValidationException("Upload an Excel .xlsx workbook with a first worksheet.");
        var rows = new List<OpeningItem>();
        var header = true;
        foreach (var row in Load(sheet).Descendants(x + "row"))
        {
            var values = new string[5];
            foreach (var cell in row.Elements(x + "c"))
            {
                var reference = (string? )cell.Attribute("r") ?? "";
                var column = new string (reference.TakeWhile(char.IsAsciiLetter).ToArray());
                if (column.Length != 1 || column[0] is < 'A' or > 'E')
                    continue;
                if (cell.Element(x + "f") != null)
                    throw new ValidationException($"Replace formula in {reference} with a value.");
                var value = cell.Element(x + "v")?.Value ?? string.Concat(cell.Descendants(x + "t").Select(t => t.Value));
                if ((string? )cell.Attribute("t") == "s")
                    value = int.TryParse(value, out var index) && index >= 0 && index < strings.Length ? strings[index] : throw new ValidationException($"Invalid text in {reference}.");
                values[column[0] - 'A'] = value.Trim();
            }

            if (values.All(string.IsNullOrWhiteSpace))
                continue;
            if (header)
            {
                if (!values.SequenceEqual(new[] { "Kind", "Ledger", "Bill", "Debit", "Credit" }, StringComparer.OrdinalIgnoreCase))
                    throw new ValidationException("Use the headers Kind, Ledger, Bill, Debit, Credit in columns A–E.");
                header = false;
                continue;
            }

            decimal Amount(string? text) => string.IsNullOrWhiteSpace(text) ? 0 : decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : throw new ValidationException($"Invalid amount on worksheet row {row.Attribute("r")?.Value}.");
            rows.Add(new() { Kind = values[0] ?? "", Ledger = values[1] ?? "", Bill = values[2], Debit = Amount(values[3]), Credit = Amount(values[4]) });
        }

        if (header)
            throw new ValidationException("Workbook has no header row.");
        return Preview([..rows]);
    }
}

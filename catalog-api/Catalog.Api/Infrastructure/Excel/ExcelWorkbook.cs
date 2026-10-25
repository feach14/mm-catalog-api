using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace Catalog.Api.Infrastructure.Excel;

public sealed record ExcelExportFile(byte[] Content, string FileName)
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}

internal sealed record ExcelWorkbookColumn(int Position, string Title);

internal static class ExcelWorkbook
{
    private const string SpreadsheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const int MaxRows = 1048576;

    internal static ExcelExportFile Create(string sheetName, string fileName, IReadOnlyList<ExcelWorkbookColumn> columns,
        IEnumerable<object?[]> rows, bool includeHeaders, CancellationToken ct)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            WritePart(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="utf-8"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                  <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
                </Types>
                """);
            WritePart(archive, "_rels/.rels", """
                <?xml version="1.0" encoding="utf-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                </Relationships>
                """);
            using (var workbook = CreateWriter(archive, "xl/workbook.xml"))
            {
                workbook.WriteStartElement("workbook", SpreadsheetNamespace);
                workbook.WriteStartElement("sheets");
                workbook.WriteStartElement("sheet");
                workbook.WriteAttributeString("name", sheetName);
                workbook.WriteAttributeString("sheetId", "1");
                workbook.WriteAttributeString("r", "id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships", "rId1");
                workbook.WriteEndElement();
                workbook.WriteEndElement();
                workbook.WriteEndElement();
            }
            WritePart(archive, "xl/_rels/workbook.xml.rels", """
                <?xml version="1.0" encoding="utf-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
                </Relationships>
                """);
            WritePart(archive, "xl/styles.xml", """
                <?xml version="1.0" encoding="utf-8"?>
                <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="11"/><name val="Calibri"/></font></fonts>
                  <fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills>
                  <borders count="1"><border/></borders>
                  <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
                  <cellXfs count="2"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1"/></cellXfs>
                  <cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>
                </styleSheet>
                """);
            using var sheet = CreateWriter(archive, "xl/worksheets/sheet1.xml");
            sheet.WriteStartElement("worksheet", SpreadsheetNamespace);
            if (includeHeaders)
            {
                sheet.WriteStartElement("sheetViews");
                sheet.WriteStartElement("sheetView");
                sheet.WriteAttributeString("workbookViewId", "0");
                sheet.WriteStartElement("pane");
                sheet.WriteAttributeString("ySplit", "1");
                sheet.WriteAttributeString("topLeftCell", "A2");
                sheet.WriteAttributeString("activePane", "bottomLeft");
                sheet.WriteAttributeString("state", "frozen");
                sheet.WriteEndElement();
                sheet.WriteEndElement();
                sheet.WriteEndElement();
            }
            sheet.WriteStartElement("cols");
            foreach (var column in columns)
            {
                sheet.WriteStartElement("col");
                sheet.WriteAttributeString("min", column.Position.ToString(CultureInfo.InvariantCulture));
                sheet.WriteAttributeString("max", column.Position.ToString(CultureInfo.InvariantCulture));
                sheet.WriteAttributeString("width", Math.Clamp(column.Title.Length + 4, 16, 44).ToString(CultureInfo.InvariantCulture));
                sheet.WriteAttributeString("customWidth", "1");
                sheet.WriteEndElement();
            }
            sheet.WriteEndElement();
            sheet.WriteStartElement("sheetData");
            var rowNumber = 0;
            if (includeHeaders)
                WriteRow(sheet, ++rowNumber, columns, columns.Select(x => (object?)x.Title).ToArray(), true);
            foreach (var row in rows)
            {
                ct.ThrowIfCancellationRequested();
                if (rowNumber == MaxRows)
                    throw new BadHttpRequestException("Количество строк превышает ограничение Excel. Уточните выборку.");
                WriteRow(sheet, ++rowNumber, columns, row, false);
            }
            sheet.WriteEndElement();
            sheet.WriteEndElement();
        }
        return new ExcelExportFile(output.ToArray(), fileName);
    }

    private static XmlWriter CreateWriter(ZipArchive archive, string name) =>
        XmlWriter.Create(archive.CreateEntry(name, CompressionLevel.Fastest).Open(), new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            CloseOutput = true
        });

    private static void WritePart(ZipArchive archive, string name, string contents)
    {
        using var writer = new StreamWriter(archive.CreateEntry(name, CompressionLevel.Fastest).Open(), new UTF8Encoding(false));
        writer.Write(contents);
    }

    private static void WriteRow(XmlWriter writer, int rowNumber, IReadOnlyList<ExcelWorkbookColumn> columns,
        IReadOnlyList<object?> values, bool header)
    {
        writer.WriteStartElement("row");
        writer.WriteAttributeString("r", rowNumber.ToString(CultureInfo.InvariantCulture));
        for (var index = 0; index < columns.Count; index++)
        {
            var value = values[index];
            if (value is null)
                continue;
            writer.WriteStartElement("c");
            writer.WriteAttributeString("r", ColumnName(columns[index].Position) + rowNumber.ToString(CultureInfo.InvariantCulture));
            if (header)
                writer.WriteAttributeString("s", "1");
            switch (value)
            {
                case bool flag:
                    writer.WriteAttributeString("t", "b");
                    writer.WriteElementString("v", flag ? "1" : "0");
                    break;
                case int or decimal:
                    writer.WriteAttributeString("t", "n");
                    writer.WriteElementString("v", Convert.ToString(value, CultureInfo.InvariantCulture));
                    break;
                default:
                    writer.WriteAttributeString("t", "inlineStr");
                    writer.WriteStartElement("is");
                    writer.WriteStartElement("t");
                    writer.WriteAttributeString("xml", "space", null, "preserve");
                    writer.WriteString(EscapeText(value.ToString()!));
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                    break;
            }
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    private static string ColumnName(int position)
    {
        var name = "";
        while (position > 0)
        {
            position--;
            name = (char)('A' + (position % 26)) + name;
            position /= 26;
        }
        return name;
    }

    private static string EscapeText(string value)
    {
        if (value.Length > 32767)
            throw new BadHttpRequestException("Текст одного из полей превышает ограничение Excel в 32767 символов.");
        var result = new StringBuilder(value.Length);
        for (var index = 0; index < value.Length; index++)
        {
            var symbol = value[index];
            // Excel interprets _xHHHH_ sequences; escape their initial underscore to retain literal text.
            if (symbol == '_' && index + 6 < value.Length && value[index + 1] == 'x'
                && value[index + 6] == '_' && value.AsSpan(index + 2, 4).IndexOfAnyExcept("0123456789abcdefABCDEF") < 0)
                result.Append("_x005F_");
            else if (char.IsHighSurrogate(symbol) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]))
            {
                result.Append(symbol).Append(value[++index]);
            }
            else if (!XmlConvert.IsXmlChar(symbol) || symbol == '\r')
                result.Append("_x").Append(((int)symbol).ToString("X4", CultureInfo.InvariantCulture)).Append('_');
            else
                result.Append(symbol);
        }
        return result.ToString();
    }
}
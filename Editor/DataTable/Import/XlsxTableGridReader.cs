using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace INLO.Core.DataTable.Editor
{
    public static class XlsxTableGridReader
    {
        private static readonly XNamespace SpreadsheetNamespace =
            "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        private static readonly XNamespace RelationshipNamespace =
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        private static readonly XNamespace PackageRelationshipNamespace =
            "http://schemas.openxmlformats.org/package/2006/relationships";

        public static List<string> ReadSheetNames(string xlsxPath)
        {
            if (string.IsNullOrWhiteSpace(xlsxPath))
                throw new ArgumentException("Xlsx path is empty.");

            if (!File.Exists(xlsxPath))
                throw new FileNotFoundException("Xlsx file not found.", xlsxPath);

            using ZipArchive archive = ZipFile.OpenRead(xlsxPath);

            List<SheetInfo> sheets = ReadSheets(archive);
            return sheets.Select(sheet => sheet.Name).ToList();
        }

        public static DataTableGrid ReadFirstSheet(string xlsxPath)
        {
            return ReadSheet(xlsxPath, string.Empty);
        }

        public static DataTableGrid ReadSheet(string xlsxPath, string sheetName)
        {
            if (string.IsNullOrWhiteSpace(xlsxPath))
                throw new ArgumentException("Xlsx path is empty.");

            if (!File.Exists(xlsxPath))
                throw new FileNotFoundException("Xlsx file not found.", xlsxPath);

            using ZipArchive archive = ZipFile.OpenRead(xlsxPath);

            ValidateArchive(archive);

            List<string> sharedStrings = ReadSharedStrings(archive);

            ZipArchiveEntry worksheetEntry = GetWorksheetEntry(
                archive,
                sheetName);

            if (worksheetEntry == null)
            {
                throw new FileNotFoundException(
                    string.IsNullOrWhiteSpace(sheetName)
                        ? "First worksheet not found."
                        : $"Worksheet not found: {sheetName}");
            }

            using Stream worksheetStream = worksheetEntry.Open();

            XDocument worksheetDocument = XDocument.Load(worksheetStream);

            ValidateSupportedWorksheet(worksheetDocument);

            List<ParsedRow> parsedRows = ParseRows(worksheetDocument, sharedStrings);

            return BuildGrid(parsedRows);
        }

        private static void ValidateArchive(ZipArchive archive)
        {
            if (archive.Entries.Any(
                entry => entry.FullName.StartsWith(
                    "xl/externalLinks/",
                    StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidDataException(
                    "Xlsx contains external links, which are not supported by the DataTable Importer.");
            }
        }

        private static void ValidateSupportedWorksheet(XDocument worksheetDocument)
        {
            XElement mergedCell = worksheetDocument
                .Descendants(SpreadsheetNamespace + "mergeCell")
                .FirstOrDefault();

            if (mergedCell != null)
            {
                throw new InvalidDataException(
                    $"Merged cells are not supported. Cell range: {mergedCell.Attribute("ref")?.Value}");
            }

            foreach (XElement cell in worksheetDocument.Descendants(SpreadsheetNamespace + "c"))
            {
                string reference = cell.Attribute("r")?.Value ?? "(unknown)";

                if (cell.Element(SpreadsheetNamespace + "f") != null)
                {
                    throw new InvalidDataException(
                        $"Formula cells are not supported. Cell: {reference}");
                }

                string type = cell.Attribute("t")?.Value;

                if (string.Equals(type, "e", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(
                        $"Excel error cells are not supported. Cell: {reference}");
                }


            }
        }

        private static ZipArchiveEntry GetWorksheetEntry(
            ZipArchive archive,
            string sheetName)
        {
            List<SheetInfo> sheets = ReadSheets(archive);

            if (sheets.Count == 0)
                return archive.GetEntry("xl/worksheets/sheet1.xml");

            SheetInfo targetSheet;

            if (string.IsNullOrWhiteSpace(sheetName))
            {
                targetSheet = sheets[0];
            }
            else
            {
                targetSheet = sheets.FirstOrDefault(
                    sheet => string.Equals(sheet.Name, sheetName, StringComparison.OrdinalIgnoreCase));
            }

            if (string.IsNullOrWhiteSpace(targetSheet.Target))
                return null;

            string target = targetSheet.Target.Replace("\\", "/");

            if (target.StartsWith("/"))
                target = target.Substring(1);

            if (!target.StartsWith("xl/"))
                target = "xl/" + target;

            return archive.GetEntry(target);
        }

        private static List<SheetInfo> ReadSheets(ZipArchive archive)
        {
            ZipArchiveEntry workbookEntry = archive.GetEntry("xl/workbook.xml");
            ZipArchiveEntry relationshipEntry = archive.GetEntry("xl/_rels/workbook.xml.rels");

            if (workbookEntry == null || relationshipEntry == null)
                return new List<SheetInfo>();

            Dictionary<string, string> relationshipTargets = ReadWorkbookRelationships(relationshipEntry);

            using Stream workbookStream = workbookEntry.Open();

            XDocument workbookDocument = XDocument.Load(workbookStream);

            List<SheetInfo> result = new();

            IEnumerable<XElement> sheetElements =
                workbookDocument.Descendants(SpreadsheetNamespace + "sheet");

            foreach (XElement sheetElement in sheetElements)
            {
                string name = sheetElement.Attribute("name")?.Value;
                string relationshipId = sheetElement.Attribute(RelationshipNamespace + "id")?.Value;

                if (string.IsNullOrWhiteSpace(name) ||
                    string.IsNullOrWhiteSpace(relationshipId))
                {
                    continue;
                }

                relationshipTargets.TryGetValue(relationshipId, out string target);

                result.Add(new SheetInfo(name, target));
            }

            return result;
        }

        private static Dictionary<string, string> ReadWorkbookRelationships(
            ZipArchiveEntry relationshipEntry)
        {
            using Stream stream = relationshipEntry.Open();

            XDocument relationshipDocument = XDocument.Load(stream);

            Dictionary<string, string> result = new();

            IEnumerable<XElement> relationshipElements =
                relationshipDocument.Descendants(PackageRelationshipNamespace + "Relationship");

            foreach (XElement relationshipElement in relationshipElements)
            {
                string id = relationshipElement.Attribute("Id")?.Value;
                string target = relationshipElement.Attribute("Target")?.Value;

                if (string.IsNullOrWhiteSpace(id) ||
                    string.IsNullOrWhiteSpace(target))
                {
                    continue;
                }

                result[id] = target;
            }

            return result;
        }

        private static List<string> ReadSharedStrings(ZipArchive archive)
        {
            ZipArchiveEntry sharedStringsEntry = archive.GetEntry("xl/sharedStrings.xml");

            if (sharedStringsEntry == null)
                return new List<string>();

            using Stream stream = sharedStringsEntry.Open();

            XDocument document = XDocument.Load(stream);

            return document
                .Descendants(SpreadsheetNamespace + "si")
                .Select(ReadSharedStringItem)
                .ToList();
        }

        private static string ReadSharedStringItem(XElement sharedStringItem)
        {
            IEnumerable<XElement> textElements =
                sharedStringItem.Descendants(SpreadsheetNamespace + "t");

            return string.Concat(textElements.Select(element => element.Value));
        }

        private static List<ParsedRow> ParseRows(
            XDocument worksheetDocument,
            List<string> sharedStrings)
        {
            List<ParsedRow> result = new();

            IEnumerable<XElement> rowElements =
                worksheetDocument.Descendants(SpreadsheetNamespace + "row");

            foreach (XElement rowElement in rowElements)
            {
                int rowNumber = ReadRowNumber(rowElement);

                Dictionary<int, string> cells = new();

                IEnumerable<XElement> cellElements =
                    rowElement.Elements(SpreadsheetNamespace + "c");

                foreach (XElement cellElement in cellElements)
                {
                    string cellReference = cellElement.Attribute("r")?.Value;

                    if (string.IsNullOrWhiteSpace(cellReference))
                        continue;

                    int columnIndex = GetColumnIndexFromCellReference(cellReference);
                    string value = ReadCellValue(cellElement, sharedStrings);

                    cells[columnIndex] = value;
                }

                if (cells.Count == 0)
                    continue;

                result.Add(new ParsedRow(rowNumber, cells));
            }

            return result;
        }

        private static int ReadRowNumber(XElement rowElement)
        {
            string rowNumberText = rowElement.Attribute("r")?.Value;

            if (int.TryParse(rowNumberText, out int rowNumber))
                return rowNumber;

            return -1;
        }

        private static string ReadCellValue(
            XElement cellElement,
            List<string> sharedStrings)
        {
            string type = cellElement.Attribute("t")?.Value;

            if (type == "inlineStr")
            {
                XElement inlineText =
                    cellElement
                        .Descendants(SpreadsheetNamespace + "t")
                        .FirstOrDefault();

                return inlineText?.Value ?? string.Empty;
            }

            XElement valueElement = cellElement.Element(SpreadsheetNamespace + "v");

            if (valueElement == null)
                return string.Empty;

            string rawValue = valueElement.Value;

            if (type == "s")
            {
                if (int.TryParse(rawValue, out int sharedStringIndex))
                {
                    if (sharedStringIndex >= 0 && sharedStringIndex < sharedStrings.Count)
                        return sharedStrings[sharedStringIndex];
                }

                return string.Empty;
            }

            return rawValue;
        }

        private static DataTableGrid BuildGrid(List<ParsedRow> parsedRows)
        {
            DataTableGrid grid = new();

            if (parsedRows.Count == 0)
                return grid;

            ParsedRow headerRow = parsedRows[0];

            int maxColumnIndex = parsedRows
                .SelectMany(row => row.Cells.Keys)
                .DefaultIfEmpty(0)
                .Max();

            for (int columnIndex = 0; columnIndex <= maxColumnIndex; columnIndex++)
            {
                headerRow.Cells.TryGetValue(columnIndex, out string header);
                grid.Headers.Add((header ?? string.Empty).Trim());
            }

            for (int i = 1; i < parsedRows.Count; i++)
            {
                ParsedRow parsedRow = parsedRows[i];

                List<string> cells = new();

                bool hasAnyValue = false;

                for (int columnIndex = 0; columnIndex <= maxColumnIndex; columnIndex++)
                {
                    parsedRow.Cells.TryGetValue(columnIndex, out string cellValue);

                    cellValue ??= string.Empty;

                    if (!string.IsNullOrWhiteSpace(cellValue))
                        hasAnyValue = true;

                    cells.Add(cellValue);
                }

                if (!hasAnyValue)
                    continue;

                grid.Rows.Add(new DataTableGridRow(parsedRow.SourceRowNumber, cells));
            }

            return grid;
        }

        private static int GetColumnIndexFromCellReference(string cellReference)
        {
            int columnIndex = 0;

            for (int i = 0; i < cellReference.Length; i++)
            {
                char character = cellReference[i];

                if (!char.IsLetter(character))
                    break;

                columnIndex *= 26;
                columnIndex += char.ToUpperInvariant(character) - 'A' + 1;
            }

            return columnIndex - 1;
        }

        private readonly struct ParsedRow
        {
            public readonly int SourceRowNumber;
            public readonly Dictionary<int, string> Cells;

            public ParsedRow(int sourceRowNumber, Dictionary<int, string> cells)
            {
                SourceRowNumber = sourceRowNumber;
                Cells = cells;
            }
        }

        private readonly struct SheetInfo
        {
            public readonly string Name;
            public readonly string Target;

            public SheetInfo(string name, string target)
            {
                Name = name;
                Target = target;
            }
        }
    }
}

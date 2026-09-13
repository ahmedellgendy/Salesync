using ClosedXML.Excel;
using Salesync.Application.Modules.DataImport.Interfaces;

namespace Salesync.Infrastructure.DataImport.Excel
{
    public class ExcelImportService : IExcelImportService
    {
        public Task<IReadOnlyList<Dictionary<string, string>>> ReadAsync(
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            if (!stream.CanRead)
                throw new InvalidOperationException(
                    "The uploaded Excel stream cannot be read.");

            if (stream.CanSeek)
                stream.Position = 0;


            using var workbook =
                new XLWorkbook(stream);

            var worksheet =
                workbook.Worksheets.FirstOrDefault();

            if (worksheet is null)
                throw new InvalidOperationException(
                    "The Excel file does not contain any worksheets.");


            var usedRange =
                worksheet.RangeUsed();

            if (usedRange is null)
            {
                return Task.FromResult<
                    IReadOnlyList<Dictionary<string, string>>>(
                    Array.Empty<Dictionary<string, string>>());
            }


            var firstRowNumber =
                usedRange.RangeAddress.FirstAddress.RowNumber;

            var lastRowNumber =
                usedRange.RangeAddress.LastAddress.RowNumber;

            var firstColumnNumber =
                usedRange.RangeAddress.FirstAddress.ColumnNumber;

            var lastColumnNumber =
                usedRange.RangeAddress.LastAddress.ColumnNumber;


            var headers =
                new List<(int ColumnNumber, string Header)>();


            var duplicateHeaders =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);


            for (var columnNumber = firstColumnNumber;
                 columnNumber <= lastColumnNumber;
                 columnNumber++)
            {
                var header =
                    worksheet
                        .Cell(firstRowNumber, columnNumber)
                        .GetString()
                        .Trim();


                if (string.IsNullOrWhiteSpace(header))
                {
                    throw new InvalidOperationException(
                        $"Excel header at column {columnNumber} is empty.");
                }


                if (headers.Any(x =>
                        x.Header.Equals(
                            header,
                            StringComparison.OrdinalIgnoreCase)))
                {
                    duplicateHeaders.Add(header);
                }


                headers.Add(
                    (columnNumber, header));
            }


            if (duplicateHeaders.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Duplicate Excel headers: {string.Join(", ", duplicateHeaders)}");
            }


            var rows =
                new List<Dictionary<string, string>>();


            for (var rowNumber = firstRowNumber + 1;
                 rowNumber <= lastRowNumber;
                 rowNumber++)
            {
                cancellationToken.ThrowIfCancellationRequested();


                var row =
                    new Dictionary<string, string>(
                        StringComparer.OrdinalIgnoreCase);


                var hasValue =
                    false;


                foreach (var header in headers)
                {
                    var value =
                        worksheet
                            .Cell(rowNumber, header.ColumnNumber)
                            .GetFormattedString()
                            .Trim();


                    if (!string.IsNullOrWhiteSpace(value))
                        hasValue = true;


                    row[header.Header] =
                        value;
                }


                /*
                 * Skip completely empty Excel rows.
                 */
                if (!hasValue)
                    continue;


                /*
                 * Keep the physical Excel row number available
                 * to validation/error reporting.
                 */
                row["__RowNumber"] =
                    rowNumber.ToString();


                rows.Add(row);
            }


            return Task.FromResult<
                IReadOnlyList<Dictionary<string, string>>>(
                rows);
        }


        public byte[] GenerateTemplate(
            string sheetName,
            IReadOnlyList<string> headers)
        {
            if (headers is null ||
                headers.Count == 0)
            {
                throw new ArgumentException(
                    "At least one Excel header is required.",
                    nameof(headers));
            }


            using var workbook =
                new XLWorkbook();


            var worksheet =
                workbook.Worksheets.Add(
                    string.IsNullOrWhiteSpace(sheetName)
                        ? "Import"
                        : sheetName);


            for (var index = 0;
                 index < headers.Count;
                 index++)
            {
                var cell =
                    worksheet.Cell(
                        1,
                        index + 1);


                cell.Value =
                    headers[index];

                cell.Style.Font.Bold =
                    true;

                cell.Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;

                cell.Style.Alignment.Vertical =
                    XLAlignmentVerticalValues.Center;
            }


            var headerRange =
                worksheet.Range(
                    1,
                    1,
                    1,
                    headers.Count);


            headerRange.Style.Fill.BackgroundColor =
                XLColor.FromHtml("#EAF5F8");

            headerRange.Style.Font.FontColor =
                XLColor.FromHtml("#087F9C");


            worksheet.SheetView.FreezeRows(1);

            worksheet.Columns()
                .AdjustToContents();


            foreach (var column in worksheet.ColumnsUsed())
            {
                if (column.Width < 15)
                    column.Width = 15;

                if (column.Width > 35)
                    column.Width = 35;
            }


            using var memoryStream =
                new MemoryStream();


            workbook.SaveAs(memoryStream);


            return memoryStream.ToArray();
        }


        public byte[] GenerateErrorReport(
            string sheetName,
            IReadOnlyList<string> headers,
            IReadOnlyList<Dictionary<string, string>> rows,
            IReadOnlyDictionary<int, List<string>> rowErrors)
        {
            using var workbook =
                new XLWorkbook();


            var worksheet =
                workbook.Worksheets.Add(
                    string.IsNullOrWhiteSpace(sheetName)
                        ? "Errors"
                        : sheetName);


            var outputHeaders =
                headers
                    .Concat(new[]
                    {
                        "Errors"
                    })
                    .ToList();


            for (var index = 0;
                 index < outputHeaders.Count;
                 index++)
            {
                worksheet.Cell(
                        1,
                        index + 1)
                    .Value =
                    outputHeaders[index];
            }


            var headerRange =
                worksheet.Range(
                    1,
                    1,
                    1,
                    outputHeaders.Count);


            headerRange.Style.Font.Bold =
                true;

            headerRange.Style.Fill.BackgroundColor =
                XLColor.FromHtml("#FEE4E2");

            headerRange.Style.Font.FontColor =
                XLColor.FromHtml("#B42318");


            var outputRowNumber =
                2;


            foreach (var row in rows)
            {
                if (!TryGetExcelRowNumber(
                        row,
                        out var excelRowNumber))
                {
                    continue;
                }


                if (!rowErrors.TryGetValue(
                        excelRowNumber,
                        out var errors) ||
                    errors.Count == 0)
                {
                    continue;
                }


                for (var index = 0;
                     index < headers.Count;
                     index++)
                {
                    row.TryGetValue(
                        headers[index],
                        out var value);


                    worksheet.Cell(
                            outputRowNumber,
                            index + 1)
                        .Value =
                        value ?? string.Empty;
                }


                worksheet.Cell(
                        outputRowNumber,
                        outputHeaders.Count)
                    .Value =
                    string.Join(
                        " | ",
                        errors);


                outputRowNumber++;
            }


            worksheet.SheetView.FreezeRows(1);

            worksheet.Columns()
                .AdjustToContents();


            foreach (var column in worksheet.ColumnsUsed())
            {
                if (column.Width < 15)
                    column.Width = 15;

                if (column.Width > 45)
                    column.Width = 45;
            }


            using var memoryStream =
                new MemoryStream();


            workbook.SaveAs(memoryStream);


            return memoryStream.ToArray();
        }


        private static bool TryGetExcelRowNumber(
            IReadOnlyDictionary<string, string> row,
            out int rowNumber)
        {
            rowNumber =
                0;


            if (!row.TryGetValue(
                    "__RowNumber",
                    out var value))
            {
                return false;
            }


            return int.TryParse(
                value,
                out rowNumber);
        }
    }
}
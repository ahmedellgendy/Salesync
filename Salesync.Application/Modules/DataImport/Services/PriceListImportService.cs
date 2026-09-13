using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.DataImport.Dtos;
using Salesync.Application.Modules.DataImport.Dtos.PriceLists;
using Salesync.Application.Modules.DataImport.Interfaces;
using Salesync.Domain.Modules.DataImport.Entities;
using Salesync.Domain.Modules.DataImport.Enums;
using Salesync.Domain.Modules.MasterData.Entities;
using System.Globalization;
using System.Text.Json;

namespace Salesync.Application.Modules.DataImport.Services
{
    public class PriceListImportService :
        IPriceListImportService
    {
        private static readonly string[] Headers =
        {
            "Code",
            "Name",
            "Description",
            "IsDefault",
            "ValidFrom",
            "ValidTo"
        };

        private readonly IUnitOfWork _unitOfWork;

        private readonly IExcelImportService
            _excelImportService;


        public PriceListImportService(
            IUnitOfWork unitOfWork,
            IExcelImportService excelImportService)
        {
            _unitOfWork =
                unitOfWork;

            _excelImportService =
                excelImportService;
        }


        // =====================================================
        // TEMPLATE
        // =====================================================

        public byte[] GenerateTemplate()
        {
            return _excelImportService
                .GenerateTemplate(
                    "Price Lists",
                    Headers);
        }


        // =====================================================
        // VALIDATE
        // =====================================================

        public async Task<ImportPreviewDto>
            ValidateAsync(
                Stream fileStream,
                string fileName,
                string? userId,
                CancellationToken cancellationToken =
                    default)
        {
            if (fileStream is null)
            {
                throw new ArgumentNullException(
                    nameof(fileStream));
            }


            if (string.IsNullOrWhiteSpace(
                    fileName))
            {
                throw new ArgumentException(
                    "File name is required.",
                    nameof(fileName));
            }


            var rows =
                await _excelImportService
                    .ReadAsync(
                        fileStream,
                        cancellationToken);


            var batch =
                new ImportBatch
                {
                    ImportType =
                        ImportType.PriceLists,

                    Status =
                        ImportStatus.Uploaded,

                    FileName =
                        fileName,

                    ImportedByUserId =
                        userId,

                    TotalRows =
                        rows.Count,

                    CreatedAt =
                        DateTime.UtcNow,

                    IsActive =
                        true
                };


            await _unitOfWork.ImportBatches
                .AddAsync(batch);

            await _unitOfWork
                .CompleteAsync();


            if (rows.Count == 0)
            {
                batch.Status =
                    ImportStatus.ValidationFailed;

                batch.ValidRows =
                    0;

                batch.ErrorRows =
                    1;

                batch.ValidatedAt =
                    DateTime.UtcNow;


                await _unitOfWork
                    .CompleteAsync();


                return new ImportPreviewDto
                {
                    BatchId =
                        batch.Id,

                    ImportType =
                        ImportType.PriceLists,

                    FileName =
                        fileName,

                    TotalRows =
                        0,

                    ValidRows =
                        0,

                    ErrorRows =
                        1,

                    Errors =
                    {
                        new ImportValidationErrorDto
                        {
                            RowNumber =
                                0,

                            Message =
                                "The Excel file does not contain any data rows."
                        }
                    }
                };
            }


            ValidateHeaders(rows);


            var existingPriceLists =
                (await _unitOfWork.PriceLists
                    .GetAllAsync())
                .ToList();


            var existingCodes =
                existingPriceLists
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(
                            x.Code))
                    .Select(x =>
                        x.Code.Trim())
                    .ToHashSet(
                        StringComparer.OrdinalIgnoreCase);


            var excelCodes =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);


            var previewErrors =
                new List<
                    ImportValidationErrorDto>();


            var validRows =
                0;


            var defaultRows =
                0;


            foreach (var row in rows)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();


                var rowNumber =
                    GetRowNumber(row);


                var priceListRow =
                    MapRow(
                        row,
                        rowNumber);


                var rowErrors =
                    new List<string>();


                // =============================================
                // CODE
                // =============================================

                if (string.IsNullOrWhiteSpace(
                        priceListRow.Code))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "Code",
                        "Price list code is required.");
                }
                else
                {
                    var normalizedCode =
                        priceListRow.Code
                            .Trim()
                            .ToUpperInvariant();


                    if (normalizedCode.Length >
                        50)
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "Code",
                            "Price list code cannot exceed 50 characters.");
                    }


                    if (!excelCodes.Add(
                            normalizedCode))
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "Code",
                            "Duplicate price list code inside the Excel file.");
                    }


                    if (existingCodes.Contains(
                            normalizedCode))
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "Code",
                            "Price list code already exists in the system.");
                    }
                }


                // =============================================
                // NAME
                // =============================================

                if (string.IsNullOrWhiteSpace(
                        priceListRow.Name))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "Name",
                        "Price list name is required.");
                }
                else if (priceListRow.Name.Length >
                         150)
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "Name",
                        "Price list name cannot exceed 150 characters.");
                }


                // =============================================
                // DESCRIPTION
                // =============================================

                if (!string.IsNullOrWhiteSpace(
                        priceListRow.Description)
                    &&
                    priceListRow.Description.Length >
                    500)
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "Description",
                        "Description cannot exceed 500 characters.");
                }


                // =============================================
                // IS DEFAULT
                // =============================================

                bool isDefault =
                    false;


                if (!TryParseBoolean(
                        priceListRow.IsDefault,
                        out isDefault))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "IsDefault",
                        "IsDefault must be True/False, Yes/No, or 1/0.");
                }
                else if (isDefault)
                {
                    defaultRows++;
                }


                // =============================================
                // VALID FROM
                // =============================================

                DateTime? validFrom =
                    null;


                if (!string.IsNullOrWhiteSpace(
                        priceListRow.ValidFrom))
                {
                    if (!TryParseDate(
                            priceListRow.ValidFrom,
                            out var parsedValidFrom))
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "ValidFrom",
                            "ValidFrom must be a valid date.");
                    }
                    else
                    {
                        validFrom =
                            parsedValidFrom.Date;
                    }
                }


                // =============================================
                // VALID TO
                // =============================================

                DateTime? validTo =
                    null;


                if (!string.IsNullOrWhiteSpace(
                        priceListRow.ValidTo))
                {
                    if (!TryParseDate(
                            priceListRow.ValidTo,
                            out var parsedValidTo))
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "ValidTo",
                            "ValidTo must be a valid date.");
                    }
                    else
                    {
                        validTo =
                            parsedValidTo.Date;
                    }
                }


                if (validFrom.HasValue &&
                    validTo.HasValue &&
                    validTo.Value <
                    validFrom.Value)
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "ValidTo",
                        "ValidTo cannot be before ValidFrom.");
                }


                // =============================================
                // STAGING
                // =============================================

                var stagingRow =
                    new ImportBatchRow
                    {
                        ImportBatchId =
                            batch.Id,

                        RowNumber =
                            rowNumber,

                        DataJson =
                            JsonSerializer.Serialize(
                                priceListRow),

                        IsValid =
                            rowErrors.Count == 0,

                        ErrorsJson =
                            rowErrors.Count == 0
                                ? null
                                : JsonSerializer.Serialize(
                                    rowErrors),

                        CreatedAt =
                            DateTime.UtcNow,

                        IsActive =
                            true
                    };


                await _unitOfWork.ImportBatchRows
                    .AddAsync(stagingRow);


                if (stagingRow.IsValid)
                {
                    validRows++;
                }
            }


            // =============================================
            // ONLY ONE DEFAULT INSIDE IMPORT FILE
            // =============================================

            if (defaultRows > 1)
            {
                var defaultRowsFromFile =
                    rows
                        .Select(row =>
                            new
                            {
                                Row =
                                    row,

                                RowNumber =
                                    GetRowNumber(row),

                                Data =
                                    MapRow(
                                        row,
                                        GetRowNumber(row))
                            })
                        .Where(x =>
                            TryParseBoolean(
                                x.Data.IsDefault,
                                out var value)
                            &&
                            value)
                        .ToList();


                foreach (var defaultRow in
                         defaultRowsFromFile)
                {
                    previewErrors.Add(
                        new ImportValidationErrorDto
                        {
                            RowNumber =
                                defaultRow.RowNumber,

                            ColumnName =
                                "IsDefault",

                            Message =
                                "Only one price list can be marked as default in the same import file."
                        });
                }


                var stagedRows =
                    (await _unitOfWork
                        .ImportBatchRows
                        .FindAsync(x =>
                            x.ImportBatchId ==
                            batch.Id))
                    .ToList();


                foreach (var stagedRow in
                         stagedRows)
                {
                    var dto =
                        JsonSerializer
                            .Deserialize<
                                PriceListImportRowDto>(
                                stagedRow.DataJson);


                    if (dto is null)
                        continue;


                    if (!TryParseBoolean(
                            dto.IsDefault,
                            out var value)
                        ||
                        !value)
                    {
                        continue;
                    }


                    var errors =
                        string.IsNullOrWhiteSpace(
                            stagedRow.ErrorsJson)
                            ? new List<string>()
                            : JsonSerializer
                                .Deserialize<
                                    List<string>>(
                                    stagedRow.ErrorsJson)
                              ?? new List<string>();


                    errors.Add(
                        "Only one price list can be marked as default in the same import file.");


                    stagedRow.IsValid =
                        false;

                    stagedRow.ErrorsJson =
                        JsonSerializer.Serialize(
                            errors);
                }


                validRows =
                    stagedRows.Count(x =>
                        x.IsValid);
            }


            batch.ValidRows =
                validRows;


            batch.ErrorRows =
                rows.Count -
                validRows;


            batch.ValidatedAt =
                DateTime.UtcNow;


            batch.Status =
                batch.ErrorRows == 0
                    ? ImportStatus.ReadyToImport
                    : ImportStatus.ValidationFailed;


            await _unitOfWork
                .CompleteAsync();


            return new ImportPreviewDto
            {
                BatchId =
                    batch.Id,

                ImportType =
                    ImportType.PriceLists,

                FileName =
                    fileName,

                TotalRows =
                    rows.Count,

                ValidRows =
                    batch.ValidRows,

                ErrorRows =
                    batch.ErrorRows,

                Errors =
                    previewErrors
            };
        }


        // =====================================================
        // IMPORT
        // =====================================================

        public async Task<ImportResultDto>
            ImportAsync(
                int batchId,
                CancellationToken cancellationToken =
                    default)
        {
            var batch =
                await _unitOfWork.ImportBatches
                    .GetByIdAsync(batchId);


            if (batch is null ||
                !batch.IsActive)
            {
                throw new KeyNotFoundException(
                    $"Import batch {batchId} was not found.");
            }


            if (batch.ImportType !=
                ImportType.PriceLists)
            {
                throw new InvalidOperationException(
                    "The selected batch is not a price list import.");
            }


            if (batch.Status !=
                ImportStatus.ReadyToImport)
            {
                throw new InvalidOperationException(
                    "The import batch is not ready to import.");
            }


            var stagingRows =
                (await _unitOfWork.ImportBatchRows
                    .FindAsync(x =>
                        x.ImportBatchId ==
                        batchId))
                .OrderBy(x =>
                    x.RowNumber)
                .ToList();


            if (stagingRows.Count == 0)
            {
                throw new InvalidOperationException(
                    "The import batch does not contain any rows.");
            }


            if (stagingRows.Any(x =>
                    !x.IsValid))
            {
                throw new InvalidOperationException(
                    "The import batch contains validation errors.");
            }


            var existingPriceLists =
                (await _unitOfWork.PriceLists
                    .GetAllAsync())
                .ToList();


            var currentCodes =
                existingPriceLists
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(
                            x.Code))
                    .Select(x =>
                        x.Code.Trim())
                    .ToHashSet(
                        StringComparer.OrdinalIgnoreCase);


            await _unitOfWork
                .BeginTransactionAsync();


            try
            {
                batch.Status =
                    ImportStatus.Importing;


                await _unitOfWork
                    .CompleteAsync();


                var importedRows =
                    0;


                PriceList? importedDefaultList =
                    null;


                foreach (var stagingRow in
                         stagingRows)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();


                    var dto =
                        JsonSerializer
                            .Deserialize<
                                PriceListImportRowDto>(
                                stagingRow.DataJson);


                    if (dto is null)
                    {
                        throw new InvalidOperationException(
                            $"Unable to read import row {stagingRow.RowNumber}.");
                    }


                    var code =
                        dto.Code
                            .Trim()
                            .ToUpperInvariant();


                    if (currentCodes.Contains(code))
                    {
                        throw new InvalidOperationException(
                            $"Price list code '{code}' already exists.");
                    }


                    var isDefault =
                        ParseBooleanForImport(
                            dto.IsDefault,
                            "IsDefault");


                    var validFrom =
                        ParseNullableDateForImport(
                            dto.ValidFrom,
                            "ValidFrom");


                    var validTo =
                        ParseNullableDateForImport(
                            dto.ValidTo,
                            "ValidTo");


                    if (validFrom.HasValue &&
                        validTo.HasValue &&
                        validTo.Value.Date <
                        validFrom.Value.Date)
                    {
                        throw new InvalidOperationException(
                            $"Price list '{code}' has an invalid validity period.");
                    }


                    var priceList =
                        new PriceList
                        {
                            Code =
                                code,

                            Name =
                                dto.Name.Trim(),

                            Description =
                                NormalizeNullable(
                                    dto.Description),

                            IsDefault =
                                isDefault,

                            ValidFrom =
                                validFrom,

                            ValidTo =
                                validTo,

                            IsActive =
                                true,

                            CreatedAt =
                                DateTime.UtcNow
                        };


                    await _unitOfWork.PriceLists
                        .AddAsync(priceList);


                    if (isDefault)
                    {
                        importedDefaultList =
                            priceList;
                    }


                    currentCodes.Add(code);

                    importedRows++;
                }


                // =============================================
                // DEFAULT PRICE LIST
                // =============================================

                if (importedDefaultList is not null)
                {
                    foreach (var existing in
                             existingPriceLists
                                 .Where(x =>
                                     x.IsDefault))
                    {
                        existing.IsDefault =
                            false;
                    }
                }


                batch.ImportedRows =
                    importedRows;


                batch.Status =
                    ImportStatus.Completed;


                batch.CompletedAt =
                    DateTime.UtcNow;


                await _unitOfWork
                    .CompleteAsync();


                await _unitOfWork
                    .CommitTransactionAsync();


                return new ImportResultDto
                {
                    BatchId =
                        batch.Id,

                    ImportedRows =
                        importedRows,

                    Success =
                        true,

                    Message =
                        $"{importedRows} price lists imported successfully."
                };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();

                _unitOfWork.ClearTracking();

                var failedBatch =
                    await _unitOfWork.ImportBatches
                        .GetByIdAsync(batchId);

                if (failedBatch is not null)
                {
                    failedBatch.Status =
                        ImportStatus.Failed;

                    failedBatch.Notes =
                        "Product import failed during database commit.";

                    await _unitOfWork.CompleteAsync();
                }

                throw;
            }
        }


        // =====================================================
        // ERROR REPORT
        // =====================================================

        public async Task<byte[]>
            GenerateErrorReportAsync(
                int batchId,
                CancellationToken cancellationToken =
                    default)
        {
            var batch =
                await _unitOfWork.ImportBatches
                    .GetByIdAsync(batchId);


            if (batch is null)
            {
                throw new KeyNotFoundException(
                    $"Import batch {batchId} was not found.");
            }


            if (batch.ImportType !=
                ImportType.PriceLists)
            {
                throw new InvalidOperationException(
                    "The selected batch is not a price list import.");
            }


            var stagingRows =
                (await _unitOfWork.ImportBatchRows
                    .FindAsync(x =>
                        x.ImportBatchId ==
                        batchId))
                .OrderBy(x =>
                    x.RowNumber)
                .ToList();


            var rows =
                new List<
                    Dictionary<string, string>>();


            var errors =
                new Dictionary<
                    int,
                    List<string>>();


            foreach (var stagingRow in
                     stagingRows)
            {
                var dto =
                    JsonSerializer
                        .Deserialize<
                            PriceListImportRowDto>(
                            stagingRow.DataJson);


                if (dto is null)
                    continue;


                rows.Add(
                    new Dictionary<
                        string,
                        string>(
                        StringComparer.OrdinalIgnoreCase)
                    {
                        ["Code"] =
                            dto.Code,

                        ["Name"] =
                            dto.Name,

                        ["Description"] =
                            dto.Description
                            ?? string.Empty,

                        ["IsDefault"] =
                            dto.IsDefault,

                        ["ValidFrom"] =
                            dto.ValidFrom
                            ?? string.Empty,

                        ["ValidTo"] =
                            dto.ValidTo
                            ?? string.Empty,

                        ["__RowNumber"] =
                            dto.RowNumber
                                .ToString()
                    });


                if (!string.IsNullOrWhiteSpace(
                        stagingRow.ErrorsJson))
                {
                    errors[stagingRow.RowNumber] =
                        JsonSerializer
                            .Deserialize<
                                List<string>>(
                                stagingRow.ErrorsJson)
                        ??
                        new List<string>();
                }
            }


            return _excelImportService
                .GenerateErrorReport(
                    "Price List Errors",
                    Headers,
                    rows,
                    errors);
        }


        // =====================================================
        // HISTORY
        // =====================================================

        public async Task<
            IEnumerable<ImportBatchHistoryDto>>
            GetHistoryAsync(
                CancellationToken cancellationToken =
                    default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();


            var batches =
                await _unitOfWork.ImportBatches
                    .GetAllAsync();


            return batches
                .Where(x =>
                    x.ImportType ==
                    ImportType.PriceLists)
                .OrderByDescending(x =>
                    x.CreatedAt)
                .Select(x =>
                    new ImportBatchHistoryDto
                    {
                        Id =
                            x.Id,

                        ImportType =
                            x.ImportType,

                        Status =
                            x.Status,

                        FileName =
                            x.FileName,

                        TotalRows =
                            x.TotalRows,

                        ValidRows =
                            x.ValidRows,

                        ErrorRows =
                            x.ErrorRows,

                        ImportedRows =
                            x.ImportedRows,

                        ImportedByUserId =
                            x.ImportedByUserId,

                        CreatedAt =
                            x.CreatedAt,

                        CompletedAt =
                            x.CompletedAt
                    })
                .ToList();
        }


        // =====================================================
        // HELPERS
        // =====================================================

        private static void ValidateHeaders(
            IReadOnlyList<
                Dictionary<string, string>>
                rows)
        {
            if (rows.Count == 0)
                return;


            var firstRow =
                rows[0];


            var missingHeaders =
                Headers
                    .Where(header =>
                        !firstRow
                            .ContainsKey(header))
                    .ToList();


            if (missingHeaders.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Missing required Excel headers: {string.Join(", ", missingHeaders)}");
            }
        }


        private static PriceListImportRowDto
            MapRow(
                IReadOnlyDictionary<
                    string,
                    string> row,
                int rowNumber)
        {
            return new PriceListImportRowDto
            {
                RowNumber =
                    rowNumber,

                Code =
                    GetValue(
                        row,
                        "Code"),

                Name =
                    GetValue(
                        row,
                        "Name"),

                Description =
                    NormalizeNullable(
                        GetValue(
                            row,
                            "Description")),

                IsDefault =
                    GetValueOrDefault(
                        row,
                        "IsDefault",
                        "false"),

                ValidFrom =
                    NormalizeNullable(
                        GetValue(
                            row,
                            "ValidFrom")),

                ValidTo =
                    NormalizeNullable(
                        GetValue(
                            row,
                            "ValidTo"))
            };
        }


        private static int GetRowNumber(
            IReadOnlyDictionary<
                string,
                string> row)
        {
            if (!row.TryGetValue(
                    "__RowNumber",
                    out var value)
                ||
                !int.TryParse(
                    value,
                    out var rowNumber))
            {
                throw new InvalidOperationException(
                    "Unable to determine the Excel row number.");
            }


            return rowNumber;
        }


        private static string GetValue(
            IReadOnlyDictionary<
                string,
                string> row,
            string key)
        {
            return row.TryGetValue(
                key,
                out var value)
                ? value.Trim()
                : string.Empty;
        }


        private static string GetValueOrDefault(
            IReadOnlyDictionary<
                string,
                string> row,
            string key,
            string defaultValue)
        {
            var value =
                GetValue(
                    row,
                    key);


            return string.IsNullOrWhiteSpace(
                value)
                ? defaultValue
                : value;
        }


        private static bool TryParseBoolean(
            string value,
            out bool result)
        {
            result =
                false;


            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return false;
            }


            switch (value
                .Trim()
                .ToLowerInvariant())
            {
                case "true":
                case "1":
                case "yes":
                case "y":

                    result =
                        true;

                    return true;


                case "false":
                case "0":
                case "no":
                case "n":

                    result =
                        false;

                    return true;


                default:

                    return false;
            }
        }


        private static bool TryParseDate(
            string value,
            out DateTime result)
        {
            if (DateTime.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out result))
            {
                return true;
            }


            return DateTime.TryParse(
                value,
                CultureInfo.CurrentCulture,
                DateTimeStyles.None,
                out result);
        }


        private static bool ParseBooleanForImport(
            string value,
            string fieldName)
        {
            if (TryParseBoolean(
                    value,
                    out var result))
            {
                return result;
            }


            throw new InvalidOperationException(
                $"{fieldName} contains an invalid boolean value.");
        }


        private static DateTime?
            ParseNullableDateForImport(
                string? value,
                string fieldName)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return null;
            }


            if (TryParseDate(
                    value,
                    out var result))
            {
                return result.Date;
            }


            throw new InvalidOperationException(
                $"{fieldName} contains an invalid date value.");
        }


        private static string?
            NormalizeNullable(
                string? value)
        {
            return string.IsNullOrWhiteSpace(
                value)
                ? null
                : value.Trim();
        }


        private static void AddError(
            ICollection<
                ImportValidationErrorDto>
                previewErrors,

            ICollection<string>
                rowErrors,

            int rowNumber,

            string columnName,

            string message)
        {
            rowErrors.Add(
                message);


            previewErrors.Add(
                new ImportValidationErrorDto
                {
                    RowNumber =
                        rowNumber,

                    ColumnName =
                        columnName,

                    Message =
                        message
                });
        }
    }
}
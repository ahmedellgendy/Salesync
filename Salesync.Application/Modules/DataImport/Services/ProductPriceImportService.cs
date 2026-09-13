using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.DataImport.Dtos;
using Salesync.Application.Modules.DataImport.Dtos.ProductPrices;
using Salesync.Application.Modules.DataImport.Interfaces;
using Salesync.Domain.Modules.DataImport.Entities;
using Salesync.Domain.Modules.DataImport.Enums;
using Salesync.Domain.Modules.MasterData.Entities;
using System.Globalization;
using System.Text.Json;

namespace Salesync.Application.Modules.DataImport.Services
{
    public class ProductPriceImportService :
        IProductPriceImportService
    {
        private static readonly string[] Headers =
        {
            "PriceListCode",
            "ItemCode",
            "UnitPrice",
            "DiscountPercentage",
            "ValidFrom",
            "ValidTo"
        };

        private readonly IUnitOfWork _unitOfWork;
        private readonly IExcelImportService _excelImportService;

        public ProductPriceImportService(
            IUnitOfWork unitOfWork,
            IExcelImportService excelImportService)
        {
            _unitOfWork = unitOfWork;
            _excelImportService = excelImportService;
        }

        // =====================================================
        // TEMPLATE
        // =====================================================

        public byte[] GenerateTemplate()
        {
            return _excelImportService.GenerateTemplate(
                "Product Prices",
                Headers);
        }

        // =====================================================
        // VALIDATE
        // =====================================================

        public async Task<ImportPreviewDto> ValidateAsync(
            Stream fileStream,
            string fileName,
            string? userId,
            CancellationToken cancellationToken = default)
        {
            if (fileStream is null)
                throw new ArgumentNullException(nameof(fileStream));

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException(
                    "File name is required.",
                    nameof(fileName));
            }

            var rows =
                await _excelImportService.ReadAsync(
                    fileStream,
                    cancellationToken);

            var batch =
                new ImportBatch
                {
                    ImportType =
                        ImportType.ProductPrices,

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

            await _unitOfWork.CompleteAsync();

            if (rows.Count == 0)
            {
                batch.Status =
                    ImportStatus.ValidationFailed;

                batch.ValidRows = 0;
                batch.ErrorRows = 1;
                batch.ValidatedAt = DateTime.UtcNow;

                await _unitOfWork.CompleteAsync();

                return new ImportPreviewDto
                {
                    BatchId = batch.Id,
                    ImportType =
                        ImportType.ProductPrices,
                    FileName = fileName,
                    TotalRows = 0,
                    ValidRows = 0,
                    ErrorRows = 1,
                    Errors =
                    {
                        new ImportValidationErrorDto
                        {
                            RowNumber = 0,
                            Message =
                                "The Excel file does not contain any data rows."
                        }
                    }
                };
            }

            ValidateHeaders(rows);

            // =================================================
            // REFERENCES
            // =================================================

            var priceLists =
                (await _unitOfWork.PriceLists
                    .GetAllAsync())
                .Where(x => x.IsActive)
                .ToList();

            var priceListByCode =
                priceLists
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(
                            x.Code))
                    .GroupBy(
                        x => x.Code.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        x => x.Key,
                        x => x.First(),
                        StringComparer.OrdinalIgnoreCase);

            var products =
                (await _unitOfWork.Products
                    .GetAllAsync())
                .Where(x => x.IsActive)
                .ToList();

            var productByCode =
                products
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(
                            x.ItemCode))
                    .GroupBy(
                        x => x.ItemCode.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        x => x.Key,
                        x => x.First(),
                        StringComparer.OrdinalIgnoreCase);

            var existingProductPrices =
                (await _unitOfWork.ProductPrices
                    .GetAllAsync())
                .ToList();

            var existingPairs =
                existingProductPrices
                    .Select(x =>
                        $"{x.PriceListId}:{x.ProductId}")
                    .ToHashSet(
                        StringComparer.OrdinalIgnoreCase);

            var excelPairs =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            var previewErrors =
                new List<ImportValidationErrorDto>();

            var validRows = 0;

            foreach (var row in rows)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                var rowNumber =
                    GetRowNumber(row);

                var productPriceRow =
                    MapRow(
                        row,
                        rowNumber);

                var rowErrors =
                    new List<string>();

                int? priceListId = null;
                int? productId = null;

                // =============================================
                // PRICE LIST
                // =============================================

                if (string.IsNullOrWhiteSpace(
                        productPriceRow.PriceListCode))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "PriceListCode",
                        "Price list code is required.");
                }
                else if (!priceListByCode
                    .TryGetValue(
                        productPriceRow
                            .PriceListCode
                            .Trim(),
                        out var priceList))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "PriceListCode",
                        $"Price list code '{productPriceRow.PriceListCode}' does not exist or is inactive.");
                }
                else
                {
                    priceListId =
                        priceList.Id;
                }

                // =============================================
                // PRODUCT
                // =============================================

                if (string.IsNullOrWhiteSpace(
                        productPriceRow.ItemCode))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "ItemCode",
                        "Item code is required.");
                }
                else if (!productByCode
                    .TryGetValue(
                        productPriceRow
                            .ItemCode
                            .Trim(),
                        out var product))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "ItemCode",
                        $"Item code '{productPriceRow.ItemCode}' does not exist or is inactive.");
                }
                else
                {
                    productId =
                        product.Id;
                }

                // =============================================
                // UNIT PRICE
                // =============================================

                decimal unitPrice = 0;

                if (string.IsNullOrWhiteSpace(
                        productPriceRow.UnitPrice))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "UnitPrice",
                        "Unit price is required.");
                }
                else if (!TryParseDecimal(
                    productPriceRow.UnitPrice,
                    out unitPrice))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "UnitPrice",
                        "Unit price must be a valid number.");
                }
                else if (unitPrice <= 0)
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "UnitPrice",
                        "Unit price must be greater than zero.");
                }

                // =============================================
                // DISCOUNT
                // =============================================

                decimal discountPercentage = 0;

                if (!TryParseDecimal(
                    productPriceRow
                        .DiscountPercentage,
                    out discountPercentage))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "DiscountPercentage",
                        "Discount percentage must be a valid number.");
                }
                else if (discountPercentage < 0 ||
                         discountPercentage > 100)
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "DiscountPercentage",
                        "Discount percentage must be between 0 and 100.");
                }

                // =============================================
                // DATES
                // =============================================

                DateTime? validFrom = null;
                DateTime? validTo = null;

                if (!string.IsNullOrWhiteSpace(
                        productPriceRow.ValidFrom))
                {
                    if (!TryParseDate(
                        productPriceRow.ValidFrom,
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

                if (!string.IsNullOrWhiteSpace(
                        productPriceRow.ValidTo))
                {
                    if (!TryParseDate(
                        productPriceRow.ValidTo,
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
                // DUPLICATE PAIR
                // =============================================

                if (priceListId.HasValue &&
                    productId.HasValue)
                {
                    var key =
                        $"{priceListId.Value}:{productId.Value}";

                    if (!excelPairs.Add(key))
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "ItemCode",
                            "Duplicate product price inside the Excel file.");
                    }

                    if (existingPairs.Contains(key))
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "ItemCode",
                            "This product already has a price in the selected price list.");
                    }
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
                                productPriceRow),

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
                    validRows++;
            }

            batch.ValidRows =
                validRows;

            batch.ErrorRows =
                rows.Count - validRows;

            batch.ValidatedAt =
                DateTime.UtcNow;

            batch.Status =
                batch.ErrorRows == 0
                    ? ImportStatus.ReadyToImport
                    : ImportStatus.ValidationFailed;

            await _unitOfWork.CompleteAsync();

            return new ImportPreviewDto
            {
                BatchId =
                    batch.Id,

                ImportType =
                    ImportType.ProductPrices,

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

        public async Task<ImportResultDto> ImportAsync(
            int batchId,
            CancellationToken cancellationToken = default)
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
                ImportType.ProductPrices)
            {
                throw new InvalidOperationException(
                    "The selected batch is not a product price import.");
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

            var priceLists =
                (await _unitOfWork.PriceLists
                    .GetAllAsync())
                .Where(x => x.IsActive)
                .ToList();

            var priceListByCode =
                priceLists
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(
                            x.Code))
                    .GroupBy(
                        x => x.Code.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        x => x.Key,
                        x => x.First(),
                        StringComparer.OrdinalIgnoreCase);

            var products =
                (await _unitOfWork.Products
                    .GetAllAsync())
                .Where(x => x.IsActive)
                .ToList();

            var productByCode =
                products
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(
                            x.ItemCode))
                    .GroupBy(
                        x => x.ItemCode.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        x => x.Key,
                        x => x.First(),
                        StringComparer.OrdinalIgnoreCase);

            var currentPairs =
                (await _unitOfWork.ProductPrices
                    .GetAllAsync())
                .Select(x =>
                    $"{x.PriceListId}:{x.ProductId}")
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

                var importedRows = 0;

                foreach (var stagingRow in
                         stagingRows)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    var dto =
                        JsonSerializer.Deserialize<
                            ProductPriceImportRowDto>(
                            stagingRow.DataJson);

                    if (dto is null)
                    {
                        throw new InvalidOperationException(
                            $"Unable to read import row {stagingRow.RowNumber}.");
                    }

                    if (!priceListByCode.TryGetValue(
                            dto.PriceListCode.Trim(),
                            out var priceList))
                    {
                        throw new InvalidOperationException(
                            $"Price list code '{dto.PriceListCode}' does not exist or is inactive.");
                    }

                    if (!productByCode.TryGetValue(
                            dto.ItemCode.Trim(),
                            out var product))
                    {
                        throw new InvalidOperationException(
                            $"Item code '{dto.ItemCode}' does not exist or is inactive.");
                    }

                    var key =
                        $"{priceList.Id}:{product.Id}";

                    if (currentPairs.Contains(key))
                    {
                        throw new InvalidOperationException(
                            $"Product '{dto.ItemCode}' already has a price in price list '{dto.PriceListCode}'.");
                    }

                    var unitPrice =
                        ParseDecimalForImport(
                            dto.UnitPrice,
                            "UnitPrice");

                    var discountPercentage =
                        ParseDecimalForImport(
                            dto.DiscountPercentage,
                            "DiscountPercentage");

                    if (unitPrice <= 0)
                    {
                        throw new InvalidOperationException(
                            $"UnitPrice must be greater than zero for item '{dto.ItemCode}'.");
                    }

                    if (discountPercentage < 0 ||
                        discountPercentage > 100)
                    {
                        throw new InvalidOperationException(
                            $"DiscountPercentage must be between 0 and 100 for item '{dto.ItemCode}'.");
                    }

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
                            $"Product price '{dto.PriceListCode}/{dto.ItemCode}' has an invalid validity period.");
                    }

                    var productPrice =
                        new ProductPrice
                        {
                            PriceListId =
                                priceList.Id,

                            ProductId =
                                product.Id,

                            UnitPrice =
                                unitPrice,

                            DiscountPercentage =
                                discountPercentage,

                            ValidFrom =
                                validFrom,

                            ValidTo =
                                validTo,

                            IsActive =
                                true,

                            CreatedAt =
                                DateTime.UtcNow
                        };

                    await _unitOfWork.ProductPrices
                        .AddAsync(productPrice);

                    currentPairs.Add(key);

                    importedRows++;
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
                        $"{importedRows} product prices imported successfully."
                };
            }
            catch
            {
                await _unitOfWork
                    .RollbackTransactionAsync();

                var failedBatch =
                    await _unitOfWork.ImportBatches
                        .GetByIdAsync(batchId);

                if (failedBatch is not null)
                {
                    failedBatch.Status =
                        ImportStatus.Failed;

                    failedBatch.Notes =
                        "Product price import failed during database commit.";

                    await _unitOfWork
                        .CompleteAsync();
                }

                throw;
            }
        }

        // =====================================================
        // ERROR REPORT
        // =====================================================

        public async Task<byte[]> GenerateErrorReportAsync(
            int batchId,
            CancellationToken cancellationToken = default)
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
                ImportType.ProductPrices)
            {
                throw new InvalidOperationException(
                    "The selected batch is not a product price import.");
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
                new Dictionary<int, List<string>>();

            foreach (var stagingRow in stagingRows)
            {
                var dto =
                    JsonSerializer.Deserialize<
                        ProductPriceImportRowDto>(
                        stagingRow.DataJson);

                if (dto is null)
                    continue;

                rows.Add(
                    new Dictionary<string, string>(
                        StringComparer.OrdinalIgnoreCase)
                    {
                        ["PriceListCode"] =
                            dto.PriceListCode,

                        ["ItemCode"] =
                            dto.ItemCode,

                        ["UnitPrice"] =
                            dto.UnitPrice,

                        ["DiscountPercentage"] =
                            dto.DiscountPercentage,

                        ["ValidFrom"] =
                            dto.ValidFrom ??
                            string.Empty,

                        ["ValidTo"] =
                            dto.ValidTo ??
                            string.Empty,

                        ["__RowNumber"] =
                            dto.RowNumber.ToString()
                    });

                if (!string.IsNullOrWhiteSpace(
                        stagingRow.ErrorsJson))
                {
                    errors[stagingRow.RowNumber] =
                        JsonSerializer.Deserialize<
                            List<string>>(
                            stagingRow.ErrorsJson)
                        ??
                        new List<string>();
                }
            }

            return _excelImportService
                .GenerateErrorReport(
                    "Product Price Errors",
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
                CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            var batches =
                await _unitOfWork.ImportBatches
                    .GetAllAsync();

            return batches
                .Where(x =>
                    x.ImportType ==
                    ImportType.ProductPrices)
                .OrderByDescending(x =>
                    x.CreatedAt)
                .Select(x =>
                    new ImportBatchHistoryDto
                    {
                        Id = x.Id,
                        ImportType = x.ImportType,
                        Status = x.Status,
                        FileName = x.FileName,
                        TotalRows = x.TotalRows,
                        ValidRows = x.ValidRows,
                        ErrorRows = x.ErrorRows,
                        ImportedRows = x.ImportedRows,
                        ImportedByUserId =
                            x.ImportedByUserId,
                        CreatedAt = x.CreatedAt,
                        CompletedAt = x.CompletedAt
                    })
                .ToList();
        }

        // =====================================================
        // HELPERS
        // =====================================================

        private static void ValidateHeaders(
            IReadOnlyList<
                Dictionary<string, string>> rows)
        {
            if (rows.Count == 0)
                return;

            var firstRow =
                rows[0];

            var missingHeaders =
                Headers
                    .Where(header =>
                        !firstRow.ContainsKey(header))
                    .ToList();

            if (missingHeaders.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Missing required Excel headers: {string.Join(", ", missingHeaders)}");
            }
        }

        private static ProductPriceImportRowDto MapRow(
            IReadOnlyDictionary<
                string,
                string> row,
            int rowNumber)
        {
            return new ProductPriceImportRowDto
            {
                RowNumber =
                    rowNumber,

                PriceListCode =
                    GetValue(
                        row,
                        "PriceListCode"),

                ItemCode =
                    GetValue(
                        row,
                        "ItemCode"),

                UnitPrice =
                    GetValue(
                        row,
                        "UnitPrice"),

                DiscountPercentage =
                    GetValueOrDefault(
                        row,
                        "DiscountPercentage",
                        "0"),

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

        private static bool TryParseDecimal(
            string value,
            out decimal result)
        {
            if (decimal.TryParse(
                    value,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out result))
            {
                return true;
            }

            return decimal.TryParse(
                value,
                NumberStyles.Any,
                CultureInfo.CurrentCulture,
                out result);
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

        private static decimal ParseDecimalForImport(
            string value,
            string fieldName)
        {
            if (TryParseDecimal(
                    value,
                    out var result))
            {
                return result;
            }

            throw new InvalidOperationException(
                $"{fieldName} contains an invalid decimal value.");
        }

        private static DateTime?
            ParseNullableDateForImport(
                string? value,
                string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (TryParseDate(
                    value,
                    out var result))
            {
                return result.Date;
            }

            throw new InvalidOperationException(
                $"{fieldName} contains an invalid date value.");
        }

        private static string? NormalizeNullable(
            string? value)
        {
            return string.IsNullOrWhiteSpace(
                value)
                ? null
                : value.Trim();
        }

        private static void AddError(
            ICollection<ImportValidationErrorDto>
                previewErrors,
            ICollection<string>
                rowErrors,
            int rowNumber,
            string columnName,
            string message)
        {
            rowErrors.Add(message);

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
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.DataImport.Dtos;
using Salesync.Application.Modules.DataImport.Dtos.OpeningInventory;
using Salesync.Application.Modules.DataImport.Interfaces;
using Salesync.Application.Modules.Inventory.Dtos;
using Salesync.Application.Modules.Inventory.Interfaces;
using Salesync.Domain.Modules.DataImport.Entities;
using Salesync.Domain.Modules.DataImport.Enums;
using System.Text.Json;

namespace Salesync.Application.Modules.DataImport.Services
{
    public class OpeningInventoryImportService
        : IOpeningInventoryImportService
    {
        private static readonly string[] Headers =
        {
            "WarehouseCode",
            "ItemCode",
            "LargeQuantity",
            "SmallQuantity",
            "Notes"
        };

        private readonly IUnitOfWork _unitOfWork;
        private readonly IExcelImportService _excelImportService;
        private readonly IInventoryService _inventoryService;

        public OpeningInventoryImportService(
            IUnitOfWork unitOfWork,
            IExcelImportService excelImportService,
            IInventoryService inventoryService)
        {
            _unitOfWork = unitOfWork;
            _excelImportService = excelImportService;
            _inventoryService = inventoryService;
        }

        // =====================================================
        // TEMPLATE
        // =====================================================

        public byte[] GenerateTemplate()
        {
            return _excelImportService.GenerateTemplate(
                "Opening Inventory",
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
                        ImportType.OpeningInventory,

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
                batch.ValidatedAt =
                    DateTime.UtcNow;

                await _unitOfWork.CompleteAsync();

                return new ImportPreviewDto
                {
                    BatchId = batch.Id,
                    ImportType = ImportType.OpeningInventory,
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

            var products =
                (await _unitOfWork.Products.GetAllAsync())
                .Where(x => x.IsActive)
                .ToList();

            var productByItemCode =
                products
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.ItemCode))
                    .GroupBy(
                        x => x.ItemCode.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        x => x.Key,
                        x => x.First(),
                        StringComparer.OrdinalIgnoreCase);

            var warehouses =
                (await _unitOfWork.Warehouses.GetAllAsync())
                .Where(x => x.IsActive)
                .ToList();

            var warehouseByCode =
                warehouses
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.WarehouseCode))
                    .GroupBy(
                        x => x.WarehouseCode.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        x => x.Key,
                        x => x.First(),
                        StringComparer.OrdinalIgnoreCase);

            var existingBalances =
                await _unitOfWork.StockBalances
                    .GetQueryable()
                    .Select(x => new
                    {
                        x.ProductId,
                        x.WarehouseId
                    })
                    .ToListAsync(cancellationToken);

            var existingBalanceKeys =
                existingBalances
                    .Select(x =>
                        $"{x.WarehouseId}:{x.ProductId}")
                    .ToHashSet(
                        StringComparer.OrdinalIgnoreCase);

            var excelKeys =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            var previewErrors =
                new List<ImportValidationErrorDto>();

            var validRows = 0;

            // =================================================
            // ROW VALIDATION
            // =================================================

            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var rowNumber =
                    GetRowNumber(row);

                var importRow =
                    MapRow(
                        row,
                        rowNumber);

                var rowErrors =
                    new List<string>();

                // ---------------------------------------------
                // Warehouse
                // ---------------------------------------------

                int? warehouseId = null;

                if (string.IsNullOrWhiteSpace(
                        importRow.WarehouseCode))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "WarehouseCode",
                        "Warehouse code is required.");
                }
                else if (!warehouseByCode.TryGetValue(
                             importRow.WarehouseCode,
                             out var warehouse))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "WarehouseCode",
                        $"Warehouse code '{importRow.WarehouseCode}' does not exist or is inactive.");
                }
                else
                {
                    warehouseId =
                        warehouse.Id;
                }

                // ---------------------------------------------
                // Product
                // ---------------------------------------------

                int? productId = null;
                int unitsPerLargeUnit = 1;

                if (string.IsNullOrWhiteSpace(
                        importRow.ItemCode))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "ItemCode",
                        "Item code is required.");
                }
                else if (!productByItemCode.TryGetValue(
                             importRow.ItemCode,
                             out var product))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "ItemCode",
                        $"Item code '{importRow.ItemCode}' does not exist or is inactive.");
                }
                else
                {
                    productId =
                        product.Id;

                    unitsPerLargeUnit =
                        product.UnitsPerLargeUnit;
                }

                // ---------------------------------------------
                // Quantities
                // ---------------------------------------------

                var largeQuantity = 0;
                var smallQuantity = 0;

                if (!TryParseNonNegativeInt(
                        importRow.LargeQuantity,
                        out largeQuantity))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "LargeQuantity",
                        "Large quantity must be a whole number greater than or equal to 0.");
                }

                if (!TryParseNonNegativeInt(
                        importRow.SmallQuantity,
                        out smallQuantity))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "SmallQuantity",
                        "Small quantity must be a whole number greater than or equal to 0.");
                }

                if (productId.HasValue &&
                     unitsPerLargeUnit > 1 &&
                     smallQuantity >= unitsPerLargeUnit)
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "SmallQuantity",
                        $"Small quantity must be less than {unitsPerLargeUnit} for this product.");
                }

                if (productId.HasValue)
                {
                    var totalQuantity =
                        checked(
                            (largeQuantity * unitsPerLargeUnit)
                            + smallQuantity);

                    if (totalQuantity <= 0)
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "LargeQuantity",
                            "Opening inventory quantity must be greater than 0.");
                    }
                }

                // ---------------------------------------------
                // Notes
                // ---------------------------------------------

                if (!string.IsNullOrWhiteSpace(
                        importRow.Notes) &&
                    importRow.Notes.Length > 500)
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "Notes",
                        "Notes cannot exceed 500 characters.");
                }

                // ---------------------------------------------
                // Duplicate pair
                // ---------------------------------------------

                if (warehouseId.HasValue &&
                    productId.HasValue)
                {
                    var key =
                        $"{warehouseId.Value}:{productId.Value}";

                    if (!excelKeys.Add(key))
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "ItemCode",
                            "Duplicate warehouse/product combination inside the Excel file.");
                    }

                    if (existingBalanceKeys.Contains(key))
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "ItemCode",
                            "Opening balance already exists for this product in this warehouse.");
                    }
                }

                // ---------------------------------------------
                // STAGING
                // ---------------------------------------------

                var stagingRow =
                    new ImportBatchRow
                    {
                        ImportBatchId =
                            batch.Id,

                        RowNumber =
                            rowNumber,

                        DataJson =
                            JsonSerializer.Serialize(
                                importRow),

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

            // =================================================
            // FINISH VALIDATION
            // =================================================

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
                    ImportType.OpeningInventory,

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
                ImportType.OpeningInventory)
            {
                throw new InvalidOperationException(
                    "The selected batch is not an opening inventory import.");
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

            var products =
                (await _unitOfWork.Products.GetAllAsync())
                .Where(x => x.IsActive)
                .ToList();

            var productByItemCode =
                products
                    .GroupBy(
                        x => x.ItemCode.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        x => x.Key,
                        x => x.First(),
                        StringComparer.OrdinalIgnoreCase);

            var warehouses =
                (await _unitOfWork.Warehouses.GetAllAsync())
                .Where(x => x.IsActive)
                .ToList();

            var warehouseByCode =
                warehouses
                    .GroupBy(
                        x => x.WarehouseCode.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        x => x.Key,
                        x => x.First(),
                        StringComparer.OrdinalIgnoreCase);

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                batch.Status =
                    ImportStatus.Importing;

                await _unitOfWork.CompleteAsync();

                var importedRows = 0;

                foreach (var stagingRow in stagingRows)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var dto =
                        JsonSerializer.Deserialize<
                            OpeningInventoryImportRowDto>(
                            stagingRow.DataJson);

                    if (dto is null)
                    {
                        throw new InvalidOperationException(
                            $"Unable to read import row {stagingRow.RowNumber}.");
                    }

                    if (!warehouseByCode.TryGetValue(
                            dto.WarehouseCode,
                            out var warehouse))
                    {
                        throw new InvalidOperationException(
                            $"Warehouse code '{dto.WarehouseCode}' does not exist or is inactive.");
                    }

                    if (!productByItemCode.TryGetValue(
                            dto.ItemCode,
                            out var product))
                    {
                        throw new InvalidOperationException(
                            $"Item code '{dto.ItemCode}' does not exist or is inactive.");
                    }

                    var largeQuantity =
                        ParseNonNegativeIntForImport(
                            dto.LargeQuantity,
                            "LargeQuantity");

                    var smallQuantity =
                        ParseNonNegativeIntForImport(
                            dto.SmallQuantity,
                            "SmallQuantity");

                    if (product.UnitsPerLargeUnit > 1 &&
                         smallQuantity >= product.UnitsPerLargeUnit)
                    {
                        throw new InvalidOperationException(
                            $"Small quantity for '{dto.ItemCode}' must be less than {product.UnitsPerLargeUnit}.");
                    }

                    var totalQuantity =
                        checked(
                            (largeQuantity *
                             product.UnitsPerLargeUnit)
                            +
                            smallQuantity);

                    if (totalQuantity <= 0)
                    {
                        throw new InvalidOperationException(
                            $"Opening inventory quantity for '{dto.ItemCode}' must be greater than 0.");
                    }

                    var openingBalanceDto =
                        new CreateOpeningBalanceDto
                        {
                            ProductId =
                                product.Id,

                            WarehouseId =
                                warehouse.Id,

                            Quantity =
                                totalQuantity,

                            Notes =
                                NormalizeNullable(
                                    dto.Notes)
                        };

                    await _inventoryService
                        .AddOpeningBalanceAsync(
                            openingBalanceDto,
                            saveChanges: false);

                    importedRows++;
                }

                batch.ImportedRows =
                    importedRows;

                batch.Status =
                    ImportStatus.Completed;

                batch.CompletedAt =
                    DateTime.UtcNow;

                await _unitOfWork.CompleteAsync();

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
                        $"{importedRows} opening inventory rows imported successfully."
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
                        "Opening inventory import failed.";

                    await _unitOfWork.CompleteAsync();
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
                ImportType.OpeningInventory)
            {
                throw new InvalidOperationException(
                    "The selected batch is not an opening inventory import.");
            }

            var stagingRows =
                (await _unitOfWork.ImportBatchRows
                    .FindAsync(x =>
                        x.ImportBatchId == batchId))
                .OrderBy(x =>
                    x.RowNumber)
                .ToList();

            var rows =
                new List<Dictionary<string, string>>();

            var errors =
                new Dictionary<int, List<string>>();

            foreach (var stagingRow in stagingRows)
            {
                var dto =
                    JsonSerializer.Deserialize<
                        OpeningInventoryImportRowDto>(
                        stagingRow.DataJson);

                if (dto is null)
                    continue;

                rows.Add(
                    new Dictionary<string, string>(
                        StringComparer.OrdinalIgnoreCase)
                    {
                        ["WarehouseCode"] =
                            dto.WarehouseCode,

                        ["ItemCode"] =
                            dto.ItemCode,

                        ["LargeQuantity"] =
                            dto.LargeQuantity,

                        ["SmallQuantity"] =
                            dto.SmallQuantity,

                        ["Notes"] =
                            dto.Notes
                            ?? string.Empty,

                        ["__RowNumber"] =
                            dto.RowNumber.ToString()
                    });

                if (!string.IsNullOrWhiteSpace(
                        stagingRow.ErrorsJson))
                {
                    errors[stagingRow.RowNumber] =
                        JsonSerializer.Deserialize<List<string>>(
                            stagingRow.ErrorsJson)
                        ?? new List<string>();
                }
            }

            return _excelImportService.GenerateErrorReport(
                "Opening Inventory Errors",
                Headers,
                rows,
                errors);
        }

        // =====================================================
        // HISTORY
        // =====================================================

        public async Task<IEnumerable<ImportBatchHistoryDto>>
            GetHistoryAsync(
                CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batches =
                await _unitOfWork.ImportBatches
                    .GetAllAsync();

            return batches
                .Where(x =>
                    x.ImportType ==
                    ImportType.OpeningInventory)
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
                        ImportedByUserId = x.ImportedByUserId,
                        CreatedAt = x.CreatedAt,
                        CompletedAt = x.CompletedAt
                    })
                .ToList();
        }

        // =====================================================
        // HELPERS
        // =====================================================

        private static void ValidateHeaders(
            IReadOnlyList<Dictionary<string, string>> rows)
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

        private static OpeningInventoryImportRowDto MapRow(
            IReadOnlyDictionary<string, string> row,
            int rowNumber)
        {
            return new OpeningInventoryImportRowDto
            {
                RowNumber =
                    rowNumber,

                WarehouseCode =
                    GetValue(
                        row,
                        "WarehouseCode"),

                ItemCode =
                    GetValue(
                        row,
                        "ItemCode"),

                LargeQuantity =
                    GetValueOrDefault(
                        row,
                        "LargeQuantity",
                        "0"),

                SmallQuantity =
                    GetValueOrDefault(
                        row,
                        "SmallQuantity",
                        "0"),

                Notes =
                    NormalizeNullable(
                        GetValue(
                            row,
                            "Notes"))
            };
        }

        private static bool TryParseNonNegativeInt(
            string value,
            out int result)
        {
            return int.TryParse(
                       value,
                       out result)
                   &&
                   result >= 0;
        }

        private static int ParseNonNegativeIntForImport(
            string value,
            string fieldName)
        {
            if (TryParseNonNegativeInt(
                    value,
                    out var result))
            {
                return result;
            }

            throw new InvalidOperationException(
                $"{fieldName} must be a whole number greater than or equal to 0.");
        }

        private static int GetRowNumber(
            IReadOnlyDictionary<string, string> row)
        {
            if (!row.TryGetValue(
                    "__RowNumber",
                    out var value) ||
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
            IReadOnlyDictionary<string, string> row,
            string key)
        {
            return row.TryGetValue(
                key,
                out var value)
                ? value.Trim()
                : string.Empty;
        }

        private static string GetValueOrDefault(
            IReadOnlyDictionary<string, string> row,
            string key,
            string defaultValue)
        {
            var value =
                GetValue(
                    row,
                    key);

            return string.IsNullOrWhiteSpace(value)
                ? defaultValue
                : value;
        }

        private static string? NormalizeNullable(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static void AddError(
            ICollection<ImportValidationErrorDto> previewErrors,
            ICollection<string> rowErrors,
            int rowNumber,
            string columnName,
            string message)
        {
            rowErrors.Add(message);

            previewErrors.Add(
                new ImportValidationErrorDto
                {
                    RowNumber = rowNumber,
                    ColumnName = columnName,
                    Message = message
                });
        }
    }
}
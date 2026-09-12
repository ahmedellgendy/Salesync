using FluentValidation;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.DataImport.Dtos;
using Salesync.Application.Modules.DataImport.Dtos.Products;
using Salesync.Application.Modules.DataImport.Interfaces;
using Salesync.Application.Modules.MasterData.Dtos.ProductDto;
using Salesync.Domain.Modules.DataImport.Entities;
using Salesync.Domain.Modules.DataImport.Enums;
using Salesync.Domain.Modules.MasterData.Entities;
using System.Globalization;
using System.Text.Json;

namespace Salesync.Application.Modules.DataImport.Services
{
    public class ProductImportService : IProductImportService
    {
        private static readonly string[] Headers =
        {
            "ItemCode",
            "Name",
            "Description",
            "SKU",
            "Barcode",
            "UnitPrice",
            "CostPrice",
            "DiscountPercentage",
            "Unit",
            "SmallUnit",
            "LargeUnit",
            "UnitsPerLargeUnit",
            "MinStockLevel",
            "MaxStockLevel",
            "EnableReturn",
            "ReturnDamaged",
            "ReturnPeriod",
            "WarehouseCode"
        };

        private readonly IUnitOfWork _unitOfWork;
        private readonly IExcelImportService _excelImportService;
        private readonly IValidator<CreateProductDto> _productValidator;

        public ProductImportService(
            IUnitOfWork unitOfWork,
            IExcelImportService excelImportService,
            IValidator<CreateProductDto> productValidator)
        {
            _unitOfWork = unitOfWork;
            _excelImportService = excelImportService;
            _productValidator = productValidator;
        }

        // =====================================================
        // TEMPLATE
        // =====================================================

        public byte[] GenerateTemplate()
        {
            return _excelImportService.GenerateTemplate(
                "Products",
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
                    ImportType = ImportType.Products,
                    Status = ImportStatus.Uploaded,
                    FileName = fileName,
                    ImportedByUserId = userId,
                    TotalRows = rows.Count,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

            await _unitOfWork.ImportBatches.AddAsync(batch);
            await _unitOfWork.CompleteAsync();

            if (rows.Count == 0)
            {
                batch.Status = ImportStatus.ValidationFailed;
                batch.ValidRows = 0;
                batch.ErrorRows = 1;
                batch.ValidatedAt = DateTime.UtcNow;

                await _unitOfWork.CompleteAsync();

                return new ImportPreviewDto
                {
                    BatchId = batch.Id,
                    ImportType = ImportType.Products,
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

            var products =
                (await _unitOfWork.Products.GetAllAsync())
                .ToList();

            var existingItemCodes =
                products
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.ItemCode))
                    .Select(x =>
                        x.ItemCode.Trim())
                    .ToHashSet(
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

            var excelItemCodes =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            var previewErrors =
                new List<ImportValidationErrorDto>();

            var validRows = 0;

            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var rowNumber =
                    GetRowNumber(row);

                var productRow =
                    MapRow(
                        row,
                        rowNumber);

                var rowErrors =
                    new List<string>();

                // =============================================
                // Parse values
                // =============================================

                decimal unitPrice = 0;
                decimal costPrice = 0;

                decimal? discountPercentage = null;

                int unitsPerLargeUnit = 1;
                int minStockLevel = 0;
                int maxStockLevel = 0;

                int? returnPeriod = null;

                bool enableReturn = true;
                bool returnDamaged = false;

                // ---------------------------------------------
                // UnitPrice
                // ---------------------------------------------

                if (string.IsNullOrWhiteSpace(
                        productRow.UnitPrice))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "UnitPrice",
                        "Unit price is required.");
                }
                else if (!TryParseDecimal(
                             productRow.UnitPrice,
                             out unitPrice))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "UnitPrice",
                        "Unit price must be a valid number.");
                }

                // ---------------------------------------------
                // CostPrice
                // ---------------------------------------------

                if (string.IsNullOrWhiteSpace(
                        productRow.CostPrice))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "CostPrice",
                        "Cost price is required.");
                }
                else if (!TryParseDecimal(
                             productRow.CostPrice,
                             out costPrice))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "CostPrice",
                        "Cost price must be a valid number.");
                }

                // ---------------------------------------------
                // DiscountPercentage
                // ---------------------------------------------

                if (!string.IsNullOrWhiteSpace(
                        productRow.DiscountPercentage))
                {
                    if (!TryParseDecimal(
                            productRow.DiscountPercentage,
                            out var parsedDiscount))
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "DiscountPercentage",
                            "Discount percentage must be a valid number.");
                    }
                    else
                    {
                        discountPercentage =
                            parsedDiscount;
                    }
                }

                // ---------------------------------------------
                // UnitsPerLargeUnit
                // ---------------------------------------------

                if (!TryParseInt(
                        productRow.UnitsPerLargeUnit,
                        out unitsPerLargeUnit))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "UnitsPerLargeUnit",
                        "Units per large unit must be a valid whole number.");
                }

                // ---------------------------------------------
                // MinStockLevel
                // ---------------------------------------------

                if (!TryParseInt(
                        productRow.MinStockLevel,
                        out minStockLevel))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "MinStockLevel",
                        "Minimum stock level must be a valid whole number.");
                }

                // ---------------------------------------------
                // MaxStockLevel
                // ---------------------------------------------

                if (!TryParseInt(
                        productRow.MaxStockLevel,
                        out maxStockLevel))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "MaxStockLevel",
                        "Maximum stock level must be a valid whole number.");
                }

                // ---------------------------------------------
                // EnableReturn
                // ---------------------------------------------

                if (!TryParseBoolean(
                        productRow.EnableReturn,
                        out enableReturn))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "EnableReturn",
                        "EnableReturn must be True/False, Yes/No, or 1/0.");
                }

                // ---------------------------------------------
                // ReturnDamaged
                // ---------------------------------------------

                if (!TryParseBoolean(
                        productRow.ReturnDamaged,
                        out returnDamaged))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "ReturnDamaged",
                        "ReturnDamaged must be True/False, Yes/No, or 1/0.");
                }

                // ---------------------------------------------
                // ReturnPeriod
                // ---------------------------------------------

                if (!string.IsNullOrWhiteSpace(
                        productRow.ReturnPeriod))
                {
                    if (!TryParseInt(
                            productRow.ReturnPeriod,
                            out var parsedReturnPeriod))
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "ReturnPeriod",
                            "Return period must be a valid whole number.");
                    }
                    else
                    {
                        returnPeriod =
                            parsedReturnPeriod;
                    }
                }

                // =============================================
                // WarehouseCode -> WarehouseId
                // =============================================

                int? warehouseId = null;

                if (!string.IsNullOrWhiteSpace(
                        productRow.WarehouseCode))
                {
                    if (!warehouseByCode.TryGetValue(
                            productRow.WarehouseCode,
                            out var warehouse))
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "WarehouseCode",
                            $"Warehouse code '{productRow.WarehouseCode}' does not exist or is inactive.");
                    }
                    else
                    {
                        warehouseId =
                            warehouse.Id;
                    }
                }

                // =============================================
                // Existing Product Validator
                // =============================================

                var createDto =
                    new CreateProductDto
                    {
                        ItemCode =
                            productRow.ItemCode,

                        Name =
                            productRow.Name,

                        Description =
                            productRow.Description,

                        SKU =
                            productRow.SKU,

                        Barcode =
                            productRow.Barcode,

                        UnitPrice =
                            unitPrice,

                        CostPrice =
                            costPrice,

                        DiscountPercentage =
                            discountPercentage,

                        Unit =
                            productRow.Unit,

                        SmallUnit =
                            productRow.SmallUnit,

                        LargeUnit =
                            productRow.LargeUnit,

                        UnitsPerLargeUnit =
                            unitsPerLargeUnit,

                        MinStockLevel =
                            minStockLevel,

                        MaxStockLevel =
                            maxStockLevel,

                        EnableReturn =
                            enableReturn,

                        ReturnDamaged =
                            returnDamaged,

                        ReturnPeriod =
                            returnPeriod,

                        WarehouseId =
                            warehouseId
                    };

                var validationResult =
                    await _productValidator.ValidateAsync(
                        createDto,
                        cancellationToken);

                foreach (var error in validationResult.Errors)
                {
                    AddErrorIfMissing(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        error.PropertyName,
                        error.ErrorMessage);
                }

                // =============================================
                // ItemCode duplicate
                // =============================================

                if (!string.IsNullOrWhiteSpace(
                        productRow.ItemCode))
                {
                    if (!excelItemCodes.Add(
                            productRow.ItemCode))
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "ItemCode",
                            "Duplicate item code inside the Excel file.");
                    }

                    if (existingItemCodes.Contains(
                            productRow.ItemCode))
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "ItemCode",
                            "Item code already exists in the system.");
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
                                productRow),

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
                    ImportType.Products,

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
                ImportType.Products)
            {
                throw new InvalidOperationException(
                    "The selected batch is not a product import.");
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
                        x.ImportBatchId == batchId))
                .OrderBy(x => x.RowNumber)
                .ToList();

            if (stagingRows.Count == 0)
            {
                throw new InvalidOperationException(
                    "The import batch does not contain any rows.");
            }

            if (stagingRows.Any(x => !x.IsValid))
            {
                throw new InvalidOperationException(
                    "The import batch contains validation errors.");
            }

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

            var currentItemCodes =
                (await _unitOfWork.Products.GetAllAsync())
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.ItemCode))
                .Select(x =>
                    x.ItemCode.Trim())
                .ToHashSet(
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
                        JsonSerializer.Deserialize<ProductImportRowDto>(
                            stagingRow.DataJson);

                    if (dto is null)
                    {
                        throw new InvalidOperationException(
                            $"Unable to read import row {stagingRow.RowNumber}.");
                    }

                    if (currentItemCodes.Contains(
                            dto.ItemCode))
                    {
                        throw new InvalidOperationException(
                            $"Item code '{dto.ItemCode}' already exists.");
                    }

                    int? warehouseId = null;

                    if (!string.IsNullOrWhiteSpace(
                            dto.WarehouseCode))
                    {
                        if (!warehouseByCode.TryGetValue(
                                dto.WarehouseCode,
                                out var warehouse))
                        {
                            throw new InvalidOperationException(
                                $"Warehouse code '{dto.WarehouseCode}' does not exist or is inactive.");
                        }

                        warehouseId =
                            warehouse.Id;
                    }

                    var unitPrice =
                        ParseDecimalForImport(
                            dto.UnitPrice,
                            "UnitPrice");

                    var costPrice =
                        ParseDecimalForImport(
                            dto.CostPrice,
                            "CostPrice");

                    var discountPercentage =
                        ParseNullableDecimalForImport(
                            dto.DiscountPercentage,
                            "DiscountPercentage");

                    var unitsPerLargeUnit =
                        ParseIntForImport(
                            dto.UnitsPerLargeUnit,
                            "UnitsPerLargeUnit");

                    var minStockLevel =
                        ParseIntForImport(
                            dto.MinStockLevel,
                            "MinStockLevel");

                    var maxStockLevel =
                        ParseIntForImport(
                            dto.MaxStockLevel,
                            "MaxStockLevel");

                    var enableReturn =
                        ParseBooleanForImport(
                            dto.EnableReturn,
                            "EnableReturn");

                    var returnDamaged =
                        ParseBooleanForImport(
                            dto.ReturnDamaged,
                            "ReturnDamaged");

                    var returnPeriod =
                        ParseNullableIntForImport(
                            dto.ReturnPeriod,
                            "ReturnPeriod");

                    var product =
                        new Product
                        {
                            ItemCode =
                                dto.ItemCode.Trim(),

                            Name =
                                dto.Name.Trim(),

                            Description =
                                NormalizeNullable(
                                    dto.Description),

                            SKU =
                                NormalizeNullable(
                                    dto.SKU),

                            Barcode =
                                NormalizeNullable(
                                    dto.Barcode),

                            UnitPrice =
                                unitPrice,

                            CostPrice =
                                costPrice,

                            DiscountPercentage =
                                discountPercentage,

                            Unit =
                                NormalizeNullable(
                                    dto.Unit),

                            SmallUnit =
                                dto.SmallUnit.Trim(),

                            LargeUnit =
                                dto.LargeUnit.Trim(),

                            UnitsPerLargeUnit =
                                unitsPerLargeUnit,

                            MinStockLevel =
                                minStockLevel,

                            MaxStockLevel =
                                maxStockLevel,

                            EnableReturn =
                                enableReturn,

                            ReturnDamaged =
                                returnDamaged,

                            ReturnPeriod =
                                returnPeriod,

                            WarehouseId =
                                warehouseId,

                            IsActive =
                                true,

                            CreatedAt =
                                DateTime.UtcNow
                        };

                    await _unitOfWork.Products
                        .AddAsync(product);

                    currentItemCodes.Add(
                        product.ItemCode);

                    importedRows++;
                }

                batch.ImportedRows =
                    importedRows;

                batch.Status =
                    ImportStatus.Completed;

                batch.CompletedAt =
                    DateTime.UtcNow;

                await _unitOfWork.CompleteAsync();

                await _unitOfWork.CommitTransactionAsync();

                return new ImportResultDto
                {
                    BatchId =
                        batch.Id,

                    ImportedRows =
                        importedRows,

                    Success =
                        true,

                    Message =
                        $"{importedRows} products imported successfully."
                };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();

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
                ImportType.Products)
            {
                throw new InvalidOperationException(
                    "The selected batch is not a product import.");
            }

            var stagingRows =
                (await _unitOfWork.ImportBatchRows
                    .FindAsync(x =>
                        x.ImportBatchId == batchId))
                .OrderBy(x => x.RowNumber)
                .ToList();

            var rows =
                new List<Dictionary<string, string>>();

            var errors =
                new Dictionary<int, List<string>>();

            foreach (var stagingRow in stagingRows)
            {
                var dto =
                    JsonSerializer.Deserialize<ProductImportRowDto>(
                        stagingRow.DataJson);

                if (dto is null)
                    continue;

                rows.Add(
                    new Dictionary<string, string>(
                        StringComparer.OrdinalIgnoreCase)
                    {
                        ["ItemCode"] = dto.ItemCode,
                        ["Name"] = dto.Name,
                        ["Description"] = dto.Description ?? string.Empty,
                        ["SKU"] = dto.SKU ?? string.Empty,
                        ["Barcode"] = dto.Barcode ?? string.Empty,
                        ["UnitPrice"] = dto.UnitPrice,
                        ["CostPrice"] = dto.CostPrice,
                        ["DiscountPercentage"] =
                            dto.DiscountPercentage ?? string.Empty,
                        ["Unit"] = dto.Unit ?? string.Empty,
                        ["SmallUnit"] = dto.SmallUnit,
                        ["LargeUnit"] = dto.LargeUnit,
                        ["UnitsPerLargeUnit"] =
                            dto.UnitsPerLargeUnit,
                        ["MinStockLevel"] =
                            dto.MinStockLevel,
                        ["MaxStockLevel"] =
                            dto.MaxStockLevel,
                        ["EnableReturn"] =
                            dto.EnableReturn,
                        ["ReturnDamaged"] =
                            dto.ReturnDamaged,
                        ["ReturnPeriod"] =
                            dto.ReturnPeriod ?? string.Empty,
                        ["WarehouseCode"] =
                            dto.WarehouseCode ?? string.Empty,
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
                "Product Errors",
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
                    ImportType.Products)
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

        private static ProductImportRowDto MapRow(
            IReadOnlyDictionary<string, string> row,
            int rowNumber)
        {
            return new ProductImportRowDto
            {
                RowNumber =
                    rowNumber,

                ItemCode =
                    GetValue(
                        row,
                        "ItemCode"),

                Name =
                    GetValue(
                        row,
                        "Name"),

                Description =
                    NormalizeNullable(
                        GetValue(row, "Description")),

                SKU =
                    NormalizeNullable(
                        GetValue(row, "SKU")),

                Barcode =
                    NormalizeNullable(
                        GetValue(row, "Barcode")),

                UnitPrice =
                    GetValue(
                        row,
                        "UnitPrice"),

                CostPrice =
                    GetValue(
                        row,
                        "CostPrice"),

                DiscountPercentage =
                    NormalizeNullable(
                        GetValue(
                            row,
                            "DiscountPercentage")),

                Unit =
                    NormalizeNullable(
                        GetValue(row, "Unit")),

                SmallUnit =
                    GetValueOrDefault(
                        row,
                        "SmallUnit",
                        "قطعة"),

                LargeUnit =
                    GetValueOrDefault(
                        row,
                        "LargeUnit",
                        "كرتونة"),

                UnitsPerLargeUnit =
                    GetValueOrDefault(
                        row,
                        "UnitsPerLargeUnit",
                        "1"),

                MinStockLevel =
                    GetValueOrDefault(
                        row,
                        "MinStockLevel",
                        "0"),

                MaxStockLevel =
                    GetValueOrDefault(
                        row,
                        "MaxStockLevel",
                        "0"),

                EnableReturn =
                    GetValueOrDefault(
                        row,
                        "EnableReturn",
                        "true"),

                ReturnDamaged =
                    GetValueOrDefault(
                        row,
                        "ReturnDamaged",
                        "false"),

                ReturnPeriod =
                    NormalizeNullable(
                        GetValue(
                            row,
                            "ReturnPeriod")),

                WarehouseCode =
                    NormalizeNullable(
                        GetValue(
                            row,
                            "WarehouseCode"))
            };
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

        private static bool TryParseInt(
            string value,
            out int result)
        {
            return int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out result)
                ||
                int.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.CurrentCulture,
                    out result);
        }

        private static bool TryParseBoolean(
            string value,
            out bool result)
        {
            result = false;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            var normalized =
                value.Trim()
                    .ToLowerInvariant();

            switch (normalized)
            {
                case "true":
                case "1":
                case "yes":
                case "y":
                    result = true;
                    return true;

                case "false":
                case "0":
                case "no":
                case "n":
                    result = false;
                    return true;

                default:
                    return false;
            }
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

        private static decimal? ParseNullableDecimalForImport(
            string? value,
            string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return ParseDecimalForImport(
                value,
                fieldName);
        }

        private static int ParseIntForImport(
            string value,
            string fieldName)
        {
            if (TryParseInt(
                    value,
                    out var result))
            {
                return result;
            }

            throw new InvalidOperationException(
                $"{fieldName} contains an invalid integer value.");
        }

        private static int? ParseNullableIntForImport(
            string? value,
            string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return ParseIntForImport(
                value,
                fieldName);
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

        private static void AddErrorIfMissing(
            ICollection<ImportValidationErrorDto> previewErrors,
            ICollection<string> rowErrors,
            int rowNumber,
            string columnName,
            string message)
        {
            if (rowErrors.Any(x =>
                    x.Equals(
                        message,
                        StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            AddError(
                previewErrors,
                rowErrors,
                rowNumber,
                columnName,
                message);
        }
    }
}
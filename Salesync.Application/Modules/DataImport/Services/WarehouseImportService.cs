using FluentValidation;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.DataImport.Dtos;
using Salesync.Application.Modules.DataImport.Dtos.Warehouses;
using Salesync.Application.Modules.DataImport.Interfaces;
using Salesync.Application.Modules.MasterData.Dtos.WarehouseDto;
using Salesync.Domain.Common.Enums.MasterData;
using Salesync.Domain.Modules.DataImport.Entities;
using Salesync.Domain.Modules.DataImport.Enums;
using Salesync.Domain.Modules.MasterData.Entities;
using System.Globalization;
using System.Text.Json;

namespace Salesync.Application.Modules.DataImport.Services
{
    public class WarehouseImportService : IWarehouseImportService
    {
        private static readonly string[] Headers =
        {
            "WarehouseCode",
            "Name",
            "BranchCode",
            "Location",
            "WarehouseType",
            "Latitude",
            "Longitude"
        };

        private readonly IUnitOfWork _unitOfWork;
        private readonly IExcelImportService _excelImportService;
        private readonly IValidator<CreateWarehouseDto> _warehouseValidator;

        public WarehouseImportService(
            IUnitOfWork unitOfWork,
            IExcelImportService excelImportService,
            IValidator<CreateWarehouseDto> warehouseValidator)
        {
            _unitOfWork = unitOfWork;
            _excelImportService = excelImportService;
            _warehouseValidator = warehouseValidator;
        }

        // =====================================================
        // TEMPLATE
        // =====================================================

        public byte[] GenerateTemplate()
        {
            return _excelImportService.GenerateTemplate(
                "Warehouses",
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
                    ImportType = ImportType.Warehouses,
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
                    ImportType = ImportType.Warehouses,
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

            var branches =
                (await _unitOfWork.Branches.GetAllAsync())
                .Where(x => x.IsActive)
                .ToList();

            var branchByCode =
                branches
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.BranchCode))
                    .GroupBy(
                        x => x.BranchCode.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        x => x.Key,
                        x => x.First(),
                        StringComparer.OrdinalIgnoreCase);

            var warehouses =
                (await _unitOfWork.Warehouses.GetAllAsync())
                .ToList();

            var existingWarehouseCodes =
                warehouses
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.WarehouseCode))
                    .Select(x =>
                        x.WarehouseCode.Trim())
                    .ToHashSet(
                        StringComparer.OrdinalIgnoreCase);

            var excelWarehouseCodes =
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

                var warehouseRow =
                    MapRow(
                        row,
                        rowNumber);

                var rowErrors =
                    new List<string>();

                // -------------------------------------------------
                // Branch
                // -------------------------------------------------

                Branch? branch = null;

                if (string.IsNullOrWhiteSpace(
                        warehouseRow.BranchCode))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "BranchCode",
                        "Branch code is required.");
                }
                else if (!branchByCode.TryGetValue(
                             warehouseRow.BranchCode,
                             out branch))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "BranchCode",
                        $"Branch code '{warehouseRow.BranchCode}' does not exist or is inactive.");
                }

                // -------------------------------------------------
                // Warehouse Type
                // -------------------------------------------------

                WarehouseType? warehouseType = null;

                if (string.IsNullOrWhiteSpace(
                        warehouseRow.WarehouseType))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "WarehouseType",
                        "Warehouse type is required.");
                }
                else if (!TryParseWarehouseType(
                             warehouseRow.WarehouseType,
                             out var parsedWarehouseType))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "WarehouseType",
                        "Invalid warehouse type. Allowed values: Main, Branch, Van, Showroom, Damaged, Return.");
                }
                else
                {
                    warehouseType =
                        parsedWarehouseType;
                }

                // -------------------------------------------------
                // GPS
                // -------------------------------------------------

                decimal? latitude = null;
                decimal? longitude = null;

                if (!string.IsNullOrWhiteSpace(
                        warehouseRow.Latitude))
                {
                    if (!TryParseDecimal(
                            warehouseRow.Latitude,
                            out var parsedLatitude))
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "Latitude",
                            "Latitude must be a valid number.");
                    }
                    else if (parsedLatitude < -90 ||
                             parsedLatitude > 90)
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "Latitude",
                            "Latitude must be between -90 and 90.");
                    }
                    else
                    {
                        latitude =
                            parsedLatitude;
                    }
                }

                if (!string.IsNullOrWhiteSpace(
                        warehouseRow.Longitude))
                {
                    if (!TryParseDecimal(
                            warehouseRow.Longitude,
                            out var parsedLongitude))
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "Longitude",
                            "Longitude must be a valid number.");
                    }
                    else if (parsedLongitude < -180 ||
                             parsedLongitude > 180)
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "Longitude",
                            "Longitude must be between -180 and 180.");
                    }
                    else
                    {
                        longitude =
                            parsedLongitude;
                    }
                }

                // -------------------------------------------------
                // Existing Warehouse Validator
                // -------------------------------------------------

                if (branch is not null &&
                    warehouseType.HasValue)
                {
                    var createDto =
                        new CreateWarehouseDto
                        {
                            WarehouseCode =
                                warehouseRow.WarehouseCode,

                            Name =
                                warehouseRow.Name,

                            BranchId =
                                branch.Id,

                            Location =
                                warehouseRow.Location,

                            Type =
                                warehouseType.Value,

                            Latitude =
                                latitude,

                            Longitude =
                                longitude
                        };

                    var validationResult =
                        await _warehouseValidator.ValidateAsync(
                            createDto,
                            cancellationToken);

                    foreach (var error in
                             validationResult.Errors)
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            error.PropertyName,
                            error.ErrorMessage);
                    }
                }
                else
                {
                    /*
                     * If BranchCode or WarehouseType is invalid,
                     * we cannot build a valid CreateWarehouseDto.
                     * Validate independent fields manually.
                     */

                    if (string.IsNullOrWhiteSpace(
                            warehouseRow.WarehouseCode))
                    {
                        AddErrorIfMissing(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "WarehouseCode",
                            "Warehouse code is required.");
                    }
                    else if (warehouseRow.WarehouseCode.Length > 50)
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "WarehouseCode",
                            "Warehouse code cannot exceed 50 characters.");
                    }

                    if (string.IsNullOrWhiteSpace(
                            warehouseRow.Name))
                    {
                        AddErrorIfMissing(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "Name",
                            "Warehouse name is required.");
                    }
                    else if (warehouseRow.Name.Length > 100)
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "Name",
                            "Warehouse name cannot exceed 100 characters.");
                    }
                }

                // -------------------------------------------------
                // Duplicate Codes
                // -------------------------------------------------

                if (!string.IsNullOrWhiteSpace(
                        warehouseRow.WarehouseCode))
                {
                    if (!excelWarehouseCodes.Add(
                            warehouseRow.WarehouseCode))
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "WarehouseCode",
                            "Duplicate warehouse code inside the Excel file.");
                    }

                    if (existingWarehouseCodes.Contains(
                            warehouseRow.WarehouseCode))
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "WarehouseCode",
                            "Warehouse code already exists in the system.");
                    }
                }

                // -------------------------------------------------
                // STAGING
                // -------------------------------------------------

                var stagingRow =
                    new ImportBatchRow
                    {
                        ImportBatchId =
                            batch.Id,

                        RowNumber =
                            rowNumber,

                        DataJson =
                            JsonSerializer.Serialize(
                                warehouseRow),

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
                    ImportType.Warehouses,

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
                ImportType.Warehouses)
            {
                throw new InvalidOperationException(
                    "The selected batch is not a warehouse import.");
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

            // =================================================
            // Reload references before commit
            // =================================================

            var branches =
                (await _unitOfWork.Branches.GetAllAsync())
                .Where(x => x.IsActive)
                .ToList();

            var branchByCode =
                branches
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.BranchCode))
                    .GroupBy(
                        x => x.BranchCode.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        x => x.Key,
                        x => x.First(),
                        StringComparer.OrdinalIgnoreCase);

            var currentWarehouseCodes =
                (await _unitOfWork.Warehouses.GetAllAsync())
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.WarehouseCode))
                .Select(x =>
                    x.WarehouseCode.Trim())
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
                        JsonSerializer.Deserialize<
                            WarehouseImportRowDto>(
                            stagingRow.DataJson);

                    if (dto is null)
                    {
                        throw new InvalidOperationException(
                            $"Unable to read import row {stagingRow.RowNumber}.");
                    }

                    if (currentWarehouseCodes.Contains(
                            dto.WarehouseCode))
                    {
                        throw new InvalidOperationException(
                            $"Warehouse code '{dto.WarehouseCode}' already exists.");
                    }

                    if (!branchByCode.TryGetValue(
                            dto.BranchCode,
                            out var branch))
                    {
                        throw new InvalidOperationException(
                            $"Branch code '{dto.BranchCode}' does not exist or is inactive.");
                    }

                    if (!TryParseWarehouseType(
                            dto.WarehouseType,
                            out var warehouseType))
                    {
                        throw new InvalidOperationException(
                            $"Warehouse type '{dto.WarehouseType}' is invalid.");
                    }

                    var latitude =
                        ParseNullableDecimalForImport(
                            dto.Latitude);

                    var longitude =
                        ParseNullableDecimalForImport(
                            dto.Longitude);

                    if (latitude.HasValue &&
                        (latitude.Value < -90 ||
                         latitude.Value > 90))
                    {
                        throw new InvalidOperationException(
                            $"Latitude in row {dto.RowNumber} is outside the allowed range.");
                    }

                    if (longitude.HasValue &&
                        (longitude.Value < -180 ||
                         longitude.Value > 180))
                    {
                        throw new InvalidOperationException(
                            $"Longitude in row {dto.RowNumber} is outside the allowed range.");
                    }

                    var warehouse =
                        new Warehouse
                        {
                            WarehouseCode =
                                dto.WarehouseCode.Trim(),

                            Name =
                                dto.Name.Trim(),

                            BranchId =
                                branch.Id,

                            Location =
                                NormalizeNullable(
                                    dto.Location),

                            Type =
                                warehouseType,

                            Latitude =
                                latitude,

                            Longitude =
                                longitude,

                            IsActive =
                                true,

                            CreatedAt =
                                DateTime.UtcNow
                        };

                    await _unitOfWork.Warehouses
                        .AddAsync(warehouse);

                    currentWarehouseCodes.Add(
                        warehouse.WarehouseCode);

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
                        $"{importedRows} warehouses imported successfully."
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
                        "Warehouse import failed during database commit.";

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
                ImportType.Warehouses)
            {
                throw new InvalidOperationException(
                    "The selected batch is not a warehouse import.");
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
                new List<Dictionary<string, string>>();

            var errors =
                new Dictionary<int, List<string>>();

            foreach (var stagingRow in
                     stagingRows)
            {
                var dto =
                    JsonSerializer.Deserialize<
                        WarehouseImportRowDto>(
                        stagingRow.DataJson);

                if (dto is null)
                    continue;

                rows.Add(
                    new Dictionary<string, string>(
                        StringComparer.OrdinalIgnoreCase)
                    {
                        ["WarehouseCode"] =
                            dto.WarehouseCode,

                        ["Name"] =
                            dto.Name,

                        ["BranchCode"] =
                            dto.BranchCode,

                        ["Location"] =
                            dto.Location
                            ?? string.Empty,

                        ["WarehouseType"] =
                            dto.WarehouseType,

                        ["Latitude"] =
                            dto.Latitude
                            ?? string.Empty,

                        ["Longitude"] =
                            dto.Longitude
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
                "Warehouse Errors",
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
                    ImportType.Warehouses)
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

        private static WarehouseImportRowDto MapRow(
            IReadOnlyDictionary<string, string> row,
            int rowNumber)
        {
            return new WarehouseImportRowDto
            {
                RowNumber =
                    rowNumber,

                WarehouseCode =
                    GetValue(
                        row,
                        "WarehouseCode"),

                Name =
                    GetValue(
                        row,
                        "Name"),

                BranchCode =
                    GetValue(
                        row,
                        "BranchCode"),

                Location =
                    NormalizeNullable(
                        GetValue(
                            row,
                            "Location")),

                WarehouseType =
                    GetValue(
                        row,
                        "WarehouseType"),

                Latitude =
                    NormalizeNullable(
                        GetValue(
                            row,
                            "Latitude")),

                Longitude =
                    NormalizeNullable(
                        GetValue(
                            row,
                            "Longitude"))
            };
        }

        private static bool TryParseWarehouseType(
            string value,
            out WarehouseType warehouseType)
        {
            warehouseType =
                default;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            var normalized =
                value.Trim();

            if (int.TryParse(
                    normalized,
                    out var numericValue))
            {
                if (!Enum.IsDefined(
                        typeof(WarehouseType),
                        numericValue))
                {
                    return false;
                }

                warehouseType =
                    (WarehouseType)numericValue;

                return true;
            }

            return Enum.TryParse(
                       normalized,
                       ignoreCase: true,
                       out warehouseType)
                   &&
                   Enum.IsDefined(
                       typeof(WarehouseType),
                       warehouseType);
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

        private static decimal? ParseNullableDecimalForImport(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (TryParseDecimal(
                    value,
                    out var result))
            {
                return result;
            }

            throw new InvalidOperationException(
                $"'{value}' is not a valid decimal number.");
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
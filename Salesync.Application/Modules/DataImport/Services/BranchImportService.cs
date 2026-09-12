using FluentValidation;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.DataImport.Dtos;
using Salesync.Application.Modules.DataImport.Dtos.Branches;
using Salesync.Application.Modules.DataImport.Interfaces;
using Salesync.Application.Modules.MasterData.Dtos.BranchDto;
using Salesync.Domain.Modules.DataImport.Entities;
using Salesync.Domain.Modules.DataImport.Enums;
using Salesync.Domain.Modules.MasterData.Entities;
using System.Text.Json;

namespace Salesync.Application.Modules.DataImport.Services
{
    public class BranchImportService : IBranchImportService
    {
        private static readonly string[] Headers =
        {
            "BranchCode",
            "Name",
            "City",
            "Address",
            "Phone"
        };

        private readonly IUnitOfWork _unitOfWork;
        private readonly IExcelImportService _excelImportService;
        private readonly IValidator<CreateBranchDto> _branchValidator;

        public BranchImportService(
            IUnitOfWork unitOfWork,
            IExcelImportService excelImportService,
            IValidator<CreateBranchDto> branchValidator)
        {
            _unitOfWork = unitOfWork;
            _excelImportService = excelImportService;
            _branchValidator = branchValidator;
        }


        public byte[] GenerateTemplate()
        {
            return _excelImportService.GenerateTemplate(
                "Branches",
                Headers);
        }


        public async Task<ImportPreviewDto> ValidateAsync(
            Stream fileStream,
            string fileName,
            string? userId,
            CancellationToken cancellationToken = default)
        {
            if (fileStream is null)
                throw new ArgumentNullException(nameof(fileStream));

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException(
                    "File name is required.",
                    nameof(fileName));


            var rows =
                await _excelImportService.ReadAsync(
                    fileStream,
                    cancellationToken);


            var batch =
                new ImportBatch
                {
                    ImportType =
                        ImportType.Branches,

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

                batch.ErrorRows =
                    1;

                batch.ValidRows =
                    0;

                batch.ValidatedAt =
                    DateTime.UtcNow;


                await _unitOfWork.CompleteAsync();


                return new ImportPreviewDto
                {
                    BatchId =
                        batch.Id,

                    ImportType =
                        ImportType.Branches,

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
                            RowNumber = 0,
                            ColumnName = null,
                            Message =
                                "The Excel file does not contain any data rows."
                        }
                    }
                };
            }


            ValidateHeaders(rows);


            var existingBranches =
                (await _unitOfWork.Branches
                    .GetAllAsync())
                .ToList();


            var existingCodes =
                existingBranches
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(
                            x.BranchCode))
                    .Select(x =>
                        x.BranchCode.Trim())
                    .ToHashSet(
                        StringComparer.OrdinalIgnoreCase);


            var excelCodes =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);


            var previewErrors =
                new List<ImportValidationErrorDto>();


            var validRows =
                0;


            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();


                var rowNumber =
                    GetRowNumber(row);


                var branchRow =
                    MapRow(
                        row,
                        rowNumber);


                var rowErrors =
                    new List<string>();


                // FluentValidation

                var createDto =
                    new CreateBranchDto
                    {
                        BranchCode =
                            branchRow.BranchCode,

                        Name =
                            branchRow.Name,

                        City =
                            branchRow.City,

                        Address =
                            branchRow.Address,

                        Phone =
                            branchRow.Phone
                    };


                var validationResult =
                    await _branchValidator
                        .ValidateAsync(
                            createDto,
                            cancellationToken);


                foreach (var error in
                         validationResult.Errors)
                {
                    rowErrors.Add(
                        error.ErrorMessage);

                    previewErrors.Add(
                        new ImportValidationErrorDto
                        {
                            RowNumber =
                                rowNumber,

                            ColumnName =
                                error.PropertyName,

                            Message =
                                error.ErrorMessage
                        });
                }


                // Duplicate inside Excel

                if (!string.IsNullOrWhiteSpace(
                        branchRow.BranchCode))
                {
                    if (!excelCodes.Add(
                            branchRow.BranchCode))
                    {
                        const string message =
                            "Duplicate branch code inside the Excel file.";


                        rowErrors.Add(
                            message);


                        previewErrors.Add(
                            new ImportValidationErrorDto
                            {
                                RowNumber =
                                    rowNumber,

                                ColumnName =
                                    "BranchCode",

                                Message =
                                    message
                            });
                    }


                    // Existing DB branch

                    if (existingCodes.Contains(
                            branchRow.BranchCode))
                    {
                        const string message =
                            "Branch code already exists in the system.";


                        rowErrors.Add(
                            message);


                        previewErrors.Add(
                            new ImportValidationErrorDto
                            {
                                RowNumber =
                                    rowNumber,

                                ColumnName =
                                    "BranchCode",

                                Message =
                                    message
                            });
                    }
                }


                var stagingRow =
                    new ImportBatchRow
                    {
                        ImportBatchId =
                            batch.Id,

                        RowNumber =
                            rowNumber,

                        DataJson =
                            JsonSerializer.Serialize(
                                branchRow),

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
                    ImportType.Branches,

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
                ImportType.Branches)
            {
                throw new InvalidOperationException(
                    "The selected batch is not a branch import.");
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


            await _unitOfWork.BeginTransactionAsync();


            try
            {
                batch.Status =
                    ImportStatus.Importing;


                await _unitOfWork.CompleteAsync();


                var currentBranches =
                    (await _unitOfWork.Branches
                        .GetAllAsync())
                    .Select(x =>
                        x.BranchCode.Trim())
                    .ToHashSet(
                        StringComparer.OrdinalIgnoreCase);


                var importedRows =
                    0;


                foreach (var stagingRow in
                         stagingRows)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();


                    var dto =
                        JsonSerializer.Deserialize<
                            BranchImportRowDto>(
                            stagingRow.DataJson);


                    if (dto is null)
                    {
                        throw new InvalidOperationException(
                            $"Unable to read import row {stagingRow.RowNumber}.");
                    }


                    /*
                     * Re-check uniqueness at confirm time.
                     * This protects against another branch being
                     * created between Validate and Import.
                     */
                    if (currentBranches.Contains(
                            dto.BranchCode))
                    {
                        throw new InvalidOperationException(
                            $"Branch code '{dto.BranchCode}' already exists.");
                    }


                    var branch =
                        new Branch
                        {
                            BranchCode =
                                dto.BranchCode.Trim(),

                            Name =
                                dto.Name.Trim(),

                            City =
                                NormalizeNullable(
                                    dto.City),

                            Address =
                                NormalizeNullable(
                                    dto.Address),

                            Phone =
                                NormalizeNullable(
                                    dto.Phone),

                            IsActive =
                                true,

                            CreatedAt =
                                DateTime.UtcNow
                        };


                    await _unitOfWork.Branches
                        .AddAsync(branch);


                    currentBranches.Add(
                        branch.BranchCode);


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
                        $"{importedRows} branches imported successfully."
                };
            }
            catch
            {
                await _unitOfWork
                    .RollbackTransactionAsync();


                /*
                 * Transaction rollback may revert the Failed status
                 * too, so persist it after rollback.
                 */
                var failedBatch =
                    await _unitOfWork.ImportBatches
                        .GetByIdAsync(batchId);


                if (failedBatch is not null)
                {
                    failedBatch.Status =
                        ImportStatus.Failed;

                    failedBatch.Notes =
                        "Import failed during database commit.";

                    await _unitOfWork.CompleteAsync();
                }


                throw;
            }
        }


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
                        BranchImportRowDto>(
                        stagingRow.DataJson);


                if (dto is null)
                    continue;


                rows.Add(
                    new Dictionary<string, string>(
                        StringComparer.OrdinalIgnoreCase)
                    {
                        ["BranchCode"] =
                            dto.BranchCode,

                        ["Name"] =
                            dto.Name,

                        ["City"] =
                            dto.City ?? string.Empty,

                        ["Address"] =
                            dto.Address ?? string.Empty,

                        ["Phone"] =
                            dto.Phone ?? string.Empty,

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
                        ?? new List<string>();
                }
            }


            return _excelImportService
                .GenerateErrorReport(
                    "Branch Errors",
                    Headers,
                    rows,
                    errors);
        }


        public async Task<IEnumerable<ImportBatchHistoryDto>>
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
                    ImportType.Branches)
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
                        !firstRow.ContainsKey(
                            header))
                    .ToList();


            if (missingHeaders.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Missing required Excel headers: {string.Join(", ", missingHeaders)}");
            }
        }


        private static BranchImportRowDto MapRow(
            IReadOnlyDictionary<string, string> row,
            int rowNumber)
        {
            return new BranchImportRowDto
            {
                RowNumber =
                    rowNumber,

                BranchCode =
                    GetValue(
                        row,
                        "BranchCode"),

                Name =
                    GetValue(
                        row,
                        "Name"),

                City =
                    NormalizeNullable(
                        GetValue(
                            row,
                            "City")),

                Address =
                    NormalizeNullable(
                        GetValue(
                            row,
                            "Address")),

                Phone =
                    NormalizeNullable(
                        GetValue(
                            row,
                            "Phone"))
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


        private static string? NormalizeNullable(
            string? value)
        {
            return string.IsNullOrWhiteSpace(
                value)
                ? null
                : value.Trim();
        }
    }
}
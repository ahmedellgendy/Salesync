using FluentValidation;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.DataImport.Dtos;
using Salesync.Application.Modules.DataImport.Dtos.Customers;
using Salesync.Application.Modules.DataImport.Interfaces;
using Salesync.Application.Modules.MasterData.Dtos.CustomerDto;
using Salesync.Domain.Common.Enums.MasterData;
using Salesync.Domain.Modules.DataImport.Entities;
using Salesync.Domain.Modules.DataImport.Enums;
using Salesync.Domain.Modules.MasterData.Entities;
using System.Globalization;
using System.Text.Json;

namespace Salesync.Application.Modules.DataImport.Services
{
    public class CustomerImportService : ICustomerImportService
    {
        private static readonly string[] Headers =
        {
            "Name",
            "Phone",
            "Email",
            "Country",
            "Address",
            "Area",
            "City",
            "District",
            "Region",
            "PostalCode",
            "Latitude",
            "Longitude",
            "CategoryCode",
            "SalesSectorCode",
            "ClassId",
            "CustomerType",
            "AllowCash",
            "AllowCheck",
            "AllowCreditCard",
            "PaymentTermsCode",
            "CreditLimit",
            "OrderCeiling",
            "AccountNumber",
            "TaxId",
            "PriceId",
            "BranchCode"
        };

        private readonly IUnitOfWork _unitOfWork;
        private readonly IExcelImportService _excelImportService;
        private readonly IValidator<CreateCustomerDto> _customerValidator;

        public CustomerImportService(
            IUnitOfWork unitOfWork,
            IExcelImportService excelImportService,
            IValidator<CreateCustomerDto> customerValidator)
        {
            _unitOfWork = unitOfWork;
            _excelImportService = excelImportService;
            _customerValidator = customerValidator;
        }

        // =====================================================
        // TEMPLATE
        // =====================================================

        public byte[] GenerateTemplate()
        {
            return _excelImportService.GenerateTemplate(
                "Customers",
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
                    ImportType = ImportType.Customers,
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
                batch.ValidatedAt = DateTime.UtcNow;

                await _unitOfWork.CompleteAsync();

                return new ImportPreviewDto
                {
                    BatchId = batch.Id,
                    ImportType = ImportType.Customers,
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

            var customers =
                (await _unitOfWork.Customers.GetAllAsync())
                .ToList();

            /*
             * Phone is required by CustomerCreateValidator,
             * therefore use it as the duplicate key for onboarding.
             */
            var existingPhones =
                customers
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.Phone))
                    .Select(x =>
                        x.Phone!.Trim())
                    .ToHashSet(
                        StringComparer.OrdinalIgnoreCase);

            var excelPhones =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            var previewErrors =
                new List<ImportValidationErrorDto>();

            var validRows = 0;

            // =================================================
            // VALIDATE ROWS
            // =================================================

            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var rowNumber =
                    GetRowNumber(row);

                var customerRow =
                    MapRow(
                        row,
                        rowNumber);

                var rowErrors =
                    new List<string>();

                // -------------------------------------------------
                // CustomerType
                // -------------------------------------------------

                CustomerType customerType =
                    CustomerType.Individual;

                if (!TryParseCustomerType(
                        customerRow.CustomerType,
                        out customerType))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "CustomerType",
                        "Invalid customer type. Allowed values: Individual, Corporate, 1, 2.");
                }

                // -------------------------------------------------
                // Payment booleans
                // -------------------------------------------------

                bool allowCash = true;
                bool allowCheck = false;
                bool allowCreditCard = false;

                if (!TryParseBoolean(
                        customerRow.AllowCash,
                        out allowCash))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "AllowCash",
                        "AllowCash must be True/False, Yes/No, or 1/0.");
                }

                if (!TryParseBoolean(
                        customerRow.AllowCheck,
                        out allowCheck))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "AllowCheck",
                        "AllowCheck must be True/False, Yes/No, or 1/0.");
                }

                if (!TryParseBoolean(
                        customerRow.AllowCreditCard,
                        out allowCreditCard))
                {
                    AddError(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        "AllowCreditCard",
                        "AllowCreditCard must be True/False, Yes/No, or 1/0.");
                }

                // -------------------------------------------------
                // GPS
                // -------------------------------------------------

                decimal? latitude = null;
                decimal? longitude = null;

                if (!string.IsNullOrWhiteSpace(
                        customerRow.Latitude))
                {
                    if (!TryParseDecimal(
                            customerRow.Latitude,
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
                        customerRow.Longitude))
                {
                    if (!TryParseDecimal(
                            customerRow.Longitude,
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
                // Credit
                // -------------------------------------------------

                decimal? creditLimit = null;
                decimal? orderCeiling = null;

                if (!string.IsNullOrWhiteSpace(
                        customerRow.CreditLimit))
                {
                    if (!TryParseDecimal(
                            customerRow.CreditLimit,
                            out var parsedCreditLimit))
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "CreditLimit",
                            "Credit limit must be a valid number.");
                    }
                    else
                    {
                        creditLimit =
                            parsedCreditLimit;
                    }
                }

                if (!string.IsNullOrWhiteSpace(
                        customerRow.OrderCeiling))
                {
                    if (!TryParseDecimal(
                            customerRow.OrderCeiling,
                            out var parsedOrderCeiling))
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "OrderCeiling",
                            "Order ceiling must be a valid number.");
                    }
                    else
                    {
                        orderCeiling =
                            parsedOrderCeiling;
                    }
                }

                // -------------------------------------------------
                // BranchCode -> BranchId
                // -------------------------------------------------

                int? branchId = null;

                if (!string.IsNullOrWhiteSpace(
                        customerRow.BranchCode))
                {
                    if (!branchByCode.TryGetValue(
                            customerRow.BranchCode,
                            out var branch))
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "BranchCode",
                            $"Branch code '{customerRow.BranchCode}' does not exist or is inactive.");
                    }
                    else
                    {
                        branchId =
                            branch.Id;
                    }
                }

                // -------------------------------------------------
                // Existing validator
                // -------------------------------------------------

                var createDto =
                    new CreateCustomerDto
                    {
                        Name =
                            customerRow.Name,

                        Phone =
                            customerRow.Phone,

                        Email =
                            customerRow.Email,

                        Country =
                            customerRow.Country,

                        Address =
                            customerRow.Address,

                        Area =
                            customerRow.Area,

                        City =
                            customerRow.City,

                        District =
                            customerRow.District,

                        Region =
                            customerRow.Region,

                        PostalCode =
                            customerRow.PostalCode,

                        Latitude =
                            latitude,

                        Longitude =
                            longitude,

                        CategoryCode =
                            customerRow.CategoryCode,

                        SalesSectorCode =
                            customerRow.SalesSectorCode,

                        ClassId =
                            customerRow.ClassId,

                        Type =
                            customerType,

                        AllowCash =
                            allowCash,

                        AllowCheck =
                            allowCheck,

                        AllowCreditCard =
                            allowCreditCard,

                        PaymentTermsCode =
                            customerRow.PaymentTermsCode,

                        CreditLimit =
                            creditLimit,

                        OrderCeiling =
                            orderCeiling,

                        AccountNumber =
                            customerRow.AccountNumber,

                        TaxId =
                            customerRow.TaxId,

                        PriceId =
                            customerRow.PriceId,

                        BranchId =
                            branchId
                    };

                var validationResult =
                    await _customerValidator.ValidateAsync(
                        createDto,
                        cancellationToken);

                foreach (var error in
                         validationResult.Errors)
                {
                    AddErrorIfMissing(
                        previewErrors,
                        rowErrors,
                        rowNumber,
                        error.PropertyName,
                        error.ErrorMessage);
                }

                // -------------------------------------------------
                // Duplicate phone
                // -------------------------------------------------

                if (!string.IsNullOrWhiteSpace(
                        customerRow.Phone))
                {
                    if (!excelPhones.Add(
                            customerRow.Phone))
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "Phone",
                            "Duplicate customer phone inside the Excel file.");
                    }

                    if (existingPhones.Contains(
                            customerRow.Phone))
                    {
                        AddError(
                            previewErrors,
                            rowErrors,
                            rowNumber,
                            "Phone",
                            "Customer phone already exists in the system.");
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
                                customerRow),

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
                    ImportType.Customers,

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
                ImportType.Customers)
            {
                throw new InvalidOperationException(
                    "The selected batch is not a customer import.");
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

            var currentPhones =
                (await _unitOfWork.Customers.GetAllAsync())
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.Phone))
                .Select(x =>
                    x.Phone!.Trim())
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
                        JsonSerializer.Deserialize<CustomerImportRowDto>(
                            stagingRow.DataJson);

                    if (dto is null)
                    {
                        throw new InvalidOperationException(
                            $"Unable to read import row {stagingRow.RowNumber}.");
                    }

                    if (currentPhones.Contains(
                            dto.Phone))
                    {
                        throw new InvalidOperationException(
                            $"Customer phone '{dto.Phone}' already exists.");
                    }

                    int? branchId = null;

                    if (!string.IsNullOrWhiteSpace(
                            dto.BranchCode))
                    {
                        if (!branchByCode.TryGetValue(
                                dto.BranchCode,
                                out var branch))
                        {
                            throw new InvalidOperationException(
                                $"Branch code '{dto.BranchCode}' does not exist or is inactive.");
                        }

                        branchId =
                            branch.Id;
                    }

                    if (!TryParseCustomerType(
                            dto.CustomerType,
                            out var customerType))
                    {
                        throw new InvalidOperationException(
                            $"Customer type '{dto.CustomerType}' is invalid.");
                    }

                    var customer =
                        new Customer
                        {
                            Name =
                                dto.Name.Trim(),

                            Phone =
                                dto.Phone.Trim(),

                            Email =
                                NormalizeNullable(dto.Email),

                            Country =
                                NormalizeNullable(dto.Country),

                            Address =
                                NormalizeNullable(dto.Address),

                            Area =
                                NormalizeNullable(dto.Area),

                            City =
                                NormalizeNullable(dto.City),

                            District =
                                NormalizeNullable(dto.District),

                            Region =
                                NormalizeNullable(dto.Region),

                            PostalCode =
                                NormalizeNullable(dto.PostalCode),

                            Latitude =
                                ParseNullableDecimalForImport(
                                    dto.Latitude,
                                    "Latitude"),

                            Longitude =
                                ParseNullableDecimalForImport(
                                    dto.Longitude,
                                    "Longitude"),

                            CategoryCode =
                                NormalizeNullable(dto.CategoryCode),

                            SalesSectorCode =
                                NormalizeNullable(dto.SalesSectorCode),

                            ClassId =
                                NormalizeNullable(dto.ClassId),

                            Type =
                                customerType,

                            AllowCash =
                                ParseBooleanForImport(
                                    dto.AllowCash,
                                    "AllowCash"),

                            AllowCheck =
                                ParseBooleanForImport(
                                    dto.AllowCheck,
                                    "AllowCheck"),

                            AllowCreditCard =
                                ParseBooleanForImport(
                                    dto.AllowCreditCard,
                                    "AllowCreditCard"),

                            PaymentTermsCode =
                                NormalizeNullable(
                                    dto.PaymentTermsCode),

                            CreditLimit =
                                ParseNullableDecimalForImport(
                                    dto.CreditLimit,
                                    "CreditLimit"),

                            OrderCeiling =
                                ParseNullableDecimalForImport(
                                    dto.OrderCeiling,
                                    "OrderCeiling"),

                            AccountNumber =
                                NormalizeNullable(dto.AccountNumber),

                            TaxId =
                                NormalizeNullable(dto.TaxId),

                            PriceId =
                                NormalizeNullable(dto.PriceId),

                            BranchId =
                                branchId,

                            CurrentBalance =
                                0,

                            TotalPurchaseAmount =
                                0,

                            Status =
                                CustomerStatus.Active,

                            IsActive =
                                true,

                            CreatedAt =
                                DateTime.UtcNow
                        };

                    await _unitOfWork.Customers
                        .AddAsync(customer);

                    currentPhones.Add(
                        customer.Phone!);

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
                    BatchId = batch.Id,
                    ImportedRows = importedRows,
                    Success = true,
                    Message =
                        $"{importedRows} customers imported successfully."
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
                        "Customer import failed during database commit.";

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
                ImportType.Customers)
            {
                throw new InvalidOperationException(
                    "The selected batch is not a customer import.");
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
                    JsonSerializer.Deserialize<CustomerImportRowDto>(
                        stagingRow.DataJson);

                if (dto is null)
                    continue;

                rows.Add(
                    new Dictionary<string, string>(
                        StringComparer.OrdinalIgnoreCase)
                    {
                        ["Name"] = dto.Name,
                        ["Phone"] = dto.Phone,
                        ["Email"] = dto.Email ?? string.Empty,
                        ["Country"] = dto.Country ?? string.Empty,
                        ["Address"] = dto.Address,
                        ["Area"] = dto.Area ?? string.Empty,
                        ["City"] = dto.City ?? string.Empty,
                        ["District"] = dto.District ?? string.Empty,
                        ["Region"] = dto.Region ?? string.Empty,
                        ["PostalCode"] = dto.PostalCode ?? string.Empty,
                        ["Latitude"] = dto.Latitude ?? string.Empty,
                        ["Longitude"] = dto.Longitude ?? string.Empty,
                        ["CategoryCode"] = dto.CategoryCode ?? string.Empty,
                        ["SalesSectorCode"] = dto.SalesSectorCode ?? string.Empty,
                        ["ClassId"] = dto.ClassId ?? string.Empty,
                        ["CustomerType"] = dto.CustomerType,
                        ["AllowCash"] = dto.AllowCash,
                        ["AllowCheck"] = dto.AllowCheck,
                        ["AllowCreditCard"] = dto.AllowCreditCard,
                        ["PaymentTermsCode"] = dto.PaymentTermsCode ?? string.Empty,
                        ["CreditLimit"] = dto.CreditLimit ?? string.Empty,
                        ["OrderCeiling"] = dto.OrderCeiling ?? string.Empty,
                        ["AccountNumber"] = dto.AccountNumber ?? string.Empty,
                        ["TaxId"] = dto.TaxId ?? string.Empty,
                        ["PriceId"] = dto.PriceId ?? string.Empty,
                        ["BranchCode"] = dto.BranchCode ?? string.Empty,
                        ["__RowNumber"] = dto.RowNumber.ToString()
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
                "Customer Errors",
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
                    ImportType.Customers)
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

            var firstRow = rows[0];

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

        private static CustomerImportRowDto MapRow(
            IReadOnlyDictionary<string, string> row,
            int rowNumber)
        {
            return new CustomerImportRowDto
            {
                RowNumber = rowNumber,

                Name =
                    GetValue(row, "Name"),

                Phone =
                    GetValue(row, "Phone"),

                Email =
                    NormalizeNullable(GetValue(row, "Email")),

                Country =
                    NormalizeNullable(GetValue(row, "Country")),

                Address =
                    GetValue(row, "Address"),

                Area =
                    NormalizeNullable(GetValue(row, "Area")),

                City =
                    NormalizeNullable(GetValue(row, "City")),

                District =
                    NormalizeNullable(GetValue(row, "District")),

                Region =
                    NormalizeNullable(GetValue(row, "Region")),

                PostalCode =
                    NormalizeNullable(GetValue(row, "PostalCode")),

                Latitude =
                    NormalizeNullable(GetValue(row, "Latitude")),

                Longitude =
                    NormalizeNullable(GetValue(row, "Longitude")),

                CategoryCode =
                    NormalizeNullable(GetValue(row, "CategoryCode")),

                SalesSectorCode =
                    NormalizeNullable(GetValue(row, "SalesSectorCode")),

                ClassId =
                    NormalizeNullable(GetValue(row, "ClassId")),

                CustomerType =
                    GetValueOrDefault(
                        row,
                        "CustomerType",
                        "Individual"),

                AllowCash =
                    GetValueOrDefault(
                        row,
                        "AllowCash",
                        "true"),

                AllowCheck =
                    GetValueOrDefault(
                        row,
                        "AllowCheck",
                        "false"),

                AllowCreditCard =
                    GetValueOrDefault(
                        row,
                        "AllowCreditCard",
                        "false"),

                PaymentTermsCode =
                    NormalizeNullable(
                        GetValue(
                            row,
                            "PaymentTermsCode")),

                CreditLimit =
                    NormalizeNullable(
                        GetValue(
                            row,
                            "CreditLimit")),

                OrderCeiling =
                    NormalizeNullable(
                        GetValue(
                            row,
                            "OrderCeiling")),

                AccountNumber =
                    NormalizeNullable(
                        GetValue(
                            row,
                            "AccountNumber")),

                TaxId =
                    NormalizeNullable(
                        GetValue(
                            row,
                            "TaxId")),

                PriceId =
                    NormalizeNullable(
                        GetValue(
                            row,
                            "PriceId")),

                BranchCode =
                    NormalizeNullable(
                        GetValue(
                            row,
                            "BranchCode"))
            };
        }

        private static bool TryParseCustomerType(
            string value,
            out CustomerType customerType)
        {
            customerType = default;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            var normalized = value.Trim();

            if (int.TryParse(
                    normalized,
                    out var numericValue))
            {
                if (!Enum.IsDefined(
                        typeof(CustomerType),
                        numericValue))
                {
                    return false;
                }

                customerType =
                    (CustomerType)numericValue;

                return true;
            }

            return Enum.TryParse(
                       normalized,
                       true,
                       out customerType)
                   &&
                   Enum.IsDefined(
                       typeof(CustomerType),
                       customerType);
        }

        private static bool TryParseBoolean(
            string value,
            out bool result)
        {
            result = false;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            switch (value.Trim().ToLowerInvariant())
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
            string? value,
            string fieldName)
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
                $"{fieldName} contains an invalid decimal value.");
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
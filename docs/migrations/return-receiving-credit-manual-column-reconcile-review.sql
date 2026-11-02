-- FINAL FOR APPROVAL ONLY. Not executed against any SQL Server by this task.
-- Target: manually added CreditAppliedAmount already exists, BOTH return-receipt
-- tables are absent, and AddReturnReceivingCreditLedger is NOT recorded.
-- Do not use the older generated migration script for this partial-schema state.
-- Approval, a database backup and a short maintenance window are prerequisites.
-- No historical return/credit reconstruction, no financial commands or business DML.
-- The existing credit column is preserved; add its zero DEFAULT only if absent.
-- This script deliberately STOPS on any unexpected existing schema or prior credit.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 15000;

DECLARE @ExpectedDatabase sysname = N'db68089';
IF DB_NAME() <> @ExpectedDatabase
    THROW 51002, N'Wrong database context. Nothing was changed.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
        THROW 51003, N'Migration history is unavailable. Review schema manually.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
                   WHERE MigrationId = N'20261021082158_AddOperationExecutionReceipts')
        THROW 51004, N'Prerequisite operation receipt migration is not recorded.', 1;
    IF EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
               WHERE MigrationId = N'20261026194614_AddReturnReceivingCreditLedger')
        THROW 51005, N'Target migration is already recorded. Do not rerun; verify schema.', 1;
    IF OBJECT_ID(N'dbo.InvoiceReturnReceipts') IS NOT NULL
       OR OBJECT_ID(N'dbo.InvoiceReturnReceiptItems') IS NOT NULL
        THROW 51006, N'A return receipt object already exists. Partial schema requires separate review.', 1;

    IF OBJECT_ID(N'dbo.Invoices', N'U') IS NULL
       OR OBJECT_ID(N'dbo.InvoiceReturns', N'U') IS NULL
       OR OBJECT_ID(N'dbo.InvoiceReturnItems', N'U') IS NULL
       OR OBJECT_ID(N'dbo.Customers', N'U') IS NULL
       OR OBJECT_ID(N'dbo.SalesReps', N'U') IS NULL
       OR OBJECT_ID(N'dbo.Products', N'U') IS NULL
       OR OBJECT_ID(N'dbo.Warehouses', N'U') IS NULL
        THROW 51007, N'A prerequisite business table is unavailable in dbo.', 1;

    -- Validate the manual column; never re-add it, change its type/nullability, or reset its values.
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns c JOIN sys.types t ON t.user_type_id = c.user_type_id
        WHERE c.object_id = OBJECT_ID(N'dbo.Invoices') AND c.name = N'CreditAppliedAmount'
          AND t.name IN (N'decimal', N'numeric') AND c.precision = 18 AND c.scale = 2
          AND c.is_nullable = 0 AND c.is_computed = 0)
        THROW 51008, N'CreditAppliedAmount must already be decimal(18,2) NOT NULL. Review its definition.', 1;

    DECLARE @CreditDefault nvarchar(max);
    DECLARE @CreditDefaultObjectId int;
    SELECT @CreditDefault = d.definition, @CreditDefaultObjectId = c.default_object_id
    FROM sys.columns c LEFT JOIN sys.default_constraints d ON d.object_id = c.default_object_id
    WHERE c.object_id = OBJECT_ID(N'dbo.Invoices') AND c.name = N'CreditAppliedAmount';
    SET @CreditDefault = REPLACE(REPLACE(REPLACE(@CreditDefault, N'(', N''), N')', N''), N' ', N'');
    IF @CreditDefaultObjectId IS NULL
        THROW 51009, N'Credit default metadata is unavailable. Review permissions and schema.', 1;
    IF @CreditDefaultObjectId <> 0 AND (
        TRY_CONVERT(decimal(18,2), @CreditDefault) IS NULL
        OR TRY_CONVERT(decimal(18,2), @CreditDefault) <> 0)
        THROW 51011, N'An existing credit DEFAULT differs from numeric zero. Nothing will be replaced.', 1;

    -- No ledger will be invented for credit already applied outside this module.
    IF EXISTS (SELECT 1 FROM dbo.Invoices WHERE CreditAppliedAmount <> 0)
        THROW 51010, N'Existing non-zero credit needs financial review before creating an empty receipt ledger.', 1;

    -- Up() specifies defaultValue: 0m. Completing a missing DEFAULT is
    -- schema-only and does not UPDATE or backfill any existing credit value.
    IF @CreditDefaultObjectId = 0
        ALTER TABLE [dbo].[Invoices] ADD DEFAULT (0) FOR [CreditAppliedAmount];

    CREATE TABLE [dbo].[InvoiceReturnReceipts] (
            [Id] int NOT NULL IDENTITY,
            [OperationId] uniqueidentifier NOT NULL,
            [RequestHash] nvarchar(64) NOT NULL,
            [ReceiptNumber] nvarchar(50) NOT NULL,
            [CreditNoteNumber] nvarchar(50) NOT NULL,
            [InvoiceReturnId] int NOT NULL,
            [InvoiceId] int NOT NULL,
            [CustomerId] int NOT NULL,
            [SalesRepId] int NULL,
            [SalesRepSessionId] int NULL,
            [TotalAmount] decimal(18,2) NOT NULL,
            [AppliedToInvoiceAmount] decimal(18,2) NOT NULL,
            [CustomerCreditAmount] decimal(18,2) NOT NULL,
            [CloseRemaining] bit NOT NULL,
            [Notes] nvarchar(500) NULL,
            [IsActive] bit NOT NULL,
            [CreatedAt] datetime2 NOT NULL,
            [UpdatedAt] datetime2 NULL,
            [CreatedBy] nvarchar(450) NULL,
            [UpdatedBy] nvarchar(max) NULL,
            CONSTRAINT [PK_InvoiceReturnReceipts] PRIMARY KEY ([Id]),
            CONSTRAINT [CK_ReturnReceipt_Amounts] CHECK ([TotalAmount] >= 0 AND [AppliedToInvoiceAmount] >= 0 AND [CustomerCreditAmount] >= 0 AND [TotalAmount] = [AppliedToInvoiceAmount] + [CustomerCreditAmount]),
            CONSTRAINT [FK_InvoiceReturnReceipts_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]) ON DELETE NO ACTION,
            CONSTRAINT [FK_InvoiceReturnReceipts_InvoiceReturns_InvoiceReturnId] FOREIGN KEY ([InvoiceReturnId]) REFERENCES [dbo].[InvoiceReturns] ([Id]) ON DELETE NO ACTION,
            CONSTRAINT [FK_InvoiceReturnReceipts_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [dbo].[Invoices] ([Id]) ON DELETE NO ACTION,
            CONSTRAINT [FK_InvoiceReturnReceipts_SalesReps_SalesRepId] FOREIGN KEY ([SalesRepId]) REFERENCES [dbo].[SalesReps] ([Id]) ON DELETE NO ACTION
        );

    CREATE TABLE [dbo].[InvoiceReturnReceiptItems] (
            [Id] int NOT NULL IDENTITY,
            [ReceiptId] int NOT NULL,
            [InvoiceReturnItemId] int NOT NULL,
            [ProductId] int NOT NULL,
            [ProductName] nvarchar(250) NOT NULL,
            [ItemCode] nvarchar(100) NOT NULL,
            [WarehouseId] int NOT NULL,
            [Quantity] int NOT NULL,
            [BonusQuantity] int NOT NULL,
            [Condition] int NOT NULL,
            [TotalAmount] decimal(18,2) NOT NULL,
            [IsActive] bit NOT NULL,
            [CreatedAt] datetime2 NOT NULL,
            [UpdatedAt] datetime2 NULL,
            [CreatedBy] nvarchar(max) NULL,
            [UpdatedBy] nvarchar(max) NULL,
            CONSTRAINT [PK_InvoiceReturnReceiptItems] PRIMARY KEY ([Id]),
            CONSTRAINT [CK_ReturnReceiptItem_Quantity] CHECK ([Quantity] >= 0 AND [BonusQuantity] >= 0 AND ([Quantity] > 0 OR [BonusQuantity] > 0) AND [TotalAmount] >= 0),
            CONSTRAINT [FK_InvoiceReturnReceiptItems_InvoiceReturnItems_InvoiceReturnItemId] FOREIGN KEY ([InvoiceReturnItemId]) REFERENCES [dbo].[InvoiceReturnItems] ([Id]) ON DELETE NO ACTION,
            CONSTRAINT [FK_InvoiceReturnReceiptItems_InvoiceReturnReceipts_ReceiptId] FOREIGN KEY ([ReceiptId]) REFERENCES [dbo].[InvoiceReturnReceipts] ([Id]) ON DELETE NO ACTION,
            CONSTRAINT [FK_InvoiceReturnReceiptItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]) ON DELETE NO ACTION,
            CONSTRAINT [FK_InvoiceReturnReceiptItems_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[Warehouses] ([Id]) ON DELETE NO ACTION
        );

    CREATE INDEX [IX_InvoiceReturnReceiptItems_InvoiceReturnItemId] ON [dbo].[InvoiceReturnReceiptItems] ([InvoiceReturnItemId]);

    CREATE INDEX [IX_InvoiceReturnReceiptItems_ProductId] ON [dbo].[InvoiceReturnReceiptItems] ([ProductId]);

    CREATE UNIQUE INDEX [IX_InvoiceReturnReceiptItems_ReceiptId_InvoiceReturnItemId] ON [dbo].[InvoiceReturnReceiptItems] ([ReceiptId], [InvoiceReturnItemId]);

    CREATE INDEX [IX_InvoiceReturnReceiptItems_WarehouseId] ON [dbo].[InvoiceReturnReceiptItems] ([WarehouseId]);

    CREATE UNIQUE INDEX [IX_InvoiceReturnReceipts_CreditNoteNumber] ON [dbo].[InvoiceReturnReceipts] ([CreditNoteNumber]);

    CREATE INDEX [IX_InvoiceReturnReceipts_CustomerId_CreatedAt] ON [dbo].[InvoiceReturnReceipts] ([CustomerId], [CreatedAt]);

    CREATE INDEX [IX_InvoiceReturnReceipts_InvoiceId] ON [dbo].[InvoiceReturnReceipts] ([InvoiceId]);

    CREATE INDEX [IX_InvoiceReturnReceipts_InvoiceReturnId] ON [dbo].[InvoiceReturnReceipts] ([InvoiceReturnId]);

    CREATE UNIQUE INDEX [IX_InvoiceReturnReceipts_OperationId] ON [dbo].[InvoiceReturnReceipts] ([OperationId]);

    CREATE UNIQUE INDEX [IX_InvoiceReturnReceipts_ReceiptNumber] ON [dbo].[InvoiceReturnReceipts] ([ReceiptNumber]);

    CREATE INDEX [IX_InvoiceReturnReceipts_SalesRepId] ON [dbo].[InvoiceReturnReceipts] ([SalesRepId]);

    -- Record completion only after ALL DDL succeeded in this same transaction.
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20261026194614_AddReturnReceivingCreditLedger', N'9.0.15');
    COMMIT TRANSACTION;
    SELECT N'Schema completed; existing invoice credit values and sessions were preserved.' AS Result;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

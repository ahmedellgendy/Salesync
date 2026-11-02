-- REVIEW ONLY: not executed by this task. Requires separate SQL approval.
-- Prerequisite: the previously deployed operation-execution migration.
-- No historical returns are backfilled; invoice credit defaults to zero.
SET XACT_ABORT ON;
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261021082158_AddOperationExecutionReceipts')
    THROW 51000, N'Apply/reconcile the prerequisite migration before this package.', 1;

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026194614_AddReturnReceivingCreditLedger'
)
BEGIN
    ALTER TABLE [Invoices] ADD [CreditAppliedAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026194614_AddReturnReceivingCreditLedger'
)
BEGIN
    CREATE TABLE [InvoiceReturnReceipts] (
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
        CONSTRAINT [FK_InvoiceReturnReceipts_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InvoiceReturnReceipts_InvoiceReturns_InvoiceReturnId] FOREIGN KEY ([InvoiceReturnId]) REFERENCES [InvoiceReturns] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InvoiceReturnReceipts_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoices] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InvoiceReturnReceipts_SalesReps_SalesRepId] FOREIGN KEY ([SalesRepId]) REFERENCES [SalesReps] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026194614_AddReturnReceivingCreditLedger'
)
BEGIN
    CREATE TABLE [InvoiceReturnReceiptItems] (
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
        CONSTRAINT [FK_InvoiceReturnReceiptItems_InvoiceReturnItems_InvoiceReturnItemId] FOREIGN KEY ([InvoiceReturnItemId]) REFERENCES [InvoiceReturnItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InvoiceReturnReceiptItems_InvoiceReturnReceipts_ReceiptId] FOREIGN KEY ([ReceiptId]) REFERENCES [InvoiceReturnReceipts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InvoiceReturnReceiptItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InvoiceReturnReceiptItems_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026194614_AddReturnReceivingCreditLedger'
)
BEGIN
    CREATE INDEX [IX_InvoiceReturnReceiptItems_InvoiceReturnItemId] ON [InvoiceReturnReceiptItems] ([InvoiceReturnItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026194614_AddReturnReceivingCreditLedger'
)
BEGIN
    CREATE INDEX [IX_InvoiceReturnReceiptItems_ProductId] ON [InvoiceReturnReceiptItems] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026194614_AddReturnReceivingCreditLedger'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InvoiceReturnReceiptItems_ReceiptId_InvoiceReturnItemId] ON [InvoiceReturnReceiptItems] ([ReceiptId], [InvoiceReturnItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026194614_AddReturnReceivingCreditLedger'
)
BEGIN
    CREATE INDEX [IX_InvoiceReturnReceiptItems_WarehouseId] ON [InvoiceReturnReceiptItems] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026194614_AddReturnReceivingCreditLedger'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InvoiceReturnReceipts_CreditNoteNumber] ON [InvoiceReturnReceipts] ([CreditNoteNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026194614_AddReturnReceivingCreditLedger'
)
BEGIN
    CREATE INDEX [IX_InvoiceReturnReceipts_CustomerId_CreatedAt] ON [InvoiceReturnReceipts] ([CustomerId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026194614_AddReturnReceivingCreditLedger'
)
BEGIN
    CREATE INDEX [IX_InvoiceReturnReceipts_InvoiceId] ON [InvoiceReturnReceipts] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026194614_AddReturnReceivingCreditLedger'
)
BEGIN
    CREATE INDEX [IX_InvoiceReturnReceipts_InvoiceReturnId] ON [InvoiceReturnReceipts] ([InvoiceReturnId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026194614_AddReturnReceivingCreditLedger'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InvoiceReturnReceipts_OperationId] ON [InvoiceReturnReceipts] ([OperationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026194614_AddReturnReceivingCreditLedger'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InvoiceReturnReceipts_ReceiptNumber] ON [InvoiceReturnReceipts] ([ReceiptNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026194614_AddReturnReceivingCreditLedger'
)
BEGIN
    CREATE INDEX [IX_InvoiceReturnReceipts_SalesRepId] ON [InvoiceReturnReceipts] ([SalesRepId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026194614_AddReturnReceivingCreditLedger'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261026194614_AddReturnReceivingCreditLedger', N'9.0.15');
END;

COMMIT;
GO


BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021082158_AddOperationExecutionReceipts'
)
BEGIN
    CREATE TABLE [OperationExecutions] (
        [Id] int NOT NULL IDENTITY,
        [ActorId] nvarchar(450) NOT NULL,
        [OperationKey] nvarchar(36) NOT NULL,
        [RequestHash] nvarchar(64) NOT NULL,
        [Operation] nvarchar(240) NOT NULL,
        [SalesRepId] int NULL,
        [ResourceId] int NULL,
        [ResponseJson] nvarchar(max) NOT NULL,
        [ResponseStatusCode] int NOT NULL,
        [CompletedAt] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_OperationExecutions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021082158_AddOperationExecutionReceipts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_OperationExecutions_ActorId_OperationKey] ON [OperationExecutions] ([ActorId], [OperationKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021082158_AddOperationExecutionReceipts'
)
BEGIN
    CREATE INDEX [IX_OperationExecutions_SalesRepId_CompletedAt] ON [OperationExecutions] ([SalesRepId], [CompletedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021082158_AddOperationExecutionReceipts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261021082158_AddOperationExecutionReceipts', N'9.0.15');
END;

COMMIT;
GO


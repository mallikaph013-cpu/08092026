BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819084733_AddSecondThirdApproverDepartments'
)
BEGIN
    ALTER TABLE [RepairTickets] ADD [SecondApproverDepartment] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819084733_AddSecondThirdApproverDepartments'
)
BEGIN
    ALTER TABLE [RepairTickets] ADD [ThirdApproverDepartment] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819084733_AddSecondThirdApproverDepartments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260819084733_AddSecondThirdApproverDepartments', N'9.0.4');
END;

COMMIT;
GO


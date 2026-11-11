USE [RutaViva];
GO

SET NOCOUNT ON;

DECLARE @Services TABLE (Id INT IDENTITY(1, 1) PRIMARY KEY, SchemaName SYSNAME NOT NULL, AppPassword NVARCHAR(128) NOT NULL);

INSERT INTO @Services (SchemaName, AppPassword)
VALUES
    (N'customers', N'$(CustomersAppPassword)'),
    (N'workshops', N'$(WorkshopsAppPassword)'),
    (N'fleet', N'$(FleetAppPassword)'),
    (N'maintenance', N'$(MaintenanceAppPassword)'),
    (N'serviceorders', N'$(ServiceOrdersAppPassword)'),
    (N'imports', N'$(ImportsAppPassword)'),
    (N'reporting', N'$(ReportingAppPassword)'),
    (N'eventbus', N'$(EventBusAppPassword)');

DECLARE @Total INT = (SELECT COUNT(*) FROM @Services);
DECLARE @Index INT = 1;
DECLARE @SchemaName SYSNAME;
DECLARE @Password NVARCHAR(128);
DECLARE @Principal SYSNAME;
DECLARE @Sql NVARCHAR(MAX);

WHILE @Index <= @Total
BEGIN
    SELECT @SchemaName = SchemaName FROM @Services WHERE Id = @Index;

    IF SCHEMA_ID(@SchemaName) IS NULL
    BEGIN
        SET @Sql = N'CREATE SCHEMA ' + QUOTENAME(@SchemaName) + N';';
        EXEC (@Sql);
    END;

    SET @Index += 1;
END;

SET @Index = 1;

WHILE @Index <= @Total
BEGIN
    SELECT @SchemaName = SchemaName, @Password = AppPassword FROM @Services WHERE Id = @Index;
    SET @Principal = N'rutaviva_' + @SchemaName + N'_app';

    IF SUSER_ID(@Principal) IS NULL
    BEGIN
        SET @Sql = N'CREATE LOGIN ' + QUOTENAME(@Principal) + N' WITH PASSWORD = ' + QUOTENAME(@Password, '''') + N', CHECK_POLICY = OFF;';
        EXEC (@Sql);
    END;

    IF DATABASE_PRINCIPAL_ID(@Principal) IS NULL
    BEGIN
        SET @Sql = N'CREATE USER ' + QUOTENAME(@Principal) + N' FOR LOGIN ' + QUOTENAME(@Principal) + N' WITH DEFAULT_SCHEMA = ' + QUOTENAME(@SchemaName) + N';';
        EXEC (@Sql);
    END;

    SET @Sql = N'GRANT SELECT, INSERT, UPDATE, DELETE, EXECUTE ON SCHEMA::' + QUOTENAME(@SchemaName) + N' TO ' + QUOTENAME(@Principal) + N';';
    EXEC (@Sql);

    SELECT @Sql = STRING_AGG(CONVERT(NVARCHAR(MAX), N'DENY SELECT, INSERT, UPDATE, DELETE, EXECUTE ON SCHEMA::' + QUOTENAME(SchemaName) + N' TO ' + QUOTENAME(@Principal) + N';'), N' ')
    FROM @Services
    WHERE SchemaName <> @SchemaName;
    EXEC (@Sql);

    SET @Index += 1;
END;
GO

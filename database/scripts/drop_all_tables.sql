-- ============================================================================
-- VeggieLifeDb - Drop All Tables Script (Clean Reset)
-- Description: Drops all Foreign Key constraints then drops all User Tables.
-- CAUTION: This will delete all tables and data in VeggieLifeDb.
-- ============================================================================

USE [VeggieLifeDb];
GO

-- 1. Drop all Foreign Key Constraints
DECLARE @sqlDropFK NVARCHAR(MAX) = N'';
SELECT @sqlDropFK += N'ALTER TABLE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) 
                   + N' DROP CONSTRAINT ' + QUOTENAME(f.name) + N';' + CHAR(13)
FROM sys.foreign_keys f
INNER JOIN sys.tables t ON f.parent_object_id = t.object_id
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id;

IF LEN(@sqlDropFK) > 0
BEGIN
    EXEC sp_executesql @sqlDropFK;
    PRINT 'All foreign key constraints have been dropped.';
END;
GO

-- 2. Drop all User Tables
DECLARE @sqlDropTables NVARCHAR(MAX) = N'';
SELECT @sqlDropTables += N'DROP TABLE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N';' + CHAR(13)
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id;

IF LEN(@sqlDropTables) > 0
BEGIN
    EXEC sp_executesql @sqlDropTables;
    PRINT 'All tables have been dropped.';
END;
GO

PRINT 'VeggieLifeDb is now clean and empty.';
GO

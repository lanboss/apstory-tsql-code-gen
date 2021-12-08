/* ====================Testing==================

exec [*gen_ApstoryStoredProcGeneratorCreateAll]

*/

CREATE   PROCEDURE [dbo].[*gen_ApstoryStoredProcGeneratorCreateAll]	
	@Schema NVARCHAR(20) = 'dbo'
AS
BEGIN

DECLARE @DBNAME nvarchar(100);

-- Get DBNAME
SELECT @DBNAME = DB_NAME();

EXEC [*gen_ApstoryCreateExcludeSchemaTableName]

EXEC [*gen_ApstoryStoredProcGeneratorDropAll] @Schema

EXEC [*gen_ApstoryCreateUserDefinedTableType] @Schema

DECLARE TableList Cursor LOCAL FOR
SELECT TABLE_SCHEMA,TABLE_NAME FROM INFORMATION_SCHEMA.TABLES 
WHERE 
TABLE_TYPE='BASE TABLE' 
AND TABLE_SCHEMA = @Schema
AND TABLE_NAME NOT IN 
(
	SELECT TableName FROM dbo.[*gen_ExcludeSchemaTableName] WHERE [Schema] = @Schema
)

DECLARE @TableSchema varchar(100), @cTableName varchar(100);

-- open the cursor
OPEN TableList

-- get the first row of cursor into variables
FETCH NEXT FROM TableList INTO @TableSchema, @cTableName

WHILE @@FETCH_STATUS = 0
    BEGIN
        
		PRINT 'Create stored procs for table ' + @cTableName + CHAR(13) + CHAR(10)  
        DECLARE @QueryToExec nvarchar(max)
		EXECUTE [dbo].[*gen_ApstoryStoredProcGeneratorDelSft] @cTableName, @Schema, @QueryToExec OUTPUT
		EXEC (@QueryToExec)
		EXECUTE [dbo].[*gen_ApstoryStoredProcGeneratorInsUpd] @cTableName, @Schema, @QueryToExec OUTPUT
		EXEC (@QueryToExec)
		EXECUTE [dbo].[*gen_ApstoryStoredProcGeneratorDelHrd] @cTableName, @Schema, @QueryToExec OUTPUT
		EXEC (@QueryToExec)
		EXECUTE [dbo].[*gen_ApstoryStoredProcGeneratorGetById] @cTableName, @Schema, @QueryToExec OUTPUT
		EXEC (@QueryToExec)
		EXECUTE [dbo].[*gen_ApstoryStoredProcGeneratorGetByIds] @cTableName, @Schema, @QueryToExec OUTPUT
		EXEC (@QueryToExec)
		EXECUTE [dbo].[*gen_ApstoryStoredProcGeneratorGetByIdsPaging] @cTableName, @Schema, @QueryToExec OUTPUT
		EXEC (@QueryToExec)
		EXECUTE [dbo].[*gen_ApstoryStoredProcGeneratorGetByNameIds] @cTableName, @Schema, @QueryToExec OUTPUT
		EXEC (@QueryToExec)

        FETCH NEXT FROM TableList INTO @TableSchema, @cTableName
    END;

-- ----------------
-- clean up cursor
-- ----------------
CLOSE TableList;
DEALLOCATE TableList;

DECLARE TableList Cursor LOCAL FOR
SELECT TABLE_SCHEMA,TABLE_NAME FROM INFORMATION_SCHEMA.TABLES 
WHERE 
TABLE_TYPE='BASE TABLE' 
AND TABLE_SCHEMA = @Schema
AND TABLE_NAME NOT IN 
(
	SELECT TableName FROM dbo.[*gen_ExcludeSchemaTableName] WHERE [Schema] = @Schema
)

SELECT sc.[name] as TABLE_SCHEMA, t.name AS TABLE_NAME
FROM sys.tables t 
INNER JOIN sys.schemas sc ON t.schema_id = sc.schema_id
INNER JOIN  sys.fulltext_indexes fi ON t.[object_id] = fi.[object_id] 
INNER JOIN sys.fulltext_index_columns ic ON ic.[object_id] = t.[object_id]
INNER JOIN sys.columns cl ON ic.column_id = cl.column_id AND ic.[object_id] = cl.[object_id]
WHERE sc.[name] = @Schema

-- open the cursor
OPEN TableList

-- get the first row of cursor into variables
FETCH NEXT FROM TableList INTO @TableSchema, @cTableName

WHILE @@FETCH_STATUS = 0
    BEGIN
        
		PRINT 'Create search contains stored procs for table ' + @cTableName + CHAR(13) + CHAR(10)          
		EXECUTE [dbo].[*gen_ApstoryStoredProcGeneratorGetBySearch] @cTableName, @Schema, @QueryToExec OUTPUT
		EXEC (@QueryToExec)		

		PRINT 'Create search free text stored procs for table ' + @cTableName + CHAR(13) + CHAR(10)          
		EXECUTE [dbo].[*gen_ApstoryStoredProcGeneratorGetBySearchFreeText] @cTableName, @Schema, @QueryToExec OUTPUT
		EXEC (@QueryToExec)

        FETCH NEXT FROM TableList INTO @TableSchema, @cTableName
    END;

-- ----------------
-- clean up cursor
-- ----------------
CLOSE TableList;
DEALLOCATE TableList;

DECLARE TableList Cursor LOCAL FOR
SELECT sc.[name] as TABLE_SCHEMA, t.name AS TABLE_NAME
FROM sys.indexes ind 
INNER JOIN sys.index_columns ic ON  ind.object_id = ic.object_id and ind.index_id = ic.index_id 
INNER JOIN sys.columns col ON ic.object_id = col.object_id and ic.column_id = col.column_id 
INNER JOIN sys.tables t ON ind.object_id = t.object_id 
INNER JOIN sys.schemas sc ON t.schema_id = sc.schema_id AND sc.[name] = @Schema
WHERE ind.is_primary_key = 0 AND ind.is_unique_constraint = 0 AND t.is_ms_shipped = 0

-- open the cursor
OPEN TableList

-- get the first row of cursor into variables
FETCH NEXT FROM TableList INTO @TableSchema, @cTableName

WHILE @@FETCH_STATUS = 0
    BEGIN
        
		PRINT 'Create index stored procs for table ' + @cTableName + CHAR(13) + CHAR(10)          
		EXECUTE [dbo].[*gen_ApstoryStoredProcGeneratorGetByIndex] @cTableName, @Schema, @QueryToExec OUTPUT
		EXEC (@QueryToExec)		

        FETCH NEXT FROM TableList INTO @TableSchema, @cTableName
    END;

-- ----------------
-- clean up cursor
-- ----------------
CLOSE TableList;
DEALLOCATE TableList;
	
END

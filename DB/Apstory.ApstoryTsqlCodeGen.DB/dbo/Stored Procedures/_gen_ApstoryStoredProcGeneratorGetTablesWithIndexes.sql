/* ====================Testing==================

exec [*gen_ApstoryStoredProcGeneratorGetTablesWithIndexes]

*/

CREATE   PROCEDURE [dbo].[*gen_ApstoryStoredProcGeneratorGetTablesWithIndexes]	
	@Schema NVARCHAR(20) = 'dbo'
AS
BEGIN

	SELECT sc.[name] as TABLE_SCHEMA, t.name AS TABLE_NAME, col.name as COLUMN_NAME
	FROM sys.indexes ind 
	INNER JOIN sys.index_columns ic ON  ind.object_id = ic.object_id and ind.index_id = ic.index_id 
	INNER JOIN sys.columns col ON ic.object_id = col.object_id and ic.column_id = col.column_id 
	INNER JOIN sys.tables t ON ind.object_id = t.object_id 
	INNER JOIN sys.schemas sc ON t.schema_id = sc.schema_id
	WHERE ind.is_primary_key = 0 AND ind.is_unique_constraint = 0 AND t.is_ms_shipped = 0 AND sc.[name] = @Schema AND ind.is_unique = 1
	
END
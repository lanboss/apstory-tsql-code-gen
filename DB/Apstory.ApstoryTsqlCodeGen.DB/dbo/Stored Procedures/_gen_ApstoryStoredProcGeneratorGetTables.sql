

/* ====================Testing==================

exec [*gen_ApstoryStoredProcGeneratorGetTables]

*/

CREATE   PROCEDURE [dbo].[*gen_ApstoryStoredProcGeneratorGetTables]	
	@Schema nvarchar(20) = 'dbo'
AS
BEGIN

	SELECT TABLE_SCHEMA,TABLE_NAME FROM INFORMATION_SCHEMA.TABLES 	 
	WHERE TABLE_TYPE='BASE TABLE' and TABLE_SCHEMA = @Schema 
	AND TABLE_NAME NOT IN 
	(
		SELECT TableName FROM dbo.[*gen_ExcludeSchemaTableName] WHERE [Schema] = @Schema
	)
END

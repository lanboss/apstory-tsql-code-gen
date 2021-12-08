CREATE   PROCEDURE [dbo].[*gen_ApstoryStoredProcGeneratorGetGenSPs]	
AS
BEGIN
	SELECT mod.definition FROM sys.procedures pr
	INNER JOIN sys.sql_modules mod ON pr.object_id = mod.object_id
	WHERE pr.is_ms_shipped = 0 AND pr.name LIKE '*gen%'
END

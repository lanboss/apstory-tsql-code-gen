/* ====================Testing==================

exec [*gen_ApstoryStoredProcGeneratorDropAll]

*/

CREATE   PROCEDURE [dbo].[*gen_ApstoryStoredProcGeneratorDropAll]	
	@Schema NVARCHAR(20) = 'dbo'
AS
BEGIN

DECLARE @DBNAME nvarchar(100);

-- Get DBNAME
SELECT @DBNAME = DB_NAME();

DECLARE StoredProcedureList Cursor LOCAL FOR
SELECT name
FROM sys.procedures
where name like 'zgen%'
and SCHEMA_NAME(schema_id) = @Schema

DECLARE @StoredProcedureName varchar(500);

-- open the cursor
OPEN StoredProcedureList

-- get the first row of cursor into variables
FETCH NEXT FROM StoredProcedureList INTO @StoredProcedureName

WHILE @@FETCH_STATUS = 0
    BEGIN
        
		PRINT 'Drop stored procedure: ' +  @Schema + '.' + @StoredProcedureName + CHAR(13) + CHAR(10);    
		DECLARE @QueryToExec nvarchar(max) = 'DROP PROCEDURE ' + @Schema + '.' + @StoredProcedureName
		EXECUTE sp_executesql @QueryToExec
        FETCH NEXT FROM StoredProcedureList INTO @StoredProcedureName
    END;

-- ----------------
-- clean up cursor
-- ----------------
CLOSE StoredProcedureList;
DEALLOCATE StoredProcedureList;   
	
END
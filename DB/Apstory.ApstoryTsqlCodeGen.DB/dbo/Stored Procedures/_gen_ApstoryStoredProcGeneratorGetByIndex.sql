
/* ====================Testing==================

declare @QueryToExec nvarchar(MAX)
exec [*gen_ApstoryStoredProcGeneratorGetByIndex] 'UserMobileNumber', @QueryToExec output
select @QueryToExec
*/

CREATE   PROCEDURE [dbo].[*gen_ApstoryStoredProcGeneratorGetByIndex]
	-- Add the parameters for the stored procedure here
	@TableName nvarchar(255),
	@Schema nvarchar(20) = 'dbo',
	@QueryToExec nvarchar(MAX) output
AS
BEGIN

DECLARE @OrderByDir nvarchar(4)=N'ASC';
DECLARE @ISACTIVE_SEL bit = 1; --Set to 1 if your table has a Bit field named IsActive
DECLARE @HASINDEX bit = 0;
DECLARE @NNND char(23) ='NOT_NULLABLE_NO_DEFAULT';
DECLARE @NNWD char(22) ='NOT_NULLABLE_W_DEFAULT';
DECLARE @NBLE char(8) ='NULLABLE';
DECLARE @LEGEND nvarchar(max);
DECLARE @PRIMARY_KEY nvarchar(100);
DECLARE @DBNAME nvarchar(100);

-- Get DBNAME
SELECT @DBNAME = DB_NAME();

--Set up Legend     
    SET @LEGEND = N'-- ===================================================================' + CHAR(13) + CHAR(10)         

--Get Primary Key Field
SELECT TOP 1 @PRIMARY_KEY = COLUMN_NAME 
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
WHERE OBJECTPROPERTY(OBJECT_ID(@Schema + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') = 1 AND TABLE_NAME = @TableName AND TABLE_CATALOG = @DBName AND TABLE_SCHEMA = @Schema;

--Check if tabe has table had index
SELECT @HASINDEX = count(*)
FROM sys.indexes ind 
INNER JOIN sys.index_columns ic ON  ind.object_id = ic.object_id and ind.index_id = ic.index_id 
INNER JOIN sys.columns col ON ic.object_id = col.object_id and ic.column_id = col.column_id 
INNER JOIN sys.tables t ON ind.object_id = t.object_id 
INNER JOIN sys.schemas sc ON t.schema_id = sc.schema_id
WHERE ind.is_primary_key = 0 AND ind.is_unique_constraint = 0 AND t.is_ms_shipped = 0
AND t.name = @TableName AND sc.[name] = @Schema

IF (@HASINDEX > 0)
BEGIN

DECLARE TableCol Cursor FOR
SELECT c.TABLE_SCHEMA, c.TABLE_NAME, c.COLUMN_NAME, c.DATA_TYPE, c.CHARACTER_MAXIMUM_LENGTH
    , IIF(c.COLUMN_NAME='RowVersion',@NBLE,IIF(c.COLUMN_NAME=@PRIMARY_KEY,@NBLE,
	IIF(c.IS_NULLABLE = 'NO' AND c.COLUMN_DEFAULT IS NULL,@NNND,IIF(c.IS_NULLABLE = 'NO' AND 
	c.COLUMN_DEFAULT IS NOT NULL,@NNWD,@NBLE)))) AS [NULLABLE_TYPE]
FROM INFORMATION_SCHEMA.COLUMNS c INNER JOIN
     INFORMATION_SCHEMA.TABLES t ON c.TABLE_NAME = t.TABLE_NAME AND c.TABLE_SCHEMA = t.TABLE_SCHEMA INNER JOIN 
	 (
		SELECT t.name AS TableName, col.name as ColumnName
		FROM sys.indexes ind 
		INNER JOIN sys.index_columns ic ON  ind.object_id = ic.object_id and ind.index_id = ic.index_id 
		INNER JOIN sys.columns col ON ic.object_id = col.object_id and ic.column_id = col.column_id 
		INNER JOIN sys.tables t ON ind.object_id = t.object_id 
		INNER JOIN sys.schemas sc ON t.schema_id = sc.schema_id
		WHERE ind.is_primary_key = 0 AND ind.is_unique_constraint = 0 AND t.is_ms_shipped = 0
		AND t.name = @TableName AND sc.[name] = @Schema
	) ti ON (ti.TableName = c.TABLE_NAME AND ti.ColumnName = c.COLUMN_NAME)
WHERE t.TABLE_CATALOG = @DBName
    AND t.TABLE_TYPE = 'BASE TABLE'
    AND t.TABLE_NAME = @TableName
	AND t.TABLE_SCHEMA = @Schema
ORDER BY [NULLABLE_TYPE], c.ORDINAL_POSITION;

DECLARE @TableSchema varchar(100), @cTableName varchar(100), @ColumnName varchar(100);
DECLARE @DataType varchar(30), @CharLength int, @NullableType varchar(30);
DECLARE @PrimaryKeyDataType varchar(30);
DECLARE @GetTransactionScope nvarchar(MAX)
DECLARE @PARAMETERS nvarchar(max);
DECLARE @CONTAINS nvarchar(max);
DECLARE @INSERT_FIELDS nvarchar(max),@INSERT_VALUES nvarchar(max);
DECLARE @UPDATE_VALUES nvarchar(max);

SET @PARAMETERS ='';
SET @INSERT_FIELDS ='';
SET @INSERT_VALUES ='';
SET @UPDATE_VALUES ='';

SET @GetTransactionScope = N'  SET NOCOUNT ON;' + CHAR(13) + CHAR(10)
SET @GetTransactionScope = @GetTransactionScope + N'  SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10)

-- open the cursor
OPEN TableCol

-- get the first row of cursor into variables
FETCH NEXT FROM TableCol INTO @TableSchema, @cTableName, @ColumnName, @DataType, @CharLength, @NullableType

WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @ColumnName NOT IN('Created','CreatedBy','Modified','ModifiedBy')
        BEGIN			
            SET @PARAMETERS=@PARAMETERS + '@' + IIF(@ColumnName=@PRIMARY_KEY,@ColumnName,@ColumnName) + ' ' + iif(@CharLength IS NULL,@DataType,@DataType + '(' + 
                CAST(@CharLength AS nvarchar(10)) + ')') +  IIF(@NullableType=@NNND OR @NullableType=@NNWD,',','=NULL,');
            IF @ColumnName <> @PRIMARY_KEY
                BEGIN
                    SET @INSERT_FIELDS=@INSERT_FIELDS + '[' + @ColumnName + '],';
                    SET @INSERT_VALUES=@INSERT_VALUES + '@' + IIF(@ColumnName=@PRIMARY_KEY,@ColumnName,@ColumnName) + ',';
                    SET @UPDATE_VALUES=@UPDATE_VALUES + '[' + @ColumnName + ']=@' + IIF(@ColumnName=@PRIMARY_KEY,@ColumnName,@ColumnName) + ',';					
                END
			ELSE
				BEGIN
					SET @PrimaryKeyDataType = @DataType;
				END	
        END		

        FETCH NEXT FROM TableCol INTO @TableSchema, @cTableName, @ColumnName, @DataType, @CharLength, @NullableType
    END;
	
	SET @PARAMETERS=LEFT(@PARAMETERS,LEN(@PARAMETERS)-1)		

-- ----------------
-- clean up cursor
-- ----------------
CLOSE TableCol;
DEALLOCATE TableCol;

    --Gen Select Statement
	IF (@HASINDEX > 0)
	BEGIN
		SET @QueryToExec = N'/****** Object:  StoredProcedure [' + @Schema + '].[zgen_' + @TableName + '_GetBy' + @ColumnName + '] ******/' + CHAR(13) + CHAR(10) 
		SET @QueryToExec = @QueryToExec +  @LEGEND;
		SET @QueryToExec = @QueryToExec +  N'-- Description    : Select By ' + @ColumnName + ' ' + @TableName + CHAR(13) + CHAR(10) 
		SET @QueryToExec = @QueryToExec +  N'-- ===================================================================' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10)     
		SET @QueryToExec = @QueryToExec +  N'CREATE OR ALTER PROCEDURE [' + @Schema + '].[zgen_' + @TableName  + '_GetBy' + @ColumnName + ']' + CHAR(13) + CHAR(10);
		SET @QueryToExec = @QueryToExec +  N'  (' + @PARAMETERS + ')' + CHAR(13) + CHAR(10);
		SET @QueryToExec = @QueryToExec +  N'AS' + CHAR(13) + CHAR(10) 
		SET @QueryToExec = @QueryToExec +  N'BEGIN' + CHAR(13) + CHAR(10)
		SET @QueryToExec = @QueryToExec +  @GetTransactionScope;   
		SET @QueryToExec = @QueryToExec +  N'  SELECT * FROM [' + @Schema + '].[' + @TableName + '] WHERE ' + @ColumnName + ' = @' + @ColumnName + CHAR(13) + CHAR(10) 		
		SET @QueryToExec = @QueryToExec +  N'END' + CHAR(13) + CHAR(10)     
		SET @QueryToExec = @QueryToExec +  CHAR(13) + CHAR(10)
	END
	
END

END

/* ====================Testing==================

declare @QueryToExec nvarchar(MAX)
exec [*gen_ApstoryStoredProcGeneratorGetBySearchFreeText] 'Company', @QueryToExec output
select @QueryToExec
*/

CREATE   PROCEDURE [dbo].[*gen_ApstoryStoredProcGeneratorGetBySearchFreeText]
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
FROM sys.tables t 
INNER JOIN sys.schemas sc ON t.schema_id = sc.schema_id
INNER JOIN  sys.fulltext_indexes fi ON t.[object_id] = fi.[object_id] 
INNER JOIN sys.fulltext_index_columns ic ON ic.[object_id] = t.[object_id]
INNER JOIN sys.columns cl ON ic.column_id = cl.column_id AND ic.[object_id] = cl.[object_id]
where t.name = @TableName AND sc.[name] = @Schema

DECLARE TableCol Cursor FOR
SELECT c.TABLE_SCHEMA, c.TABLE_NAME, c.COLUMN_NAME, c.DATA_TYPE, c.CHARACTER_MAXIMUM_LENGTH
    , IIF(c.COLUMN_NAME='RowVersion',@NBLE,IIF(c.COLUMN_NAME=@PRIMARY_KEY,@NBLE,
	IIF(c.IS_NULLABLE = 'NO' AND c.COLUMN_DEFAULT IS NULL,@NNND,IIF(c.IS_NULLABLE = 'NO' AND 
	c.COLUMN_DEFAULT IS NOT NULL,@NNWD,@NBLE)))) AS [NULLABLE_TYPE], ft.ColumnName as [FT_INDEX]
FROM INFORMATION_SCHEMA.COLUMNS c INNER JOIN
     INFORMATION_SCHEMA.TABLES t ON c.TABLE_NAME = t.TABLE_NAME AND c.TABLE_SCHEMA = t.TABLE_SCHEMA INNER JOIN 
	 (
		SELECT t.name AS TableName, cl.name AS ColumnName
		FROM sys.tables t 
		INNER JOIN sys.schemas sc on t.schema_id = sc.schema_id
		INNER JOIN  sys.fulltext_indexes fi ON t.[object_id] = fi.[object_id] 
		INNER JOIN sys.fulltext_index_columns ic ON ic.[object_id] = t.[object_id]
		INNER JOIN sys.columns cl ON ic.column_id = cl.column_id AND ic.[object_id] = cl.[object_id]
		where t.name = @TableName AND sc.[name] = @Schema
	) ft ON (ft.TableName = c.TABLE_NAME AND ft.ColumnName = c.COLUMN_NAME)
WHERE t.TABLE_CATALOG = @DBName
    AND t.TABLE_TYPE = 'BASE TABLE'
    AND t.TABLE_NAME = @TableName
	AND t.TABLE_SCHEMA = @Schema
ORDER BY [NULLABLE_TYPE], c.ORDINAL_POSITION;

DECLARE @TableSchema varchar(100), @cTableName varchar(100), @ColumnName varchar(100);
DECLARE @DataType varchar(30), @CharLength int, @NullableType varchar(30);
DECLARE @FTIndex varchar(100)
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
SET @CONTAINS = ''

SET @GetTransactionScope = N'  SET NOCOUNT ON;' + CHAR(13) + CHAR(10)
SET @GetTransactionScope = @GetTransactionScope + N'  SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10)

-- open the cursor
OPEN TableCol

-- get the first row of cursor into variables
FETCH NEXT FROM TableCol INTO @TableSchema, @cTableName, @ColumnName, @DataType, @CharLength, @NullableType, @FTIndex

WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @ColumnName NOT IN('Created','CreatedBy','Modified','ModifiedBy')
        BEGIN
			SET @CONTAINS=@CONTAINS + 'FREETEXT(' + @ColumnName + ',@SearchCriteria) OR '
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

        FETCH NEXT FROM TableCol INTO @TableSchema, @cTableName, @ColumnName, @DataType, @CharLength, @NullableType, @FTIndex
    END;    

	IF (@CONTAINS <> '')
	BEGIN
		SET @CONTAINS=LEFT(@CONTAINS,LEN(@CONTAINS)-3)
	END

-- ----------------
-- clean up cursor
-- ----------------
CLOSE TableCol;
DEALLOCATE TableCol;

    --Gen Select Statement
	IF (@HASINDEX > 0)
	BEGIN
		SET @QueryToExec = N'/****** Object:  StoredProcedure [' + @Schema + '].[zgen_' + @TableName + '_GetBySearchFreeText] ******/' + CHAR(13) + CHAR(10) 
		SET @QueryToExec = @QueryToExec +  @LEGEND;
		SET @QueryToExec = @QueryToExec +  N'-- Description    : Select By Search ' + @TableName + CHAR(13) + CHAR(10) 
		SET @QueryToExec = @QueryToExec +  N'-- ===================================================================' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10)     
		SET @QueryToExec = @QueryToExec +  N'CREATE OR ALTER PROCEDURE [' + @Schema + '].[zgen_' + @TableName  + '_GetBySearchFreeText]' + CHAR(13) + CHAR(10);
		SET @QueryToExec = @QueryToExec +  N'  (@SearchCriteria nvarchar(255)' + IIF(@ISACTIVE_SEL = 1,', @IsActive bit=NULL','') + ')' + CHAR(13) + CHAR(10);
		SET @QueryToExec = @QueryToExec +  N'AS' + CHAR(13) + CHAR(10) 
		SET @QueryToExec = @QueryToExec +  N'BEGIN' + CHAR(13) + CHAR(10)
		SET @QueryToExec = @QueryToExec +  @GetTransactionScope;   
		SET @QueryToExec = @QueryToExec +  N'  IF (@SearchCriteria <> '''' OR @SearchCriteria IS NOT NULL)' + CHAR(13) + CHAR(10)     
		SET @QueryToExec = @QueryToExec +  N'  BEGIN' + CHAR(13) + CHAR(10) 
		SET @QueryToExec = @QueryToExec +  N'    SET @SearchCriteria = ''"'' + @SearchCriteria + ''*"''' + CHAR(13) + CHAR(10)         
		SET @QueryToExec = @QueryToExec +  N'  END' + CHAR(13) + CHAR(10) 
		SET @QueryToExec = @QueryToExec +  N'  ELSE' + CHAR(13) + CHAR(10) 
		SET @QueryToExec = @QueryToExec +  N'  BEGIN' + CHAR(13) + CHAR(10)
		SET @QueryToExec = @QueryToExec +  N'    SET @SearchCriteria = ''""''' + CHAR(13) + CHAR(10) 
		SET @QueryToExec = @QueryToExec +  N'  END' + CHAR(13) + CHAR(10) 
		SET @QueryToExec = @QueryToExec +  N'' + CHAR(13) + CHAR(10) 
		SET @QueryToExec = @QueryToExec +  N'  SELECT * FROM [' + @Schema + '].[' + @TableName + '] WHERE IsActive = @IsActive' + CHAR(13) + CHAR(10) 
		SET @QueryToExec = @QueryToExec +  N'  AND (' + @CONTAINS + ')' + CHAR(13) + CHAR(10)	
		SET @QueryToExec = @QueryToExec +  N'END' + CHAR(13) + CHAR(10)     
		SET @QueryToExec = @QueryToExec +  CHAR(13) + CHAR(10)
	END
	
END
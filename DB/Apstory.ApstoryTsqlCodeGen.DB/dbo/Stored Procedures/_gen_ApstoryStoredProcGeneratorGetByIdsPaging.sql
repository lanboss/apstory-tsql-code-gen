/* ====================Testing==================

declare @QueryToExec nvarchar(MAX)
exec [*gen_ApstoryStoredProcGeneratorGetByIdsPaging] 'UserNotification','dbo',@QueryToExec output
select @QueryToExec
*/

CREATE   PROCEDURE [dbo].[*gen_ApstoryStoredProcGeneratorGetByIdsPaging]
	-- Add the parameters for the stored procedure here
	@TableName nvarchar(255),
	@Schema nvarchar(20) = 'dbo',
	@QueryToExec nvarchar(MAX) output
AS
BEGIN

	DECLARE @OrderByDir nvarchar(4)=N'ASC';
	DECLARE @ISACTIVE_SEL bit = 1; --Set to 1 if your table has a Bit field named IsActive
	DECLARE @NNND char(23) ='NOT_NULLABLE_NO_DEFAULT';
	DECLARE @NNWD char(22) ='NOT_NULLABLE_W_DEFAULT';
	DECLARE @NBLE char(8) ='NULLABLE';
	DECLARE @LEGEND nvarchar(max);
	DECLARE @PRIMARY_KEY nvarchar(100);
	DECLARE @DBNAME nvarchar(100);
	DECLARE @GEOGRAPHY_COLUMN nvarchar(100);
	DECLARE @GEOGRAPHY_ORDERBY nvarchar(MAX) = '';

	-- Get DBNAME
	SELECT @DBNAME = DB_NAME();

	--Set up Legend     
	SET @LEGEND = N'-- ===================================================================' + CHAR(13) + CHAR(10)         

	--Get Primary Key Field
	SELECT TOP 1 @PRIMARY_KEY = COLUMN_NAME 
	FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
	WHERE OBJECTPROPERTY(OBJECT_ID(@Schema + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') = 1 AND TABLE_NAME = @TableName AND TABLE_CATALOG = @DBName AND TABLE_SCHEMA = @Schema;

	SELECT @ISACTIVE_SEL = count(*)
	FROM INFORMATION_SCHEMA.COLUMNS
	WHERE TABLE_NAME = @TableName AND TABLE_CATALOG = @DBName AND COLUMN_NAME = 'IsActive' AND TABLE_SCHEMA = @Schema;

	--Check for geography data type
	SELECT top 1 @GEOGRAPHY_COLUMN = COLUMN_NAME
	FROM INFORMATION_SCHEMA.COLUMNS c INNER JOIN
	INFORMATION_SCHEMA.TABLES t ON c.TABLE_NAME = t.TABLE_NAME AND c.TABLE_SCHEMA = t.TABLE_SCHEMA
	WHERE t.TABLE_NAME = @TableName and DATA_TYPE = 'geography' and t.TABLE_CATALOG = @DBNAME

	DECLARE TableCol Cursor FOR
	SELECT c.TABLE_SCHEMA, c.TABLE_NAME, c.COLUMN_NAME, c.DATA_TYPE, c.CHARACTER_MAXIMUM_LENGTH
		, IIF(c.COLUMN_NAME='RowVersion',@NBLE,IIF(c.COLUMN_NAME=@PRIMARY_KEY,@NBLE,
		IIF(c.IS_NULLABLE = 'NO' AND c.COLUMN_DEFAULT IS NULL,@NNND,IIF(c.IS_NULLABLE = 'NO' AND 
		c.COLUMN_DEFAULT IS NOT NULL,@NNWD,@NBLE)))) AS [NULLABLE_TYPE], fk.ColumnName as [FK_COLUMN_NAME]
	FROM INFORMATION_SCHEMA.COLUMNS c INNER JOIN
		 INFORMATION_SCHEMA.TABLES t ON c.TABLE_NAME = t.TABLE_NAME AND c.TABLE_SCHEMA = t.TABLE_SCHEMA INNER JOIN 
		 (
			SELECT t.name as TableName, col.name as ColumnName
			FROM sys.tables t
			inner join sys.columns col on col.object_id = t.object_id
			INNER JOIN sys.schemas sc on t.schema_id = sc.schema_id
			left outer join sys.foreign_key_columns fk_cols on fk_cols.parent_object_id = t.object_id and fk_cols.parent_column_id = col.column_id
			left outer join sys.foreign_keys fk on fk.object_id = fk_cols.constraint_object_id
			left outer join sys.tables pk_tab on pk_tab.object_id = fk_cols.referenced_object_id
			left outer join sys.columns pk_col on pk_col.column_id = fk_cols.referenced_column_id and pk_col.object_id = fk_cols.referenced_object_id
			WHERE t.name = @TableName 
			AND sc.[name] = @Schema
			and pk_col.name is not null
		 ) fk ON (fk.TableName = c.TABLE_NAME AND fk.ColumnName = c.COLUMN_NAME)
	WHERE t.TABLE_CATALOG = @DBName
		AND t.TABLE_TYPE = 'BASE TABLE'
		AND t.TABLE_NAME = @TableName
		AND t.TABLE_SCHEMA = @Schema
	ORDER BY [NULLABLE_TYPE], c.ORDINAL_POSITION;

	DECLARE @TableSchema varchar(100), @cTableName varchar(100), @ColumnName varchar(100);
	DECLARE @DataType varchar(30), @CharLength int, @NullableType varchar(30);
	DECLARE @FK_COLUMN_NAME varchar(100)
	DECLARE @PrimaryKeyDataType varchar(30);
	DECLARE @GetTransactionScope nvarchar(MAX)
	DECLARE @PARAMETERS nvarchar(max);
	DECLARE @INSERT_FIELDS nvarchar(max),@INSERT_VALUES nvarchar(max);
	DECLARE @UPDATE_VALUES nvarchar(max);
	DECLARE @WHERE nvarchar(max);
	DECLARE @OPTION_RECOMPILE nvarchar(max);
	DECLARE @CONSTRAINT_EXISTS int;

	SET @PARAMETERS ='';
	SET @INSERT_FIELDS ='';
	SET @INSERT_VALUES ='';
	SET @UPDATE_VALUES ='';
	SET @WHERE = 'WHERE';
	SET @OPTION_RECOMPILE = 'OPTION (RECOMPILE);'
	SET @CONSTRAINT_EXISTS = 0;

	SET @GetTransactionScope = N'  SET NOCOUNT ON;' + CHAR(13) + CHAR(10)
	SET @GetTransactionScope = @GetTransactionScope + N'  SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10)

	-- open the cursor
	OPEN TableCol

	-- get the first row of cursor into variables
	FETCH NEXT FROM TableCol INTO @TableSchema, @cTableName, @ColumnName, @DataType, @CharLength, @NullableType, @FK_COLUMN_NAME

	WHILE @@FETCH_STATUS = 0
		BEGIN
			IF @ColumnName NOT IN('Created','CreatedBy','Modified','ModifiedBy')
			BEGIN			
				SET @PARAMETERS=@PARAMETERS + '@' + IIF(@ColumnName=@PRIMARY_KEY,@ColumnName,@ColumnName) + ' ' + iif(@CharLength IS NULL,@DataType,@DataType + '(' + 
					CAST(@CharLength AS nvarchar(10)) + ')') + '=NULL,';
				IF @ColumnName <> @PRIMARY_KEY
					BEGIN
						SET @INSERT_FIELDS=@INSERT_FIELDS + '[' + @ColumnName + '],';
						SET @INSERT_VALUES=@INSERT_VALUES + '@' + IIF(@ColumnName=@PRIMARY_KEY,@ColumnName,@ColumnName) + ',';
						SET @UPDATE_VALUES=@UPDATE_VALUES + '[' + @ColumnName + ']=@' + IIF(@ColumnName=@PRIMARY_KEY,@ColumnName,@ColumnName) + ',';
						SET @WHERE=@WHERE + IIF(@WHERE='WHERE','',' AND') + ' (@' + @ColumnName + ' IS NULL OR [' + @ColumnName + '] = @' + @ColumnName + ')'
					END
				ELSE
					BEGIN
						SET @PrimaryKeyDataType = @DataType;
					END	
			END

			SET @CONSTRAINT_EXISTS = 1;

			FETCH NEXT FROM TableCol INTO @TableSchema, @cTableName, @ColumnName, @DataType, @CharLength, @NullableType, @FK_COLUMN_NAME
		END;    

		IF @CONSTRAINT_EXISTS = 1
		BEGIN
		  SET @PARAMETERS=LEFT(@PARAMETERS,LEN(@PARAMETERS)-1)
		END

	-- ----------------
	-- clean up cursor
	-- ----------------
	CLOSE TableCol;
	DEALLOCATE TableCol;

	  IF @GEOGRAPHY_COLUMN IS NOT NULL
	  BEGIN
		SET @PARAMETERS = '@' + @GEOGRAPHY_COLUMN + ' geography=NULL,' + @PARAMETERS
		SET @GEOGRAPHY_ORDERBY = 'CASE WHEN @' + @GEOGRAPHY_COLUMN + ' IS NOT NULL THEN ' + @GEOGRAPHY_COLUMN + '.STDistance(@' + @GEOGRAPHY_COLUMN + ') END ASC,'
	  END

	  IF @CONSTRAINT_EXISTS = 1
	  BEGIN
		--Gen Select Statement
		SET @QueryToExec = N'/****** Object:  StoredProcedure [' + @Schema + '].[zgen_' + @TableName + '_GetByIdsPaging] ******/' + CHAR(13) + CHAR(10) 
		SET @QueryToExec = @QueryToExec +  @LEGEND;
		SET @QueryToExec = @QueryToExec +  N'-- Description    : Select By Ids Paging ' + @TableName + CHAR(13) + CHAR(10) 
		SET @QueryToExec = @QueryToExec +  N'-- ===================================================================' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10)     
		SET @QueryToExec = @QueryToExec +  N'CREATE OR ALTER PROCEDURE [' + @Schema + '].[zgen_' + @TableName  + '_GetByIdsPaging]' + CHAR(13) + CHAR(10);
		SET @QueryToExec = @QueryToExec +  N'  (' + @PARAMETERS + IIF(@ISACTIVE_SEL = 1,',@IsActive bit=NULL','') + ',@PageNumber int=1,@PageSize int=50,@SortDirection varchar(5)=''ASC'')' + CHAR(13) + CHAR(10);    
		SET @QueryToExec = @QueryToExec +  N'AS' + CHAR(13) + CHAR(10) 
		SET @QueryToExec = @QueryToExec +  N'BEGIN' + CHAR(13) + CHAR(10)
		SET @QueryToExec = @QueryToExec +  @GetTransactionScope;

		IF @ISACTIVE_SEL = 1
		BEGIN
		   SET @QueryToExec = @QueryToExec +  N'    BEGIN' + CHAR(13) + CHAR(10) 
		   SET @QueryToExec = @QueryToExec +  N'      IF @IsActive IS NULL' + CHAR(13) + CHAR(10) 
		   SET @QueryToExec = @QueryToExec +  N'        WITH CTE_' + @TableName + ' AS (' + CHAR(13) + CHAR(10) 
		   SET @QueryToExec = @QueryToExec +  N'        SELECT * FROM [' + @Schema + '].[' + @TableName + '] ' + @WHERE + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        ORDER BY '
		   SET @QueryToExec = @QueryToExec +  N'' + @GEOGRAPHY_ORDERBY + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        CASE WHEN @SortDirection = ''ASC'' THEN ' + 'CreateDT' + ' END ASC, CASE WHEN @SortDirection = ''DESC'' THEN ' + 'CreateDT' + ' END DESC' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        OFFSET @PageSize * (@PageNumber - 1) ROWS FETCH NEXT @PageSize ROWS ONLY),' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        CTE_TotalRows AS' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        (' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'          SELECT COUNT(' + @PRIMARY_KEY + ') AS TotalRows FROM [' + @Schema + '].[' + @TableName + ']' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'          ' + @WHERE + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        )' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        SELECT TotalRows, [' + @Schema + '].[' + @TableName + '].* FROM [' + @Schema + '].[' + @TableName + '], CTE_TotalRows ' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        WHERE EXISTS (SELECT 1 FROM CTE_' + @TableName + ' WHERE CTE_' + @TableName + '.' + @PRIMARY_KEY + ' = [' + @Schema + '].[' + @TableName + '].' + @PRIMARY_KEY + ')' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        ORDER BY '
		   SET @QueryToExec = @QueryToExec +  N'' + @GEOGRAPHY_ORDERBY + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        CASE WHEN @SortDirection = ''ASC'' THEN ' + 'CreateDT' + ' END ASC, CASE WHEN @SortDirection = ''DESC'' THEN ' + 'CreateDT' + ' END DESC' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        ' + @OPTION_RECOMPILE + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'      ELSE' + CHAR(13) + CHAR(10) 
		   SET @QueryToExec = @QueryToExec +  N'        WITH CTE_' + @TableName + ' AS (' + CHAR(13) + CHAR(10) 
		   SET @QueryToExec = @QueryToExec +  N'        SELECT * FROM [' + @Schema + '].[' + @TableName + '] ' + @WHERE + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        AND IsActive = @IsActive' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        ORDER BY '
		   SET @QueryToExec = @QueryToExec +  N'' + @GEOGRAPHY_ORDERBY + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        CASE WHEN @SortDirection = ''ASC'' THEN ' + 'CreateDT' + ' END ASC, CASE WHEN @SortDirection = ''DESC'' THEN ' + 'CreateDT' + ' END DESC' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        OFFSET @PageSize * (@PageNumber - 1) ROWS FETCH NEXT @PageSize ROWS ONLY),' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        CTE_TotalRows AS' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        (' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'          SELECT COUNT(' + @PRIMARY_KEY + ') AS TotalRows FROM [' + @Schema + '].[' + @TableName + ']' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'          ' + @WHERE + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'          AND IsActive = @IsActive' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        )' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        SELECT TotalRows, [' + @Schema + '].[' + @TableName + '].* FROM [' + @Schema + '].[' + @TableName + '], CTE_TotalRows ' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        WHERE EXISTS (SELECT 1 FROM CTE_' + @TableName + ' WHERE CTE_' + @TableName + '.' + @PRIMARY_KEY + ' = [' + @Schema + '].[' + @TableName + '].' + @PRIMARY_KEY + ')' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        ORDER BY '
		   SET @QueryToExec = @QueryToExec +  N'' + @GEOGRAPHY_ORDERBY + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        CASE WHEN @SortDirection = ''ASC'' THEN ' + 'CreateDT' + ' END ASC, CASE WHEN @SortDirection = ''DESC'' THEN ' + 'CreateDT' + ' END DESC' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        ' + @OPTION_RECOMPILE + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'    END' + CHAR(13) + CHAR(10) 
		END
		ELSE
		BEGIN
		   SET @QueryToExec = @QueryToExec +  N'    BEGIN' + CHAR(13) + CHAR(10) 
		   SET @QueryToExec = @QueryToExec +  N'        WITH CTE_' + @TableName + ' AS (' + CHAR(13) + CHAR(10) 
		   SET @QueryToExec = @QueryToExec +  N'        SELECT * FROM [' + @Schema + '].[' + @TableName + '] ' + @WHERE + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        ORDER BY '
		   SET @QueryToExec = @QueryToExec +  N'' + @GEOGRAPHY_ORDERBY + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        CASE WHEN @SortDirection = ''ASC'' THEN ' + 'CreateDT' + ' END ASC, CASE WHEN @SortDirection = ''DESC'' THEN ' + 'CreateDT' + ' END DESC' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        OFFSET @PageSize * (@PageNumber - 1) ROWS FETCH NEXT @PageSize ROWS ONLY),' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        CTE_TotalRows AS' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        (' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'          SELECT COUNT(' + @PRIMARY_KEY + ') AS TotalRows FROM [' + @Schema + '].[' + @TableName + ']' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'          ' + @WHERE + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        )' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        SELECT TotalRows, [' + @Schema + '].[' + @TableName + '].* FROM [' + @Schema + '].[' + @TableName + '], CTE_TotalRows ' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        WHERE EXISTS (SELECT 1 FROM CTE_' + @TableName + ' WHERE CTE_' + @TableName + '.' + @PRIMARY_KEY + ' = [' + @Schema + '].[' + @TableName + '].' + @PRIMARY_KEY + ')' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        ORDER BY '
		   SET @QueryToExec = @QueryToExec +  N'' + @GEOGRAPHY_ORDERBY + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        CASE WHEN @SortDirection = ''ASC'' THEN ' + 'CreateDT' + ' END ASC, CASE WHEN @SortDirection = ''DESC'' THEN ' + 'CreateDT' + ' END DESC' + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'        ' + @OPTION_RECOMPILE + CHAR(13) + CHAR(10)
		   SET @QueryToExec = @QueryToExec +  N'    END' + CHAR(13) + CHAR(10) 
		END
		SET @QueryToExec = @QueryToExec +  N'END' + CHAR(13) + CHAR(10)
	END

END

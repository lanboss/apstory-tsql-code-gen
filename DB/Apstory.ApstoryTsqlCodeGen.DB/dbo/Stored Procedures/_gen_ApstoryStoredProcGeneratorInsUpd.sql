
/* ====================Testing==================

declare @QueryToExec nvarchar(MAX)
exec [*gen_ApstoryStoredProcGeneratorInsUpd] 'Account', @QueryToExec output
select @QueryToExec
*/

CREATE   PROCEDURE [dbo].[*gen_ApstoryStoredProcGeneratorInsUpd]
	-- Add the parameters for the stored procedure here
	@TableName nvarchar(255),
	@Schema nvarchar(20) = 'dbo',
	@QueryToExec nvarchar(MAX) output
AS
BEGIN

DECLARE @OrderByDir nvarchar(4)=N'ASC';
DECLARE @ISACTIVE_SEL bit =1; --Set to 1 if your table has a Bit field named IsActive
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

DECLARE TableCol Cursor FOR
SELECT c.TABLE_SCHEMA, c.TABLE_NAME, c.COLUMN_NAME, c.DATA_TYPE, c.CHARACTER_MAXIMUM_LENGTH, c.NUMERIC_PRECISION, c.NUMERIC_SCALE
    , IIF(c.COLUMN_NAME='RowVersion',@NBLE,IIF((c.COLUMN_NAME=@PRIMARY_KEY OR c.COLUMN_NAME IN ('CreateDT','UpdateDT','IsActive')),@NBLE,IIF(c.IS_NULLABLE = 'NO' AND c.COLUMN_DEFAULT IS NULL,@NNND,IIF(c.IS_NULLABLE = 'NO' AND c.COLUMN_DEFAULT IS NOT NULL,@NNWD,@NBLE)))) AS [NULLABLE_TYPE]
FROM INFORMATION_SCHEMA.COLUMNS c INNER JOIN
    INFORMATION_SCHEMA.TABLES t ON c.TABLE_NAME = t.TABLE_NAME AND t.TABLE_SCHEMA = C.TABLE_SCHEMA
WHERE t.TABLE_CATALOG = @DBName
    AND t.TABLE_TYPE = 'BASE TABLE'
    AND t.TABLE_NAME = @TableName
	AND t.TABLE_SCHEMA = @Schema
	AND c.COLUMN_NAME not in ('CreateDT')
ORDER BY [NULLABLE_TYPE], c.ORDINAL_POSITION;

DECLARE @TableSchema varchar(100), @cTableName varchar(100), @ColumnName varchar(100);
DECLARE @DataType varchar(30), @CharLength varchar(30), @NullableType varchar(30);
DECLARE @PrimaryKeyDataType varchar(30);
DECLARE @InsUpdDelTransTryCatchTop nvarchar(MAX)
DECLARE @InsUpdDelTransTryCatchBottom nvarchar(MAX)
DECLARE @GetTransactionScope nvarchar(MAX)
DECLARE @PARAMETERS nvarchar(max);
DECLARE @INSERT_FIELDS nvarchar(max),@INSERT_VALUES nvarchar(max);
DECLARE @UPDATE_VALUES nvarchar(max);
DECLARE @IsIdentity bit = 0;
DECLARE @NumericPrecision INT
DECLARE @NumericScale INT

SET @PARAMETERS ='';
SET @INSERT_FIELDS ='';
SET @INSERT_VALUES ='';
SET @UPDATE_VALUES ='';

SET @InsUpdDelTransTryCatchTop = N'  DECLARE @InitialTransCount INT = @@TRANCOUNT;' + CHAR(13) + CHAR(10)
SET @InsUpdDelTransTryCatchTop = @InsUpdDelTransTryCatchTop + N'  DECLARE @TranName varchar(32) = OBJECT_NAME(@@PROCID);' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10)
SET @InsUpdDelTransTryCatchTop = @InsUpdDelTransTryCatchTop + N'  BEGIN TRY' + CHAR(13) + CHAR(10)
SET @InsUpdDelTransTryCatchTop = @InsUpdDelTransTryCatchTop + N'  IF @InitialTransCount = 0 BEGIN TRANSACTION @TranName' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10)
SET @InsUpdDelTransTryCatchBottom = N'  IF @@ERROR <> 0 BEGIN GOTO errorMsg_section END' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10)
SET @InsUpdDelTransTryCatchBottom = @InsUpdDelTransTryCatchBottom + N'  IF @InitialTransCount = 0 COMMIT TRANSACTION @TranName' + CHAR(13) + CHAR(10)
SET @InsUpdDelTransTryCatchBottom = @InsUpdDelTransTryCatchBottom + N'  SET @RetMsg = LTRIM(ISNULL(@RetMsg, '''') + ''Successful'')' + CHAR(13) + CHAR(10)
SET @InsUpdDelTransTryCatchBottom = @InsUpdDelTransTryCatchBottom + N'  RETURN 0' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10)      
SET @InsUpdDelTransTryCatchBottom = @InsUpdDelTransTryCatchBottom + N'  errorMsg_section:' + CHAR(13) + CHAR(10)
SET @InsUpdDelTransTryCatchBottom = @InsUpdDelTransTryCatchBottom + N'    SET @RetMsg = LTRIM(ISNULL(@RetMsg, '''') + '' SQLErrMSG: '' + ISNULL(ERROR_MESSAGE(), ''''))' + CHAR(13) + CHAR(10)  
SET @InsUpdDelTransTryCatchBottom = @InsUpdDelTransTryCatchBottom + N'    GOTO error_section' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10)
SET @InsUpdDelTransTryCatchBottom = @InsUpdDelTransTryCatchBottom + N'  error_section:' + CHAR(13) + CHAR(10)
SET @InsUpdDelTransTryCatchBottom = @InsUpdDelTransTryCatchBottom + N'    SET @RetMsg = ISNULL(@RetMsg, '''')' + CHAR(13) + CHAR(10) 
SET @InsUpdDelTransTryCatchBottom = @InsUpdDelTransTryCatchBottom + N'    IF @InitialTransCount = 0 ROLLBACK TRANSACTION @TranName' + CHAR(13) + CHAR(10) 
SET @InsUpdDelTransTryCatchBottom = @InsUpdDelTransTryCatchBottom + N'    RETURN 1' + CHAR(13) + CHAR(10)
SET @InsUpdDelTransTryCatchBottom = @InsUpdDelTransTryCatchBottom + N'  END TRY' + CHAR(13) + CHAR(10) 
SET @InsUpdDelTransTryCatchBottom = @InsUpdDelTransTryCatchBottom + N'  BEGIN CATCH' + CHAR(13) + CHAR(10) 
SET @InsUpdDelTransTryCatchBottom = @InsUpdDelTransTryCatchBottom + N'    IF @InitialTransCount = 0 ROLLBACK TRANSACTION @TranName' + CHAR(13) + CHAR(10) 
SET @InsUpdDelTransTryCatchBottom = @InsUpdDelTransTryCatchBottom + N'    SET @RetMsg = LTRIM(ISNULL(@RetMsg, '''') + '' SQLErrMSG: '' + ISNULL(ERROR_MESSAGE(), ''''))' + CHAR(13) + CHAR(10) 
SET @InsUpdDelTransTryCatchBottom = @InsUpdDelTransTryCatchBottom + N'    RETURN 1' + CHAR(13) + CHAR(10)
SET @InsUpdDelTransTryCatchBottom = @InsUpdDelTransTryCatchBottom + N'  END CATCH' + CHAR(13) + CHAR(10)                

SET @GetTransactionScope = N'  SET NOCOUNT ON;' + CHAR(13) + CHAR(10)
SET @GetTransactionScope = @GetTransactionScope + N'  SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10)

-- open the cursor
OPEN TableCol

-- get the first row of cursor into variables
FETCH NEXT FROM TableCol INTO @TableSchema, @cTableName, @ColumnName, @DataType, @CharLength, @NumericPrecision, @NumericScale, @NullableType

WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @ColumnName NOT IN('Created','CreatedBy','Modified','ModifiedBy')
        BEGIN
			select @IsIdentity =columnproperty(object_id(@Schema + '.' + @cTableName),@ColumnName,'IsIdentity');

			IF (@IsIdentity = 0 OR @ColumnName = @PRIMARY_KEY)			
						SET @PARAMETERS=@PARAMETERS + '@' + IIF(@ColumnName=@PRIMARY_KEY,@ColumnName,@ColumnName) + ' ' + 
                                                iif(@CharLength IS NULL,IIF(@DataType = 'decimal',@DataType+ '('+CAST(@NumericPrecision as VARCHAR)+','+CAST(@NumericScale AS VARCHAR)+')', @DataType)
												,IIF(@DataType = 'geography',@DataType,@DataType + '(' + 
			IIF(@CharLength = '-1','MAX',@CharLength) + ')')) +  IIF(@NullableType=@NNND OR @NullableType=@NNWD,',','=NULL,');

			IF @ColumnName <> @PRIMARY_KEY AND @IsIdentity = 0
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

        FETCH NEXT FROM TableCol INTO @TableSchema, @cTableName, @ColumnName, @DataType, @CharLength, @NumericPrecision, @NumericScale, @NullableType
    END;

    SET @PARAMETERS=LEFT(@PARAMETERS,LEN(@PARAMETERS)-1)
    SET @INSERT_FIELDS=LEFT(@INSERT_FIELDS,LEN(@INSERT_FIELDS)-1)
    SET @INSERT_VALUES=LEFT(@INSERT_VALUES,LEN(@INSERT_VALUES)-1)
    SET @UPDATE_VALUES=LEFT(@UPDATE_VALUES,LEN(@UPDATE_VALUES)-1)

-- ----------------
-- clean up cursor
-- ----------------
CLOSE TableCol;
DEALLOCATE TableCol;

--Print Upsert Statement
    SET @QueryToExec = N'/****** Object:  StoredProcedure [' + @Schema + '].[zgen_' + @TableName + '_InsUpd] ******/' + CHAR(13) + CHAR(10) 
    SET @QueryToExec = @QueryToExec +  @LEGEND;
    SET @QueryToExec = @QueryToExec +  N'-- Description    : Insert Update ' + @TableName + CHAR(13) + CHAR(10) 
    SET @QueryToExec = @QueryToExec +  N'-- ===================================================================' + CHAR(13) + CHAR(10) 
    SET @QueryToExec = @QueryToExec +  CHAR(13) + CHAR(10) 
    SET @QueryToExec = @QueryToExec +  N'CREATE OR ALTER PROCEDURE [' + @Schema + '].[zgen_' + @TableName  + '_InsUpd]' + CHAR(13) + CHAR(10);
    SET @QueryToExec = @QueryToExec +  N'  (' + @PARAMETERS + N', @RetMsg NVARCHAR(MAX) OUTPUT)' + CHAR(13) + CHAR(10);
    SET @QueryToExec = @QueryToExec +  N'AS' + CHAR(13) + CHAR(10) 
    SET @QueryToExec = @QueryToExec +  N'BEGIN' + CHAR(13) + CHAR(10)
	SET @QueryToExec = @QueryToExec +  @InsUpdDelTransTryCatchTop
	IF(@PrimaryKeyDataType = 'uniqueidentifier')
	BEGIN
		SET @QueryToExec = @QueryToExec +  N'  DECLARE @tempUID TABLE (PrimaryGuid uniqueidentifier)' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10)
	END
	SET @QueryToExec = @QueryToExec +  N'  SET @UpdateDT = ISNULL(@UpdateDT,GETDATE())' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10)	
    SET @QueryToExec = @QueryToExec +  N'  IF @' + @PRIMARY_KEY + ' IS NULL' + CHAR(13) + CHAR(10)
    SET @QueryToExec = @QueryToExec +  N'    BEGIN' + CHAR(13) + CHAR(10) 
    SET @QueryToExec = @QueryToExec +  N'      INSERT INTO [' + @Schema + '].[' + @TableName + ']' + CHAR(13) + CHAR(10) 
    SET @QueryToExec = @QueryToExec +  N'        (' + @INSERT_FIELDS + N')' + CHAR(13) + CHAR(10)
	IF(@PrimaryKeyDataType = 'uniqueidentifier')
	BEGIN
		SET @QueryToExec = @QueryToExec +  N'        OUTPUT inserted.' + @PRIMARY_KEY + ' INTO @tempUID' + CHAR(13) + CHAR(10)
	END
    SET @QueryToExec = @QueryToExec +  N'      VALUES' + CHAR(13) + CHAR(10) 
    SET @QueryToExec = @QueryToExec +  N'        (' + @INSERT_VALUES + N');' + CHAR(13) + CHAR(10)
	IF(@PrimaryKeyDataType = 'uniqueidentifier')
	BEGIN
		SET @QueryToExec = @QueryToExec +  N'      SELECT * FROM [' + @Schema + '].[' + @TableName + '] WHERE [' + @PRIMARY_KEY + '] IN (SELECT PrimaryGuid FROM @tempUID);' + CHAR(13) + CHAR(10) 	
	END
	ELSE
		SET @QueryToExec = @QueryToExec +  N'      SELECT * FROM [' + @Schema + '].[' + @TableName + '] WHERE [' + @PRIMARY_KEY + '] = SCOPE_IDENTITY();' + CHAR(13) + CHAR(10)	
    SET @QueryToExec = @QueryToExec +  N'    END' + CHAR(13) + CHAR(10) 
    SET @QueryToExec = @QueryToExec +  N'  ELSE' + CHAR(13) + CHAR(10) 
    SET @QueryToExec = @QueryToExec +  N'    BEGIN' + CHAR(13) + CHAR(10) 
    SET @QueryToExec = @QueryToExec +  N'      UPDATE [' + @Schema + '].[' + @TableName + ']' + CHAR(13) + CHAR(10) 
    SET @QueryToExec = @QueryToExec +  N'        SET ' + @UPDATE_VALUES + CHAR(13) + CHAR(10) 
    SET @QueryToExec = @QueryToExec +  N'        WHERE ([' + @PRIMARY_KEY + '] = @' + @PRIMARY_KEY + ');' + CHAR(13) + CHAR(10) 
    SET @QueryToExec = @QueryToExec +  N'      SELECT * FROM [' + @Schema + '].[' + @TableName + '] WHERE [' + @PRIMARY_KEY + '] = @' + @PRIMARY_KEY + ';' + CHAR(13) + CHAR(10) 
    SET @QueryToExec = @QueryToExec +  N'    END' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) 
	SET @QueryToExec = @QueryToExec +  @InsUpdDelTransTryCatchBottom
    SET @QueryToExec = @QueryToExec +  N'END' + CHAR(13) + CHAR(10)    
    SET @QueryToExec = @QueryToExec +  CHAR(13) + CHAR(10)           
	
END

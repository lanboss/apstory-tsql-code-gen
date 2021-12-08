/* ====================Testing==================

declare @Model nvarchar(MAX)
exec [*gen_ApstoryModelGeneratorByTableName] 'Club','dbo', 0, @Model output

*/

CREATE   PROCEDURE [dbo].[*gen_ApstoryModelGeneratorByTableName]
	-- Add the parameters for the stored procedure here
	@TableName nvarchar(255),
	@Schema nvarchar(20) = 'dbo',
	@IncludeJsonConvert BIT = 0,
	@IncludeForeignKeys BIT = 0,
	@Model nvarchar(MAX) output
AS
BEGIN

declare @Result varchar(max) = '    public partial class ' + @TableName + '
    {
		public int? TotalRows { get; set; }
		'

DECLARE @Inject NVARCHAR(256) = ''
IF (@IncludeJsonConvert = 1)
BEGIN
	SET @Inject = '        [JsonConverter(typeof(LongToStringConverter))]'
END

IF (@IncludeForeignKeys = 0)
BEGIN
	select @Result = @Result 
		+ CASE ColumnType WHEN 'long' THEN @Inject WHEN 'long?' THEN @Inject ELSE '' END
		+ CASE ColumnType WHEN 'SqlGeography' THEN '        
		[Newtonsoft.Json.JsonIgnore]' ELSE '' END + '
		public ' + ColumnType + NullableSign + ' ' + ColumnName + ' { get; set; }
	'
	from
	(
		select 
			replace(col.name, ' ', '_') AS ColumnName,
			column_id AS ColumnId,
			case typ.name 
				when 'bigint' then 'long'
				when 'binary' then 'byte[]'
				when 'bit' then 'bool'
				when 'char' then 'string'
				when 'date' then 'DateTime'
				when 'datetime' then 'DateTime'
				when 'datetime2' then 'DateTime'
				when 'datetimeoffset' then 'DateTimeOffset'
				when 'decimal' then 'decimal'
				when 'float' then 'double'
				when 'image' then 'byte[]'
				when 'int' then 'int'
				when 'money' then 'decimal'
				when 'nchar' then 'string'
				when 'ntext' then 'string'
				when 'numeric' then 'decimal'
				when 'nvarchar' then 'string'
				when 'real' then 'float'
				when 'smalldatetime' then 'DateTime'
				when 'smallint' then 'short'
				when 'smallmoney' then 'decimal'
				when 'text' then 'string'
				when 'time' then 'TimeSpan'
				when 'timestamp' then 'long'
				when 'tinyint' then 'byte'
				when 'uniqueidentifier' then 'Guid'
				when 'varbinary' then 'byte[]'
				when 'varchar' then 'string'
				when 'geography' then 'SqlGeography'
				else 'UNKNOWN_' + typ.name
			end ColumnType,
			case 
				when (
				col.is_nullable = 1 or
				col.name in (SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc INNER JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE ccu ON tc.CONSTRAINT_NAME = ccu.CONSTRAINT_NAME WHERE tc.TABLE_NAME = @TableName and tc.CONSTRAINT_TYPE = 'Primary Key') or
				col.name in ('CreateDT', 'UpdateDT')
				) 
				and typ.name in ('bigint', 'bit', 'date', 'datetime', 'datetime2', 'datetimeoffset', 'decimal', 'float', 'int', 'money', 'numeric', 'real', 'smalldatetime', 'smallint', 'smallmoney', 'time', 'tinyint', 'uniqueidentifier') 
				then '?' 
				else '' 
			end NullableSign
		from sys.columns col
		inner join sys.types typ on
		col.system_type_id = typ.system_type_id AND col.user_type_id = typ.user_type_id
		where object_id = object_id(@Schema + '.' + @TableName)
	) t
	order by ColumnId
END
ELSE
BEGIN
	select @Result = @Result 
			+ CASE ColumnType WHEN 'long' THEN @Inject WHEN 'long?' THEN @Inject ELSE '' END
			+ CASE ColumnType WHEN 'SqlGeography' THEN '        
		[Newtonsoft.Json.JsonIgnore]' ELSE '' END + '
		public ' + ColumnType + NullableSign + ' ' + ColumnName + ' { get; set; }
	'
	from
	(
		select 
			replace(col.name, ' ', '_') AS ColumnName,
			column_id AS ColumnId,
			case typ.name 
				when 'bigint' then 'long'
				when 'binary' then 'byte[]'
				when 'bit' then 'bool'
				when 'char' then 'string'
				when 'date' then 'DateTime'
				when 'datetime' then 'DateTime'
				when 'datetime2' then 'DateTime'
				when 'datetimeoffset' then 'DateTimeOffset'
				when 'decimal' then 'decimal'
				when 'float' then 'double'
				when 'image' then 'byte[]'
				when 'int' then 'int'
				when 'money' then 'decimal'
				when 'nchar' then 'string'
				when 'ntext' then 'string'
				when 'numeric' then 'decimal'
				when 'nvarchar' then 'string'
				when 'real' then 'float'
				when 'smalldatetime' then 'DateTime'
				when 'smallint' then 'short'
				when 'smallmoney' then 'decimal'
				when 'text' then 'string'
				when 'time' then 'TimeSpan'
				when 'timestamp' then 'long'
				when 'tinyint' then 'byte'
				when 'uniqueidentifier' then 'Guid'
				when 'varbinary' then 'byte[]'
				when 'varchar' then 'string'
				when 'geography' then 'SqlGeography'
				else 'UNKNOWN_' + typ.name
			end ColumnType,
			case 
				when (
				col.is_nullable = 1 or
				col.name in (SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc INNER JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE ccu ON tc.CONSTRAINT_NAME = ccu.CONSTRAINT_NAME WHERE tc.TABLE_NAME = @TableName and tc.CONSTRAINT_TYPE = 'Primary Key') or
				col.name in ('CreateDT', 'UpdateDT')
				) 
				and typ.name in ('bigint', 'bit', 'date', 'datetime', 'datetime2', 'datetimeoffset', 'decimal', 'float', 'int', 'money', 'numeric', 'real', 'smalldatetime', 'smallint', 'smallmoney', 'time', 'tinyint', 'uniqueidentifier') 
				then '?' 
				else '' 
			end NullableSign
		from sys.columns col
		inner join sys.types typ on
		col.system_type_id = typ.system_type_id AND col.user_type_id = typ.user_type_id
		where object_id = object_id(@Schema + '.' + @TableName)
		UNION -- ENUMS
			select 
			replace(replace(col.name, ' ', '_'), 'Id', '') AS ColumnName,
			100+column_id AS ColumnId,
			COALESCE(tab.[name], replace(replace(col.name, ' ', '_'), 'Id', '')) ColumnType,
			'' NullableSign
		from sys.columns col
		inner join sys.types typ on	col.system_type_id = typ.system_type_id AND col.user_type_id = typ.user_type_id
		LEFT JOIN sys.foreign_key_columns fkc ON fkc.parent_object_id = col.object_id AND fkc.parent_column_id = col.column_id
		LEFT JOIN sys.tables tab ON tab.object_id = fkc.referenced_object_id
		where col.object_id = object_id(@Schema + '.' + @TableName) 
		AND column_id <> 1 AND col.name like '%Id' AND typ.name = 'tinyint'
		UNION
			select 
			replace(replace(col.name, ' ', '_'), 'Id', '') AS ColumnName,
			200+column_id AS ColumnId,
			COALESCE(tab.[name], replace(replace(col.name, ' ', '_'), 'Id', '')) ColumnType,
			'' NullableSign
		from sys.columns col
		inner join sys.types typ on 		col.system_type_id = typ.system_type_id AND col.user_type_id = typ.user_type_id
		LEFT JOIN sys.foreign_key_columns fkc ON fkc.parent_object_id = col.object_id AND fkc.parent_column_id = col.column_id
		LEFT JOIN sys.tables tab ON tab.object_id = fkc.referenced_object_id
		where col.object_id = object_id(@Schema + '.' + @TableName) 
		AND column_id <> 1 AND col.name like '%Id' AND typ.name <> 'tinyint' AND typ.name NOT IN ('text', 'varchar', 'nvarchar', 'char', 'nchar', 'ntext')
	) t
	order by ColumnId
END





set @Result = @Result  + '}'
set @Model = @Result;
select @Model
	
END

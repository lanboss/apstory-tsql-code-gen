
/* ====================Testing==================

exec [*gen_ApstoryStoredProcGeneratorGetParamsByTable] 'zgen','Company','GetBySearch'

*/

CREATE   PROCEDURE [dbo].[*gen_ApstoryStoredProcGeneratorGetParamsByTable]
@Prefix nvarchar(4),
@TableName nvarchar(255),
@Suffix nvarchar(50),
@Schema nvarchar(20) = 'dbo'
AS
BEGIN

	SELECT sp.ROUTINE_NAME as RoutineName,[name] as ParameterName,type_name(user_type_id) as TypeName, p.is_nullable as [IsNullable],
	case type_name(user_type_id) 
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
		when 'geography' then 'Microsoft.SqlServer.Types.SqlGeography'
		when 'udtt_Uniqueidentifiers' then 'List<Guid>'
		when 'udtt_Ints' then 'List<int>'
		else 'UNKNOWN_' + type_name(user_type_id)
	end ColumnType,	
	parameter_id as ParamOrder
	FROM sys.parameters p
	INNER JOIN INFORMATION_SCHEMA.ROUTINES sp
	ON p.object_id = object_id(sp.ROUTINE_SCHEMA + '.' + sp.ROUTINE_NAME)
	WHERE sp.ROUTINE_TYPE = 'PROCEDURE'
	AND sp.ROUTINE_SCHEMA = @Schema
	AND sp.ROUTINE_NAME = @Prefix + '_' + @TableName + '_' + @Suffix
	ORDER BY sp.ROUTINE_NAME,parameter_id ASC
	
END


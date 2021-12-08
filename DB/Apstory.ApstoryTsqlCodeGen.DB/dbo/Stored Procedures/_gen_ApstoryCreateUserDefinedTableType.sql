
/* ====================Testing==================

exec [*gen_ApstoryCreateUserDefinedTableType]

*/

CREATE   PROCEDURE [dbo].[*gen_ApstoryCreateUserDefinedTableType]
@Schema nvarchar(20) = 'dbo'
AS
BEGIN
    DECLARE @QueryToExec nvarchar(max)
	IF TYPE_ID(@Schema + N'.udtt_Uniqueidentifiers') IS NULL
    BEGIN
        SET @QueryToExec = 'CREATE TYPE [' + @Schema + '].[udtt_Uniqueidentifiers] AS TABLE([Id] [uniqueidentifier] NULL)'
        EXECUTE sp_executesql @QueryToExec
    END
    IF TYPE_ID(@Schema + N'.udtt_Ints') IS NULL
    BEGIN
        SET @QueryToExec = 'CREATE TYPE [' + @Schema + '].[udtt_Ints] AS TABLE([Id] [int] NULL)'
        EXECUTE sp_executesql @QueryToExec
    END
END
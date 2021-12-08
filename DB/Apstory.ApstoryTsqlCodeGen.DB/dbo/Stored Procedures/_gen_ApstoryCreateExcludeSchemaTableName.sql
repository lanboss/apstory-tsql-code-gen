
/* ====================Testing==================

exec [*gen_ApstoryCreateExcludeSchemaTableName]

*/

CREATE   PROCEDURE [dbo].[*gen_ApstoryCreateExcludeSchemaTableName]	
AS
BEGIN
	IF OBJECT_ID(N'dbo.*gen_ExcludeSchemaTableName', N'U') IS NULL 
	BEGIN	
		CREATE TABLE [dbo].[*gen_ExcludeSchemaTableName](
		[Id] [int] IDENTITY(1,1) NOT NULL,
		[Schema] [nvarchar](50) NOT NULL,
		[TableName] [nvarchar](120) NOT NULL,
		[CreateDT] [datetime] NOT NULL,
		[UpdateDT] [datetime] NOT NULL,
		[IsActive] [bit] NOT NULL,
		CONSTRAINT [PK_*gen_ExcludeSchemaTableName] PRIMARY KEY CLUSTERED 
		(
			[Id] ASC
		)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
		) ON [PRIMARY]
		ALTER TABLE [dbo].[*gen_ExcludeSchemaTableName] ADD  CONSTRAINT [DF_*gen_ExcludeSchemaTableName_CreateDT]  DEFAULT (getdate()) FOR [CreateDT]
		ALTER TABLE [dbo].[*gen_ExcludeSchemaTableName] ADD  CONSTRAINT [DF_*gen_ExcludeSchemaTableName_UpdateDT]  DEFAULT (getdate()) FOR [UpdateDT]
		ALTER TABLE [dbo].[*gen_ExcludeSchemaTableName] ADD  CONSTRAINT [DF_*gen_ExcludeSchemaTableName_IsActive]  DEFAULT ((1)) FOR [IsActive]

		INSERT INTO [*gen_ExcludeSchemaTableName] ([Schema],TableName) VALUES ('dbo','__RefactorLog')
		INSERT INTO [*gen_ExcludeSchemaTableName] ([Schema],TableName) VALUES ('dbo','sysdiagrams')	
		INSERT INTO [*gen_ExcludeSchemaTableName] ([Schema],TableName) VALUES ('dbo','schemaversions')
		INSERT INTO [*gen_ExcludeSchemaTableName] ([Schema],TableName) VALUES ('dbo','*gen_ExcludeSchemaTableName')			

	END
END
CREATE TABLE [dbo].[*gen_ExcludeSchemaTableName] (
    [Id]        INT            IDENTITY (1, 1) NOT NULL,
    [Schema]    NVARCHAR (50)  NOT NULL,
    [TableName] NVARCHAR (120) NOT NULL,
    [CreateDT]  DATETIME       CONSTRAINT [DF_*gen_ExcludeSchemaTableName_CreateDT] DEFAULT (getdate()) NOT NULL,
    [UpdateDT]  DATETIME       CONSTRAINT [DF_*gen_ExcludeSchemaTableName_UpdateDT] DEFAULT (getdate()) NOT NULL,
    [IsActive]  BIT            CONSTRAINT [DF_*gen_ExcludeSchemaTableName_IsActive] DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_*gen_ExcludeSchemaTableName] PRIMARY KEY CLUSTERED ([Id] ASC)
);


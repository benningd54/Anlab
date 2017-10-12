﻿CREATE TABLE [dbo].[TestItems] (
    [Id]                   NVARCHAR (128) NOT NULL,
    [AdditionalInfoPrompt] NVARCHAR (MAX) NULL,
    [Analysis]             NVARCHAR (512) NOT NULL,
    [Category]             NVARCHAR (64)  NOT NULL,
    [Group]                NVARCHAR (MAX) NOT NULL,
    [LabOrder]             INT            NOT NULL,
    [Notes]                NVARCHAR (MAX) NULL,
    [Public]               BIT            NOT NULL,
    [RequestOrder]         INT            NOT NULL,
    CONSTRAINT [PK_TestItems] PRIMARY KEY CLUSTERED ([Id] ASC)
);




/*
ReThreads SQL Server installation. Run this ENTIRE file in SSMS or sqlcmd.
No SQLCMD mode or EF CLI needed. Requires SQL Server 2017+ (validated on SQL Server 2022).
Use the SAME @DatabaseName in both files. Default: ReThreadsDb.
Fresh database installation only. No DROP DATABASE, TRUNCATE or existing-data reset.
Current project migration: 20260922115216_AddMonthlyFundStatements.
STEP 1 OF 2: CREATE DATABASE AND SCHEMA ONLY.
Creates 49 application tables plus __EFMigrationsHistory, PK/FK/indexes/defaults.
All tables remain EMPTY. Migration history entries are inserted by file 02.
Next run: 02_ReThreads_Seed_Data.sql (required before starting the application).
Rerunning this file after success leaves existing schema/data unchanged.

*/
USE [master];
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @DatabaseName sysname = N'ReThreadsDb';
IF @DatabaseName IS NULL OR LEN(LTRIM(RTRIM(@DatabaseName)))=0
    THROW 51002, 'Database name is required.', 1;
IF @DatabaseName IN (N'master',N'model',N'msdb',N'tempdb')
    THROW 51003, 'Choose an application database name.', 1;
IF DB_ID(@DatabaseName) IS NULL
BEGIN
    DECLARE @CreateSql nvarchar(max)=N'CREATE DATABASE '+QUOTENAME(@DatabaseName)+N';';
    EXEC sys.sp_executesql @CreateSql;
END;

DECLARE @Sql nvarchar(max)=N'USE '+QUOTENAME(@DatabaseName)+N';'+
N'SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @LockResult int;
    EXEC @LockResult=sys.sp_getapplock @Resource=N''ReThreads.Bootstrap'',
        @LockMode=''Exclusive'',@LockOwner=''Transaction'',@LockTimeout=10000;
    IF @LockResult<0 THROW 51000, ''Another database installer is running.'', 1;
IF EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N''ReThreads.Schema.Version'' AND CONVERT(nvarchar(100),value)=N''20260924.2'')
BEGIN
    PRINT N''Schema is already installed. No changes made.'';
    COMMIT; RETURN;
END;
IF EXISTS(SELECT 1 FROM sys.tables WHERE is_ms_shipped=0)
    THROW 51001, ''Database is not empty. Select a NEW database name; existing data is never overwritten.'', 1;
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[__EFMigrationsHistory](
	[MigrationId] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ProductVersion] [nvarchar](32) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED
(
	[MigrationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[AiPromptConfigurations](
	[Id] [uniqueidentifier] NOT NULL,
	[Feature] [nvarchar](450) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Name] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[PromptText] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Enabled] [bit] NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
 CONSTRAINT [PK_AiPromptConfigurations] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[AreaGroups](
	[Id] [uniqueidentifier] NOT NULL,
	[AreaId] [uniqueidentifier] NOT NULL,
	[GroupName] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Description] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CapacityKg] [decimal](18, 2) NOT NULL,
	[CurrentKg] [decimal](18, 2) NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
 CONSTRAINT [PK_AreaGroups] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[Categories](
	[Id] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](450) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Description] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
	[Code] [nvarchar](80) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ParentId] [uniqueidentifier] NULL,
	[SortOrder] [int] NOT NULL,
	[Type] [nvarchar](40) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[MinimumMatchCount] [int] NULL,
 CONSTRAINT [PK_Categories] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[ClassificationBatchTransfers](
	[Id] [uniqueidentifier] NOT NULL,
	[IntakeBatchId] [uniqueidentifier] NOT NULL,
	[FromTeamId] [uniqueidentifier] NOT NULL,
	[ToTeamId] [uniqueidentifier] NOT NULL,
	[StaffId] [uniqueidentifier] NOT NULL,
	[TransferredAt] [datetime2](7) NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
 CONSTRAINT [PK_ClassificationBatchTransfers] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[ClassificationScoringRule](
	[Id] [int] NOT NULL,
	[GradeAMinimum] [decimal](18, 2) NOT NULL,
	[GradeBMinimum] [decimal](18, 2) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_ClassificationScoringRule] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[ClassifiedBatchDonationRequests](
	[ClassifiedBatchId] [uniqueidentifier] NOT NULL,
	[DonationRequestId] [uniqueidentifier] NOT NULL,
	[IntakeBatchId] [uniqueidentifier] NOT NULL,
	[LinkedAt] [datetime2](7) NOT NULL,
	[Id] [uniqueidentifier] NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
 CONSTRAINT [PK_ClassifiedBatchDonationRequests] PRIMARY KEY CLUSTERED
(
	[ClassifiedBatchId] ASC,
	[DonationRequestId] ASC,
	[IntakeBatchId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[ClassifiedBatches](
	[Id] [uniqueidentifier] NOT NULL,
	[WarehouseId] [uniqueidentifier] NOT NULL,
	[GroupId] [uniqueidentifier] NULL,
	[AreaId] [uniqueidentifier] NULL,
	[ConditionRating] [int] NOT NULL,
	[BatchCode] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[TotalWeight] [decimal](18, 2) NOT NULL,
	[TotalItem] [int] NOT NULL,
	[Status] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
	[ClassificationDate] [datetime2](7) NOT NULL,
	[ClothingType] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[FabricType] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[GarmentGroup] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Gender] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[GroupKey] [nvarchar](450) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ProcessingDirection] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Size] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[TargetUser] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ReceivedItemCount] [int] NULL,
	[ReceivedWeight] [decimal](18, 2) NULL,
	[SentToWarehouseAt] [datetime2](7) NULL,
	[SentToWarehouseByStaffId] [uniqueidentifier] NULL,
	[StoredAt] [datetime2](7) NULL,
	[StoredByStaffId] [uniqueidentifier] NULL,
	[WarehouseReceiptNotes] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[WarehouseReceivedAt] [datetime2](7) NULL,
	[WarehouseReceivedByStaffId] [uniqueidentifier] NULL,
	[ClothingTypeId] [uniqueidentifier] NULL,
	[ConditionGradeId] [uniqueidentifier] NULL,
	[FabricTypeId] [uniqueidentifier] NULL,
	[GarmentGroupId] [uniqueidentifier] NULL,
	[GenderId] [uniqueidentifier] NULL,
	[SizeId] [uniqueidentifier] NULL,
	[TargetUserId] [uniqueidentifier] NULL,
	[ClassificationAreaName] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PlacedInClassificationAreaAt] [datetime2](7) NULL,
	[PlacedInClassificationAreaByStaffId] [uniqueidentifier] NULL,
	[RemovedFromClassificationAreaAt] [datetime2](7) NULL,
	[RemovedFromClassificationAreaByStaffId] [uniqueidentifier] NULL,
	[StorageLocationId] [uniqueidentifier] NULL,
	[ProcessingOperationOutputId] [uniqueidentifier] NULL,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_ClassifiedBatches] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[ClassifiedItems](
	[Id] [uniqueidentifier] NOT NULL,
	[BatchId] [uniqueidentifier] NOT NULL,
	[ClassifiedBatchId] [uniqueidentifier] NULL,
	[ConditionRating] [int] NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
	[ClassifiedAt] [datetime2](7) NOT NULL,
	[ClassifiedByStaffId] [uniqueidentifier] NOT NULL,
	[ClothingType] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[FabricType] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[GarmentGroup] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Gender] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ItemCode] [nvarchar](450) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Notes] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ProcessingDirection] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Size] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Status] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[TargetUser] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ImageUrls] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ClothingTypeId] [uniqueidentifier] NULL,
	[ConditionGradeId] [uniqueidentifier] NULL,
	[FabricTypeId] [uniqueidentifier] NULL,
	[GarmentGroupId] [uniqueidentifier] NULL,
	[GenderId] [uniqueidentifier] NULL,
	[SizeId] [uniqueidentifier] NULL,
	[TargetUserId] [uniqueidentifier] NULL,
	[ScoringSnapshot] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[WeightedScore] [decimal](18, 2) NULL,
 CONSTRAINT [PK_ClassifiedItems] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[ConditionAnswers](
	[Id] [uniqueidentifier] NOT NULL,
	[ConditionQuestionId] [uniqueidentifier] NOT NULL,
	[AnswerText] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ConditionRating] [int] NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
 CONSTRAINT [PK_ConditionAnswers] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[ConditionQuestions](
	[Id] [uniqueidentifier] NOT NULL,
	[QuestionText] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[DisplayOrder] [int] NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
	[Weight] [decimal](18, 2) NOT NULL,
 CONSTRAINT [PK_ConditionQuestions] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[DirectChatMessages](
	[Id] [uniqueidentifier] NOT NULL,
	[SenderId] [uniqueidentifier] NOT NULL,
	[RecipientId] [uniqueidentifier] NOT NULL,
	[Message] [nvarchar](2000) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[SentAt] [datetime2](7) NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
 CONSTRAINT [PK_DirectChatMessages] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[DistributionItems](
	[Id] [uniqueidentifier] NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
	[ConditionRating] [int] NOT NULL,
	[DistributionRequestId] [uniqueidentifier] NOT NULL,
	[Notes] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[RequestedQuantity] [int] NOT NULL,
	[ApprovedQuantity] [int] NOT NULL,
	[InventoryId] [uniqueidentifier] NOT NULL,
	[IssuedQuantity] [int] NOT NULL,
	[IssuedWeight] [decimal](18, 2) NOT NULL,
	[RequestedWeight] [decimal](18, 2) NOT NULL,
 CONSTRAINT [PK_DistributionItems] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[DistributionRequests](
	[Id] [uniqueidentifier] NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
	[ActualDeliveryTime] [datetime2](7) NULL,
	[ApprovedAt] [datetime2](7) NULL,
	[CarrierName] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[EstimatedDeliveryTime] [datetime2](7) NULL,
	[RejectReason] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[RequestNotes] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[RequestedAt] [datetime2](7) NOT NULL,
	[ShippingFee] [decimal](18, 2) NOT NULL,
	[ShippingPaymentType] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Status] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ToAddress] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[TrackingCode] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[UserId] [uniqueidentifier] NOT NULL,
	[WarehouseId] [uniqueidentifier] NOT NULL,
	[ApprovedByManagerId] [uniqueidentifier] NULL,
	[GhnOrderCode] [nvarchar](450) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[GhnStatus] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[GhnUpdatedAt] [datetime2](7) NULL,
	[IssueSlipCode] [nvarchar](450) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[RecipientName] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[RecipientPhone] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[WarehouseIssuedAt] [datetime2](7) NULL,
	[WarehouseIssuedByStaffId] [uniqueidentifier] NULL,
	[RequestCode] [nvarchar](32) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_DistributionRequests] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[DonationChatMessages](
	[Id] [uniqueidentifier] NOT NULL,
	[DonationRequestId] [uniqueidentifier] NOT NULL,
	[SenderId] [uniqueidentifier] NOT NULL,
	[Message] [nvarchar](2000) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[SentAt] [datetime2](7) NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
 CONSTRAINT [PK_DonationChatMessages] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[DonationPointRules](
	[Id] [uniqueidentifier] NOT NULL,
	[PointsPerKg] [int] NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
 CONSTRAINT [PK_DonationPointRules] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[DonationPointTransactions](
	[Id] [uniqueidentifier] NOT NULL,
	[UserId] [uniqueidentifier] NOT NULL,
	[DonationRequestId] [uniqueidentifier] NULL,
	[Points] [int] NOT NULL,
	[BalanceAfter] [int] NOT NULL,
	[WeightKg] [decimal](18, 2) NULL,
	[Type] [nvarchar](450) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Description] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[OccurredAt] [datetime2](7) NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
 CONSTRAINT [PK_DonationPointTransactions] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[DonationRequests](
	[Id] [uniqueidentifier] NOT NULL,
	[DonorId] [uniqueidentifier] NOT NULL,
	[WarehouseId] [uniqueidentifier] NOT NULL,
	[ImageUrls] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Description] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[EstimateWeight] [decimal](18, 2) NOT NULL,
	[ActualWeight] [decimal](18, 2) NULL,
	[PickupAddress] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[PickupDate] [datetime2](7) NULL,
	[RejectReason] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Status] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
	[ContactName] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ContactPhoneNumber] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[DeliveryMethod] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[RequestCode] [nvarchar](32) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[DropOffMethod] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CarrierName] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[TrackingCode] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_DonationRequests] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[FundContribution](
	[Id] [uniqueidentifier] NOT NULL,
	[OrderCode] [bigint] IDENTITY(20260922000000,1) NOT NULL,
	[UserId] [uniqueidentifier] NOT NULL,
	[RequestKey] [uniqueidentifier] NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[Status] [nvarchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[PaymentLinkId] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CheckoutUrl] [nvarchar](1000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedAt] [datetimeoffset](7) NOT NULL,
	[ExpiresAt] [datetimeoffset](7) NOT NULL,
	[ConfirmedAt] [datetimeoffset](7) NULL,
 CONSTRAINT [PK_FundContribution] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[FundExpense](
	[Id] [uniqueidentifier] NOT NULL,
	[RequestKey] [uniqueidentifier] NOT NULL,
	[ManagerId] [uniqueidentifier] NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[Title] [nvarchar](160) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Description] [nvarchar](2000) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[SpentOn] [date] NOT NULL,
	[PublishedAt] [datetimeoffset](7) NOT NULL,
	[Receipt] [varbinary](max) NOT NULL,
	[ReceiptContentType] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[VoidedAt] [datetimeoffset](7) NULL,
	[VoidedBy] [uniqueidentifier] NULL,
	[VoidReason] [nvarchar](1000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_FundExpense] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[FundStatement](
	[Id] [uniqueidentifier] NOT NULL,
	[Period] [date] NOT NULL,
	[RequestKey] [uniqueidentifier] NOT NULL,
	[ManagerId] [uniqueidentifier] NOT NULL,
	[ClosingBalance] [decimal](18, 2) NOT NULL,
	[Note] [nvarchar](2000) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Document] [varbinary](max) NOT NULL,
	[ContentType] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[PublishedAt] [datetimeoffset](7) NOT NULL,
 CONSTRAINT [PK_FundStatement] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[FundStatementReminder](
	[Period] [date] NOT NULL,
	[ManagerId] [uniqueidentifier] NOT NULL,
	[SentAt] [datetimeoffset](7) NOT NULL,
 CONSTRAINT [PK_FundStatementReminder] PRIMARY KEY CLUSTERED
(
	[Period] ASC,
	[ManagerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[InspectionAnswers](
	[Id] [uniqueidentifier] NOT NULL,
	[ClassifiedItemId] [uniqueidentifier] NOT NULL,
	[ConditionQuestionId] [uniqueidentifier] NOT NULL,
	[ConditionAnswerId] [uniqueidentifier] NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
 CONSTRAINT [PK_InspectionAnswers] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[IntakeBatchDonationRequests](
	[IntakeBatchId] [uniqueidentifier] NOT NULL,
	[DonationRequestId] [uniqueidentifier] NOT NULL,
	[AddedAt] [datetime2](7) NOT NULL,
	[AddedByStaffId] [uniqueidentifier] NOT NULL,
	[Id] [uniqueidentifier] NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
 CONSTRAINT [PK_IntakeBatchDonationRequests] PRIMARY KEY CLUSTERED
(
	[IntakeBatchId] ASC,
	[DonationRequestId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[IntakeBatches](
	[Id] [uniqueidentifier] NOT NULL,
	[WarehouseId] [uniqueidentifier] NOT NULL,
	[IntakeDate] [datetime2](7) NOT NULL,
	[TotalWeight] [decimal](18, 2) NOT NULL,
	[Status] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Note] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
	[BatchCode] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[BatchImages] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ReceivingTeamId] [uniqueidentifier] NULL,
	[CompletedAt] [datetime2](7) NULL,
	[RouteName] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[StartedAt] [datetime2](7) NULL,
	[ShiftId] [uniqueidentifier] NOT NULL,
	[ClassificationReceivedAt] [datetime2](7) NULL,
	[ClassificationReceivedByStaffId] [uniqueidentifier] NULL,
	[SentToClassificationAt] [datetime2](7) NULL,
	[ClassificationAreaName] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ClassificationCompletedAt] [datetime2](7) NULL,
	[ClassificationCompletedByStaffId] [uniqueidentifier] NULL,
	[ClassificationStartedAt] [datetime2](7) NULL,
	[ClassificationStartedByStaffId] [uniqueidentifier] NULL,
	[ClassifiedAreaPlacedAt] [datetime2](7) NULL,
	[ClassifiedAreaPlacedByStaffId] [uniqueidentifier] NULL,
	[CountedAt] [datetime2](7) NULL,
	[CountedByStaffId] [uniqueidentifier] NULL,
	[CountedItemCount] [int] NULL,
	[CountedTotalWeight] [decimal](18, 2) NULL,
	[CountingNotes] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ClassificationAssignedAt] [datetime2](7) NULL,
	[ClassificationAssignedByManagerId] [uniqueidentifier] NULL,
	[ClassificationTeamId] [uniqueidentifier] NULL,
	[CurrentAreaId] [uniqueidentifier] NULL,
	[WarehouseReceivedAt] [datetime2](7) NULL,
	[WarehouseReceivedByStaffId] [uniqueidentifier] NULL,
	[CurrentAreaGroupId] [uniqueidentifier] NULL,
	[CurrentStorageLocationId] [uniqueidentifier] NULL,
	[ProcessingOperationOutputId] [uniqueidentifier] NULL,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_IntakeBatches] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[Inventories](
	[Id] [uniqueidentifier] NOT NULL,
	[WarehouseId] [uniqueidentifier] NOT NULL,
	[ConditionRating] [int] NOT NULL,
	[Quantity] [int] NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
	[AreaGroupId] [uniqueidentifier] NULL,
	[TotalWeight] [decimal](18, 2) NOT NULL,
	[ClassifiedBatchId] [uniqueidentifier] NULL,
	[ClothingType] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[FabricType] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[GarmentGroup] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Gender] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ProcessingDirection] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ReservedQuantity] [int] NOT NULL,
	[ReservedWeight] [decimal](18, 2) NOT NULL,
	[Size] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Sku] [nvarchar](450) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Status] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[StorageLocationId] [uniqueidentifier] NULL,
	[TargetUser] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ClothingTypeId] [uniqueidentifier] NULL,
	[ConditionGradeId] [uniqueidentifier] NULL,
	[FabricTypeId] [uniqueidentifier] NULL,
	[GarmentGroupId] [uniqueidentifier] NULL,
	[GenderId] [uniqueidentifier] NULL,
	[SizeId] [uniqueidentifier] NULL,
	[TargetUserId] [uniqueidentifier] NULL,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_Inventories] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[InventoryTransactions](
	[Id] [uniqueidentifier] NOT NULL,
	[WarehouseId] [uniqueidentifier] NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
	[Notes] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ReferenceId] [uniqueidentifier] NULL,
	[ReferenceType] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Status] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[TransactionCode] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[TransactionType] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[PerformedAt] [datetime2](7) NOT NULL,
	[PerformedByStaffId] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_InventoryTransactions] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[Notifications](
	[Id] [uniqueidentifier] NOT NULL,
	[UserId] [uniqueidentifier] NOT NULL,
	[DonationRequestId] [uniqueidentifier] NULL,
	[Type] [nvarchar](60) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Title] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Message] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[TargetUrl] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IsRead] [bit] NOT NULL,
	[ReadAt] [datetime2](7) NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
 CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[OperationalTeams](
	[Id] [uniqueidentifier] NOT NULL,
	[ShiftId] [uniqueidentifier] NOT NULL,
	[TeamType] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[TeamName] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
	[CompletedAt] [datetime2](7) NULL,
	[CompletedByStaffId] [uniqueidentifier] NULL,
	[StartedAt] [datetime2](7) NULL,
	[StartedByStaffId] [uniqueidentifier] NULL,
	[Status] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[MaxReceivingRequests] [int] NULL,
	[MaxReceivingWeightKg] [decimal](18, 2) NULL,
 CONSTRAINT [PK_OperationalTeams] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[PickupAssignments](
	[Id] [uniqueidentifier] NOT NULL,
	[DonorRequestId] [uniqueidentifier] NOT NULL,
	[TeamId] [uniqueidentifier] NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
	[AreaKey] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[IntakeBatchId] [uniqueidentifier] NOT NULL,
	[Notes] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ProcessedAt] [datetime2](7) NULL,
	[RouteOrder] [int] NOT NULL,
	[ShiftId] [uniqueidentifier] NOT NULL,
	[Status] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_PickupAssignments] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[ProcessingOperationInputs](
	[Id] [uniqueidentifier] NOT NULL,
	[ProcessingOperationId] [uniqueidentifier] NOT NULL,
	[InventoryId] [uniqueidentifier] NOT NULL,
	[ClassifiedBatchId] [uniqueidentifier] NULL,
	[RequestedQuantity] [int] NOT NULL,
	[RequestedWeight] [decimal](18, 2) NOT NULL,
	[IssuedQuantity] [int] NOT NULL,
	[IssuedWeight] [decimal](18, 2) NOT NULL,
	[Notes] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
 CONSTRAINT [PK_ProcessingOperationInputs] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[ProcessingOperationOutputs](
	[Id] [uniqueidentifier] NOT NULL,
	[ProcessingOperationId] [uniqueidentifier] NOT NULL,
	[OutputType] [nvarchar](40) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Quantity] [int] NOT NULL,
	[Weight] [decimal](18, 2) NOT NULL,
	[ReturnedQuantity] [int] NOT NULL,
	[ReturnedWeight] [decimal](18, 2) NOT NULL,
	[RecordedByStaffId] [uniqueidentifier] NULL,
	[RecordedAt] [datetime2](7) NULL,
	[Notes] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
 CONSTRAINT [PK_ProcessingOperationOutputs] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[ProcessingOperations](
	[Id] [uniqueidentifier] NOT NULL,
	[OperationCode] [nvarchar](32) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[OperationType] [nvarchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Status] [nvarchar](40) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[WarehouseId] [uniqueidentifier] NOT NULL,
	[OrganizationId] [uniqueidentifier] NOT NULL,
	[CreatedByUserId] [uniqueidentifier] NULL,
	[ApprovedByManagerId] [uniqueidentifier] NULL,
	[IssuedByStaffId] [uniqueidentifier] NULL,
	[RequestedAt] [datetime2](7) NOT NULL,
	[ApprovedAt] [datetime2](7) NULL,
	[IssuedAt] [datetime2](7) NULL,
	[OrganizationReceivedAt] [datetime2](7) NULL,
	[ProcessingStartedAt] [datetime2](7) NULL,
	[ProcessingCompletedAt] [datetime2](7) NULL,
	[OutputReturnedAt] [datetime2](7) NULL,
	[TrackingCode] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CarrierName] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[RequestNotes] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[OrganizationRejectionReason] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CompletionNotes] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
	[ApprovedByOrganizationId] [uniqueidentifier] NULL,
	[ManagerRejectionReason] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ManagerRespondedAt] [datetime2](7) NULL,
	[OrganizationRespondedAt] [datetime2](7) NULL,
	[RejectedAt] [datetime2](7) NULL,
	[RejectedByManagerId] [uniqueidentifier] NULL,
	[RowVersion] [timestamp] NOT NULL,
	[GhnOrderCode] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[GhnStatus] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[GhnUpdatedAt] [datetime2](7) NULL,
	[ExpectedReturnDate] [datetime2](7) NULL,
	[ReturnCarrierName] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ReturnDispatchedAt] [datetime2](7) NULL,
	[ReturnNotes] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ReturnTrackingCode] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_ProcessingOperations] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[ProcessingShipmentEvents](
	[Id] [uniqueidentifier] NOT NULL,
	[ProcessingOperationId] [uniqueidentifier] NOT NULL,
	[Status] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Description] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[OccurredAt] [datetime2](7) NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
 CONSTRAINT [PK_ProcessingShipmentEvents] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[Roles](
	[Id] [uniqueidentifier] NOT NULL,
	[RoleName] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Description] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
 CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[Shifts](
	[Id] [uniqueidentifier] NOT NULL,
	[WarehouseId] [uniqueidentifier] NOT NULL,
	[ShiftDate] [datetime2](7) NOT NULL,
	[StartTime] [time](7) NOT NULL,
	[EndTime] [time](7) NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
	[ShiftName] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Status] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CompletedAt] [datetime2](7) NULL,
	[StartedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_Shifts] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[ShipmentStatusHistories](
	[Id] [uniqueidentifier] NOT NULL,
	[DistributionRequestId] [uniqueidentifier] NOT NULL,
	[Status] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Description] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Source] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[OccurredAt] [datetime2](7) NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
 CONSTRAINT [PK_ShipmentStatusHistories] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[StorageLocations](
	[Id] [uniqueidentifier] NOT NULL,
	[WarehouseId] [uniqueidentifier] NOT NULL,
	[AreaId] [uniqueidentifier] NOT NULL,
	[AreaGroupId] [uniqueidentifier] NULL,
	[LocationCode] [nvarchar](450) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[AisleCode] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[RackCode] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ShelfCode] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[BinCode] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[PreferredGarmentGroup] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PreferredProcessingDirection] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CapacityKg] [decimal](18, 2) NOT NULL,
	[CurrentWeightKg] [decimal](18, 2) NOT NULL,
	[Status] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_StorageLocations] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[TeamMembers](
	[Id] [uniqueidentifier] NOT NULL,
	[TeamId] [uniqueidentifier] NOT NULL,
	[StaffId] [uniqueidentifier] NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
 CONSTRAINT [PK_TeamMembers] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[TransactionItems](
	[Id] [uniqueidentifier] NOT NULL,
	[TransactionId] [uniqueidentifier] NOT NULL,
	[InventoryId] [uniqueidentifier] NOT NULL,
	[ClassifiedBatchId] [uniqueidentifier] NULL,
	[Quantity] [int] NOT NULL,
	[Weight] [decimal](18, 2) NOT NULL,
	[Notes] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
	[DestinationLocationId] [uniqueidentifier] NULL,
	[QuantityAfter] [int] NOT NULL,
	[QuantityBefore] [int] NOT NULL,
	[SourceLocationId] [uniqueidentifier] NULL,
	[WeightAfter] [decimal](18, 2) NOT NULL,
	[WeightBefore] [decimal](18, 2) NOT NULL,
 CONSTRAINT [PK_TransactionItems] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[TransferItems](
	[Id] [uniqueidentifier] NOT NULL,
	[TransferId] [uniqueidentifier] NOT NULL,
	[ToAreaId] [uniqueidentifier] NOT NULL,
	[BatchId] [uniqueidentifier] NULL,
	[ClassifiedBatchId] [uniqueidentifier] NULL,
	[RequestStaffId] [uniqueidentifier] NOT NULL,
	[ApproveStaffId] [uniqueidentifier] NULL,
	[ReceivedAt] [datetime2](7) NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
 CONSTRAINT [PK_TransferItems] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[TransferRequests](
	[Id] [uniqueidentifier] NOT NULL,
	[WarehouseId] [uniqueidentifier] NOT NULL,
	[FromAreaId] [uniqueidentifier] NOT NULL,
	[ToAreaId] [uniqueidentifier] NOT NULL,
	[Status] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ReceivedAt] [datetime2](7) NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
 CONSTRAINT [PK_TransferRequests] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[Users](
	[Id] [uniqueidentifier] NOT NULL,
	[FullName] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Email] [nvarchar](450) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[PhoneNumber] [nvarchar](450) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[RoleId] [uniqueidentifier] NOT NULL,
	[WarehouseId] [uniqueidentifier] NULL,
	[AvatarUrl] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PasswordHash] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[UserName] [nvarchar](450) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Address] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[UserStatus] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[DonationPoint] [int] NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
	[EmailConfirmed] [bit] NOT NULL,
	[CertificateImageUrl] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[RepresentativeName] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[TaxCode] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[UserVerificationCodes](
	[Id] [uniqueidentifier] NOT NULL,
	[UserId] [uniqueidentifier] NOT NULL,
	[CodeHash] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ExpiresAt] [datetime2](7) NOT NULL,
	[VerifiedAt] [datetime2](7) NULL,
	[FailedAttempts] [int] NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
	[Purpose] [nvarchar](32) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_UserVerificationCodes] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[VoucherCodes](
	[Id] [uniqueidentifier] NOT NULL,
	[VoucherId] [uniqueidentifier] NOT NULL,
	[Code] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ExpireDate] [datetime2](7) NOT NULL,
	[Status] [nvarchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[RedeemedByUserId] [uniqueidentifier] NULL,
	[RedeemedAt] [datetime2](7) NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
 CONSTRAINT [PK_VoucherCodes] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[VoucherRedemptions](
	[Id] [uniqueidentifier] NOT NULL,
	[UserId] [uniqueidentifier] NOT NULL,
	[VoucherId] [uniqueidentifier] NOT NULL,
	[VoucherCodeId] [uniqueidentifier] NOT NULL,
	[PointsSpent] [int] NOT NULL,
	[RedeemedAt] [datetime2](7) NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
 CONSTRAINT [PK_VoucherRedemptions] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[Vouchers](
	[Id] [uniqueidentifier] NOT NULL,
	[VoucherUrl] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[RequiredPoints] [int] NOT NULL,
	[Description] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ExpireDate] [datetime2](7) NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
	[ImageUrl] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Name] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[PartnerName] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[StartDate] [datetime2](7) NOT NULL,
	[Status] [nvarchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[TermsAndConditions] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Value] [decimal](18, 2) NOT NULL,
 CONSTRAINT [PK_Vouchers] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[WarehouseAreas](
	[Id] [uniqueidentifier] NOT NULL,
	[WarehouseId] [uniqueidentifier] NOT NULL,
	[AreaName] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Description] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CapacityKg] [decimal](18, 2) NOT NULL,
	[CurrentKg] [decimal](18, 2) NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
	[AreaType] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[RowVersion] [timestamp] NOT NULL,
	[ProcessingDirection] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_WarehouseAreas] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[Warehouses](
	[Id] [uniqueidentifier] NOT NULL,
	[WarehouseName] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Address] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[PhoneNumber] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Email] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Description] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
	[CurrentWeight] [decimal](18, 2) NOT NULL,
	[TotalCapacityKg] [decimal](18, 2) NOT NULL,
	[Latitude] [float] NULL,
	[Longitude] [float] NULL,
	[ServiceRadiusKm] [float] NOT NULL,
	[RowVersion] [timestamp] NOT NULL,
	[MaxReceivingRequests] [int] NOT NULL,
	[MaxReceivingWeightKg] [decimal](18, 2) NOT NULL,
 CONSTRAINT [PK_Warehouses] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_NULLS ON'';
EXEC sys.sp_executesql N''SET QUOTED_IDENTIFIER ON'';
EXEC sys.sp_executesql N''CREATE TABLE [dbo].[WorkScheduleTemplates](
	[Id] [uniqueidentifier] NOT NULL,
	[WarehouseId] [uniqueidentifier] NOT NULL,
	[Year] [int] NOT NULL,
	[WorkingDays] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[MorningStartTime] [time](7) NOT NULL,
	[MorningEndTime] [time](7) NOT NULL,
	[AfternoonStartTime] [time](7) NOT NULL,
	[AfternoonEndTime] [time](7) NOT NULL,
	[CreateAt] [datetime2](7) NULL,
	[UpdateAt] [datetime2](7) NULL,
	[DeleteAt] [datetime2](7) NULL,
	[CreatedBy] [uniqueidentifier] NULL,
	[UpdatedBy] [uniqueidentifier] NULL,
	[DeletedBy] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
 CONSTRAINT [PK_WorkScheduleTemplates] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
'';
EXEC sys.sp_executesql N''SET ANSI_PADDING ON
'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_AiPromptConfigurations_Feature] ON [dbo].[AiPromptConfigurations]
(
	[Feature] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_AreaGroups_AreaId] ON [dbo].[AreaGroups]
(
	[AreaId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''SET ANSI_PADDING ON
'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_Categories_Code] ON [dbo].[Categories]
(
	[Code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''SET ANSI_PADDING ON
'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_Categories_Type_ParentId_Name] ON [dbo].[Categories]
(
	[Type] ASC,
	[ParentId] ASC,
	[Name] ASC
)
WHERE ([ParentId] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ClassificationBatchTransfers_FromTeamId] ON [dbo].[ClassificationBatchTransfers]
(
	[FromTeamId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ClassificationBatchTransfers_IntakeBatchId] ON [dbo].[ClassificationBatchTransfers]
(
	[IntakeBatchId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ClassificationBatchTransfers_StaffId] ON [dbo].[ClassificationBatchTransfers]
(
	[StaffId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ClassificationBatchTransfers_ToTeamId] ON [dbo].[ClassificationBatchTransfers]
(
	[ToTeamId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ClassifiedBatchDonationRequests_DonationRequestId] ON [dbo].[ClassifiedBatchDonationRequests]
(
	[DonationRequestId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ClassifiedBatchDonationRequests_IntakeBatchId] ON [dbo].[ClassifiedBatchDonationRequests]
(
	[IntakeBatchId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ClassifiedBatches_AreaId] ON [dbo].[ClassifiedBatches]
(
	[AreaId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ClassifiedBatches_GroupId] ON [dbo].[ClassifiedBatches]
(
	[GroupId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''SET ANSI_PADDING ON
'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_ClassifiedBatches_GroupKey] ON [dbo].[ClassifiedBatches]
(
	[GroupKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ClassifiedBatches_PlacedInClassificationAreaByStaffId] ON [dbo].[ClassifiedBatches]
(
	[PlacedInClassificationAreaByStaffId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ClassifiedBatches_ProcessingOperationOutputId] ON [dbo].[ClassifiedBatches]
(
	[ProcessingOperationOutputId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ClassifiedBatches_RemovedFromClassificationAreaByStaffId] ON [dbo].[ClassifiedBatches]
(
	[RemovedFromClassificationAreaByStaffId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ClassifiedBatches_SentToWarehouseByStaffId] ON [dbo].[ClassifiedBatches]
(
	[SentToWarehouseByStaffId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ClassifiedBatches_StorageLocationId] ON [dbo].[ClassifiedBatches]
(
	[StorageLocationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ClassifiedBatches_StoredByStaffId] ON [dbo].[ClassifiedBatches]
(
	[StoredByStaffId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ClassifiedBatches_WarehouseId] ON [dbo].[ClassifiedBatches]
(
	[WarehouseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ClassifiedBatches_WarehouseReceivedByStaffId] ON [dbo].[ClassifiedBatches]
(
	[WarehouseReceivedByStaffId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ClassifiedItems_BatchId] ON [dbo].[ClassifiedItems]
(
	[BatchId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ClassifiedItems_ClassifiedBatchId] ON [dbo].[ClassifiedItems]
(
	[ClassifiedBatchId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ClassifiedItems_ClassifiedByStaffId] ON [dbo].[ClassifiedItems]
(
	[ClassifiedByStaffId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''SET ANSI_PADDING ON
'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_ClassifiedItems_ItemCode] ON [dbo].[ClassifiedItems]
(
	[ItemCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ConditionAnswers_ConditionQuestionId] ON [dbo].[ConditionAnswers]
(
	[ConditionQuestionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_DirectChatMessages_RecipientId] ON [dbo].[DirectChatMessages]
(
	[RecipientId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_DirectChatMessages_SenderId_RecipientId_SentAt] ON [dbo].[DirectChatMessages]
(
	[SenderId] ASC,
	[RecipientId] ASC,
	[SentAt] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_DistributionItems_DistributionRequestId] ON [dbo].[DistributionItems]
(
	[DistributionRequestId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_DistributionItems_InventoryId] ON [dbo].[DistributionItems]
(
	[InventoryId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_DistributionRequests_ApprovedByManagerId] ON [dbo].[DistributionRequests]
(
	[ApprovedByManagerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''SET ANSI_PADDING ON
'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_DistributionRequests_GhnOrderCode] ON [dbo].[DistributionRequests]
(
	[GhnOrderCode] ASC
)
WHERE ([GhnOrderCode] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''SET ANSI_PADDING ON
'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_DistributionRequests_IssueSlipCode] ON [dbo].[DistributionRequests]
(
	[IssueSlipCode] ASC
)
WHERE ([IssueSlipCode] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''SET ANSI_PADDING ON
'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_DistributionRequests_RequestCode] ON [dbo].[DistributionRequests]
(
	[RequestCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_DistributionRequests_UserId] ON [dbo].[DistributionRequests]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_DistributionRequests_WarehouseId] ON [dbo].[DistributionRequests]
(
	[WarehouseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_DistributionRequests_WarehouseIssuedByStaffId] ON [dbo].[DistributionRequests]
(
	[WarehouseIssuedByStaffId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_DonationChatMessages_DonationRequestId_SentAt] ON [dbo].[DonationChatMessages]
(
	[DonationRequestId] ASC,
	[SentAt] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_DonationChatMessages_SenderId] ON [dbo].[DonationChatMessages]
(
	[SenderId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''SET ANSI_PADDING ON
'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_DonationPointTransactions_DonationRequestId_Type] ON [dbo].[DonationPointTransactions]
(
	[DonationRequestId] ASC,
	[Type] ASC
)
WHERE ([DonationRequestId] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_DonationPointTransactions_UserId] ON [dbo].[DonationPointTransactions]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_DonationRequests_DonorId] ON [dbo].[DonationRequests]
(
	[DonorId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''SET ANSI_PADDING ON
'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_DonationRequests_RequestCode] ON [dbo].[DonationRequests]
(
	[RequestCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_DonationRequests_WarehouseId] ON [dbo].[DonationRequests]
(
	[WarehouseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_FundContribution_OrderCode] ON [dbo].[FundContribution]
(
	[OrderCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''SET ANSI_PADDING ON
'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_FundContribution_PaymentLinkId] ON [dbo].[FundContribution]
(
	[PaymentLinkId] ASC
)
WHERE ([PaymentLinkId] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_FundContribution_UserId_RequestKey] ON [dbo].[FundContribution]
(
	[UserId] ASC,
	[RequestKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_FundExpense_ManagerId_RequestKey] ON [dbo].[FundExpense]
(
	[ManagerId] ASC,
	[RequestKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_FundStatement_ManagerId_RequestKey] ON [dbo].[FundStatement]
(
	[ManagerId] ASC,
	[RequestKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_FundStatement_Period] ON [dbo].[FundStatement]
(
	[Period] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_FundStatementReminder_ManagerId] ON [dbo].[FundStatementReminder]
(
	[ManagerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_InspectionAnswers_ClassifiedItemId_ConditionQuestionId] ON [dbo].[InspectionAnswers]
(
	[ClassifiedItemId] ASC,
	[ConditionQuestionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_InspectionAnswers_ConditionAnswerId] ON [dbo].[InspectionAnswers]
(
	[ConditionAnswerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_InspectionAnswers_ConditionQuestionId] ON [dbo].[InspectionAnswers]
(
	[ConditionQuestionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_IntakeBatchDonationRequests_AddedByStaffId] ON [dbo].[IntakeBatchDonationRequests]
(
	[AddedByStaffId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_IntakeBatchDonationRequests_DonationRequestId] ON [dbo].[IntakeBatchDonationRequests]
(
	[DonationRequestId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_IntakeBatches_ClassificationAssignedByManagerId] ON [dbo].[IntakeBatches]
(
	[ClassificationAssignedByManagerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_IntakeBatches_ClassificationCompletedByStaffId] ON [dbo].[IntakeBatches]
(
	[ClassificationCompletedByStaffId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_IntakeBatches_ClassificationReceivedByStaffId] ON [dbo].[IntakeBatches]
(
	[ClassificationReceivedByStaffId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_IntakeBatches_ClassificationStartedByStaffId] ON [dbo].[IntakeBatches]
(
	[ClassificationStartedByStaffId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_IntakeBatches_ClassificationTeamId] ON [dbo].[IntakeBatches]
(
	[ClassificationTeamId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_IntakeBatches_ClassifiedAreaPlacedByStaffId] ON [dbo].[IntakeBatches]
(
	[ClassifiedAreaPlacedByStaffId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_IntakeBatches_CountedByStaffId] ON [dbo].[IntakeBatches]
(
	[CountedByStaffId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_IntakeBatches_CurrentAreaGroupId] ON [dbo].[IntakeBatches]
(
	[CurrentAreaGroupId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_IntakeBatches_CurrentAreaId] ON [dbo].[IntakeBatches]
(
	[CurrentAreaId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_IntakeBatches_CurrentStorageLocationId] ON [dbo].[IntakeBatches]
(
	[CurrentStorageLocationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_IntakeBatches_ProcessingOperationOutputId] ON [dbo].[IntakeBatches]
(
	[ProcessingOperationOutputId] ASC
)
WHERE ([ProcessingOperationOutputId] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_IntakeBatches_ReceivingTeamId] ON [dbo].[IntakeBatches]
(
	[ReceivingTeamId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_IntakeBatches_ShiftId_ReceivingTeamId] ON [dbo].[IntakeBatches]
(
	[ShiftId] ASC,
	[ReceivingTeamId] ASC
)
WHERE ([ReceivingTeamId] IS NOT NULL AND [IsActive]=(1))
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_IntakeBatches_WarehouseId] ON [dbo].[IntakeBatches]
(
	[WarehouseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_IntakeBatches_WarehouseReceivedByStaffId] ON [dbo].[IntakeBatches]
(
	[WarehouseReceivedByStaffId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_Inventories_AreaGroupId] ON [dbo].[Inventories]
(
	[AreaGroupId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_Inventories_ClassifiedBatchId] ON [dbo].[Inventories]
(
	[ClassifiedBatchId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''SET ANSI_PADDING ON
'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_Inventories_Sku] ON [dbo].[Inventories]
(
	[Sku] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_Inventories_StorageLocationId] ON [dbo].[Inventories]
(
	[StorageLocationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_Inventories_WarehouseId] ON [dbo].[Inventories]
(
	[WarehouseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_InventoryTransactions_PerformedByStaffId] ON [dbo].[InventoryTransactions]
(
	[PerformedByStaffId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_InventoryTransactions_ReferenceId] ON [dbo].[InventoryTransactions]
(
	[ReferenceId] ASC
)
WHERE ([ReferenceType]=''''ProcessingOperation'''' AND [ReferenceId] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_InventoryTransactions_WarehouseId] ON [dbo].[InventoryTransactions]
(
	[WarehouseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_Notifications_DonationRequestId] ON [dbo].[Notifications]
(
	[DonationRequestId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_Notifications_UserId_IsRead_CreateAt] ON [dbo].[Notifications]
(
	[UserId] ASC,
	[IsRead] ASC,
	[CreateAt] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_OperationalTeams_ShiftId] ON [dbo].[OperationalTeams]
(
	[ShiftId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_PickupAssignments_DonorRequestId] ON [dbo].[PickupAssignments]
(
	[DonorRequestId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_PickupAssignments_IntakeBatchId] ON [dbo].[PickupAssignments]
(
	[IntakeBatchId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_PickupAssignments_ShiftId] ON [dbo].[PickupAssignments]
(
	[ShiftId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_PickupAssignments_TeamId] ON [dbo].[PickupAssignments]
(
	[TeamId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ProcessingOperationInputs_ClassifiedBatchId] ON [dbo].[ProcessingOperationInputs]
(
	[ClassifiedBatchId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ProcessingOperationInputs_InventoryId] ON [dbo].[ProcessingOperationInputs]
(
	[InventoryId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_ProcessingOperationInputs_ProcessingOperationId_InventoryId] ON [dbo].[ProcessingOperationInputs]
(
	[ProcessingOperationId] ASC,
	[InventoryId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ProcessingOperationOutputs_ProcessingOperationId] ON [dbo].[ProcessingOperationOutputs]
(
	[ProcessingOperationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ProcessingOperationOutputs_RecordedByStaffId] ON [dbo].[ProcessingOperationOutputs]
(
	[RecordedByStaffId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ProcessingOperations_ApprovedByManagerId] ON [dbo].[ProcessingOperations]
(
	[ApprovedByManagerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ProcessingOperations_ApprovedByOrganizationId] ON [dbo].[ProcessingOperations]
(
	[ApprovedByOrganizationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ProcessingOperations_CreatedByUserId] ON [dbo].[ProcessingOperations]
(
	[CreatedByUserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''SET ANSI_PADDING ON
'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_ProcessingOperations_GhnOrderCode] ON [dbo].[ProcessingOperations]
(
	[GhnOrderCode] ASC
)
WHERE ([GhnOrderCode] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ProcessingOperations_IssuedByStaffId] ON [dbo].[ProcessingOperations]
(
	[IssuedByStaffId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''SET ANSI_PADDING ON
'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_ProcessingOperations_OperationCode] ON [dbo].[ProcessingOperations]
(
	[OperationCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ProcessingOperations_OrganizationId] ON [dbo].[ProcessingOperations]
(
	[OrganizationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ProcessingOperations_RejectedByManagerId] ON [dbo].[ProcessingOperations]
(
	[RejectedByManagerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ProcessingOperations_WarehouseId] ON [dbo].[ProcessingOperations]
(
	[WarehouseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ProcessingShipmentEvents_ProcessingOperationId] ON [dbo].[ProcessingShipmentEvents]
(
	[ProcessingOperationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_Shifts_WarehouseId] ON [dbo].[Shifts]
(
	[WarehouseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_ShipmentStatusHistories_DistributionRequestId] ON [dbo].[ShipmentStatusHistories]
(
	[DistributionRequestId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_StorageLocations_AreaGroupId] ON [dbo].[StorageLocations]
(
	[AreaGroupId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_StorageLocations_AreaId] ON [dbo].[StorageLocations]
(
	[AreaId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''SET ANSI_PADDING ON
'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_StorageLocations_WarehouseId_LocationCode] ON [dbo].[StorageLocations]
(
	[WarehouseId] ASC,
	[LocationCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_TeamMembers_StaffId] ON [dbo].[TeamMembers]
(
	[StaffId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_TeamMembers_TeamId_StaffId] ON [dbo].[TeamMembers]
(
	[TeamId] ASC,
	[StaffId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_TransactionItems_ClassifiedBatchId] ON [dbo].[TransactionItems]
(
	[ClassifiedBatchId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_TransactionItems_DestinationLocationId] ON [dbo].[TransactionItems]
(
	[DestinationLocationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_TransactionItems_InventoryId] ON [dbo].[TransactionItems]
(
	[InventoryId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_TransactionItems_SourceLocationId] ON [dbo].[TransactionItems]
(
	[SourceLocationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_TransactionItems_TransactionId] ON [dbo].[TransactionItems]
(
	[TransactionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_TransferItems_ApproveStaffId] ON [dbo].[TransferItems]
(
	[ApproveStaffId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_TransferItems_BatchId] ON [dbo].[TransferItems]
(
	[BatchId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_TransferItems_ClassifiedBatchId] ON [dbo].[TransferItems]
(
	[ClassifiedBatchId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_TransferItems_RequestStaffId] ON [dbo].[TransferItems]
(
	[RequestStaffId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_TransferItems_ToAreaId] ON [dbo].[TransferItems]
(
	[ToAreaId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_TransferItems_TransferId] ON [dbo].[TransferItems]
(
	[TransferId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_TransferRequests_FromAreaId] ON [dbo].[TransferRequests]
(
	[FromAreaId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_TransferRequests_ToAreaId] ON [dbo].[TransferRequests]
(
	[ToAreaId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_TransferRequests_WarehouseId] ON [dbo].[TransferRequests]
(
	[WarehouseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''SET ANSI_PADDING ON
'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_Users_Email] ON [dbo].[Users]
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''SET ANSI_PADDING ON
'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_Users_PhoneNumber] ON [dbo].[Users]
(
	[PhoneNumber] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_Users_RoleId] ON [dbo].[Users]
(
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''SET ANSI_PADDING ON
'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_Users_UserName] ON [dbo].[Users]
(
	[UserName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_Users_WarehouseId] ON [dbo].[Users]
(
	[WarehouseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_UserVerificationCodes_UserId_IsActive] ON [dbo].[UserVerificationCodes]
(
	[UserId] ASC,
	[IsActive] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''SET ANSI_PADDING ON
'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_VoucherCodes_Code] ON [dbo].[VoucherCodes]
(
	[Code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_VoucherCodes_RedeemedByUserId] ON [dbo].[VoucherCodes]
(
	[RedeemedByUserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_VoucherCodes_VoucherId] ON [dbo].[VoucherCodes]
(
	[VoucherId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_VoucherRedemptions_UserId_RedeemedAt] ON [dbo].[VoucherRedemptions]
(
	[UserId] ASC,
	[RedeemedAt] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_VoucherRedemptions_VoucherCodeId] ON [dbo].[VoucherRedemptions]
(
	[VoucherCodeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_VoucherRedemptions_VoucherId] ON [dbo].[VoucherRedemptions]
(
	[VoucherId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''SET ANSI_PADDING ON
'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_Vouchers_PartnerName_Name] ON [dbo].[Vouchers]
(
	[PartnerName] ASC,
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE NONCLUSTERED INDEX [IX_WarehouseAreas_WarehouseId] ON [dbo].[WarehouseAreas]
(
	[WarehouseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''CREATE UNIQUE NONCLUSTERED INDEX [IX_WorkScheduleTemplates_WarehouseId_Year] ON [dbo].[WorkScheduleTemplates]
(
	[WarehouseId] ASC,
	[Year] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Categories] ADD  DEFAULT (N'''''''') FOR [Code]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Categories] ADD  DEFAULT ((0)) FOR [SortOrder]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Categories] ADD  DEFAULT (N'''''''') FOR [Type]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches] ADD  DEFAULT (''''0001-01-01T00:00:00.0000000'''') FOR [ClassificationDate]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches] ADD  DEFAULT (N'''''''') FOR [ClothingType]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches] ADD  DEFAULT (N'''''''') FOR [FabricType]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches] ADD  DEFAULT (N'''''''') FOR [GarmentGroup]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches] ADD  DEFAULT (N'''''''') FOR [Gender]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches] ADD  DEFAULT (N'''''''') FOR [GroupKey]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches] ADD  DEFAULT (N'''''''') FOR [ProcessingDirection]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches] ADD  DEFAULT (N'''''''') FOR [Size]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches] ADD  DEFAULT (N'''''''') FOR [TargetUser]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedItems] ADD  DEFAULT (''''0001-01-01T00:00:00.0000000'''') FOR [ClassifiedAt]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedItems] ADD  DEFAULT (''''00000000-0000-0000-0000-000000000000'''') FOR [ClassifiedByStaffId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedItems] ADD  DEFAULT (N'''''''') FOR [ClothingType]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedItems] ADD  DEFAULT (N'''''''') FOR [FabricType]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedItems] ADD  DEFAULT (N'''''''') FOR [GarmentGroup]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedItems] ADD  DEFAULT (N'''''''') FOR [Gender]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedItems] ADD  DEFAULT (N'''''''') FOR [ItemCode]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedItems] ADD  DEFAULT (N'''''''') FOR [ProcessingDirection]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedItems] ADD  DEFAULT (N'''''''') FOR [Size]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedItems] ADD  DEFAULT (N'''''''') FOR [Status]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedItems] ADD  DEFAULT (N'''''''') FOR [TargetUser]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ConditionQuestions] ADD  DEFAULT ((1.0)) FOR [Weight]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionItems] ADD  DEFAULT ((0)) FOR [ConditionRating]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionItems] ADD  DEFAULT (''''00000000-0000-0000-0000-000000000000'''') FOR [DistributionRequestId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionItems] ADD  DEFAULT ((0)) FOR [RequestedQuantity]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionItems] ADD  DEFAULT ((0)) FOR [ApprovedQuantity]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionItems] ADD  DEFAULT (''''00000000-0000-0000-0000-000000000000'''') FOR [InventoryId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionItems] ADD  DEFAULT ((0)) FOR [IssuedQuantity]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionItems] ADD  DEFAULT ((0.0)) FOR [IssuedWeight]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionItems] ADD  DEFAULT ((0.0)) FOR [RequestedWeight]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionRequests] ADD  DEFAULT (''''0001-01-01T00:00:00.0000000'''') FOR [RequestedAt]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionRequests] ADD  DEFAULT ((0.0)) FOR [ShippingFee]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionRequests] ADD  DEFAULT (N'''''''') FOR [Status]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionRequests] ADD  DEFAULT (N'''''''') FOR [ToAddress]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionRequests] ADD  DEFAULT (''''00000000-0000-0000-0000-000000000000'''') FOR [UserId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionRequests] ADD  DEFAULT (''''00000000-0000-0000-0000-000000000000'''') FOR [WarehouseId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionRequests] ADD  DEFAULT (N'''''''') FOR [RecipientName]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionRequests] ADD  DEFAULT (N'''''''') FOR [RecipientPhone]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DonationRequests] ADD  DEFAULT (N'''''''') FOR [ContactName]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DonationRequests] ADD  DEFAULT (N'''''''') FOR [ContactPhoneNumber]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DonationRequests] ADD  DEFAULT (N''''StaffPickup'''') FOR [DeliveryMethod]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DonationRequests] ADD  DEFAULT (N'''''''') FOR [RequestCode]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches] ADD  DEFAULT (N'''''''') FOR [BatchCode]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches] ADD  DEFAULT (N'''''''') FOR [RouteName]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches] ADD  DEFAULT (''''00000000-0000-0000-0000-000000000000'''') FOR [ShiftId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Inventories] ADD  DEFAULT ((0.0)) FOR [TotalWeight]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Inventories] ADD  DEFAULT (N'''''''') FOR [ClothingType]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Inventories] ADD  DEFAULT (N'''''''') FOR [FabricType]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Inventories] ADD  DEFAULT (N'''''''') FOR [GarmentGroup]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Inventories] ADD  DEFAULT (N'''''''') FOR [Gender]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Inventories] ADD  DEFAULT (N'''''''') FOR [ProcessingDirection]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Inventories] ADD  DEFAULT ((0)) FOR [ReservedQuantity]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Inventories] ADD  DEFAULT ((0.0)) FOR [ReservedWeight]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Inventories] ADD  DEFAULT (N'''''''') FOR [Size]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Inventories] ADD  DEFAULT (N'''''''') FOR [Sku]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Inventories] ADD  DEFAULT (N'''''''') FOR [Status]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Inventories] ADD  DEFAULT (N'''''''') FOR [TargetUser]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[InventoryTransactions] ADD  DEFAULT (N'''''''') FOR [Status]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[InventoryTransactions] ADD  DEFAULT (N'''''''') FOR [TransactionCode]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[InventoryTransactions] ADD  DEFAULT (N'''''''') FOR [TransactionType]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[InventoryTransactions] ADD  DEFAULT (''''0001-01-01T00:00:00.0000000'''') FOR [PerformedAt]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[InventoryTransactions] ADD  DEFAULT (''''00000000-0000-0000-0000-000000000000'''') FOR [PerformedByStaffId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[OperationalTeams] ADD  DEFAULT (N''''Scheduled'''') FOR [Status]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[PickupAssignments] ADD  DEFAULT (N'''''''') FOR [AreaKey]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[PickupAssignments] ADD  DEFAULT (''''00000000-0000-0000-0000-000000000000'''') FOR [IntakeBatchId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[PickupAssignments] ADD  DEFAULT ((0)) FOR [RouteOrder]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[PickupAssignments] ADD  DEFAULT (''''00000000-0000-0000-0000-000000000000'''') FOR [ShiftId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[PickupAssignments] ADD  DEFAULT (N'''''''') FOR [Status]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Shifts] ADD  DEFAULT (N'''''''') FOR [ShiftName]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Shifts] ADD  DEFAULT (N'''''''') FOR [Status]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransactionItems] ADD  DEFAULT ((0)) FOR [QuantityAfter]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransactionItems] ADD  DEFAULT ((0)) FOR [QuantityBefore]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransactionItems] ADD  DEFAULT ((0.0)) FOR [WeightAfter]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransactionItems] ADD  DEFAULT ((0.0)) FOR [WeightBefore]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Users] ADD  DEFAULT ((0)) FOR [DonationPoint]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Users] ADD  DEFAULT (CONVERT([bit],(0))) FOR [EmailConfirmed]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[UserVerificationCodes] ADD  DEFAULT (N''''Registration'''') FOR [Purpose]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Vouchers] ADD  DEFAULT (N'''''''') FOR [Name]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Vouchers] ADD  DEFAULT (N'''''''') FOR [PartnerName]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Vouchers] ADD  DEFAULT (''''0001-01-01T00:00:00.0000000'''') FOR [StartDate]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Vouchers] ADD  DEFAULT (N'''''''') FOR [Status]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Vouchers] ADD  DEFAULT ((0.0)) FOR [Value]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[WarehouseAreas] ADD  DEFAULT (N''''Storage'''') FOR [AreaType]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Warehouses] ADD  DEFAULT ((0.0)) FOR [CurrentWeight]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Warehouses] ADD  DEFAULT ((0.0)) FOR [TotalCapacityKg]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Warehouses] ADD  DEFAULT ((2.4000000000000000e+001)) FOR [ServiceRadiusKm]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Warehouses] ADD  DEFAULT ((8)) FOR [MaxReceivingRequests]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Warehouses] ADD  DEFAULT ((80.0)) FOR [MaxReceivingWeightKg]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[AreaGroups]  WITH CHECK ADD  CONSTRAINT [FK_AreaGroups_WarehouseAreas_AreaId] FOREIGN KEY([AreaId])
REFERENCES [dbo].[WarehouseAreas] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[AreaGroups] CHECK CONSTRAINT [FK_AreaGroups_WarehouseAreas_AreaId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassificationBatchTransfers]  WITH CHECK ADD  CONSTRAINT [FK_ClassificationBatchTransfers_IntakeBatches_IntakeBatchId] FOREIGN KEY([IntakeBatchId])
REFERENCES [dbo].[IntakeBatches] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassificationBatchTransfers] CHECK CONSTRAINT [FK_ClassificationBatchTransfers_IntakeBatches_IntakeBatchId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassificationBatchTransfers]  WITH CHECK ADD  CONSTRAINT [FK_ClassificationBatchTransfers_OperationalTeams_FromTeamId] FOREIGN KEY([FromTeamId])
REFERENCES [dbo].[OperationalTeams] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassificationBatchTransfers] CHECK CONSTRAINT [FK_ClassificationBatchTransfers_OperationalTeams_FromTeamId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassificationBatchTransfers]  WITH CHECK ADD  CONSTRAINT [FK_ClassificationBatchTransfers_OperationalTeams_ToTeamId] FOREIGN KEY([ToTeamId])
REFERENCES [dbo].[OperationalTeams] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassificationBatchTransfers] CHECK CONSTRAINT [FK_ClassificationBatchTransfers_OperationalTeams_ToTeamId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassificationBatchTransfers]  WITH CHECK ADD  CONSTRAINT [FK_ClassificationBatchTransfers_Users_StaffId] FOREIGN KEY([StaffId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassificationBatchTransfers] CHECK CONSTRAINT [FK_ClassificationBatchTransfers_Users_StaffId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatchDonationRequests]  WITH CHECK ADD  CONSTRAINT [FK_ClassifiedBatchDonationRequests_ClassifiedBatches_ClassifiedBatchId] FOREIGN KEY([ClassifiedBatchId])
REFERENCES [dbo].[ClassifiedBatches] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatchDonationRequests] CHECK CONSTRAINT [FK_ClassifiedBatchDonationRequests_ClassifiedBatches_ClassifiedBatchId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatchDonationRequests]  WITH CHECK ADD  CONSTRAINT [FK_ClassifiedBatchDonationRequests_DonationRequests_DonationRequestId] FOREIGN KEY([DonationRequestId])
REFERENCES [dbo].[DonationRequests] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatchDonationRequests] CHECK CONSTRAINT [FK_ClassifiedBatchDonationRequests_DonationRequests_DonationRequestId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatchDonationRequests]  WITH CHECK ADD  CONSTRAINT [FK_ClassifiedBatchDonationRequests_IntakeBatches_IntakeBatchId] FOREIGN KEY([IntakeBatchId])
REFERENCES [dbo].[IntakeBatches] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatchDonationRequests] CHECK CONSTRAINT [FK_ClassifiedBatchDonationRequests_IntakeBatches_IntakeBatchId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches]  WITH CHECK ADD  CONSTRAINT [FK_ClassifiedBatches_AreaGroups_GroupId] FOREIGN KEY([GroupId])
REFERENCES [dbo].[AreaGroups] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches] CHECK CONSTRAINT [FK_ClassifiedBatches_AreaGroups_GroupId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches]  WITH CHECK ADD  CONSTRAINT [FK_ClassifiedBatches_ProcessingOperationOutputs_ProcessingOperationOutputId] FOREIGN KEY([ProcessingOperationOutputId])
REFERENCES [dbo].[ProcessingOperationOutputs] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches] CHECK CONSTRAINT [FK_ClassifiedBatches_ProcessingOperationOutputs_ProcessingOperationOutputId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches]  WITH CHECK ADD  CONSTRAINT [FK_ClassifiedBatches_StorageLocations_StorageLocationId] FOREIGN KEY([StorageLocationId])
REFERENCES [dbo].[StorageLocations] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches] CHECK CONSTRAINT [FK_ClassifiedBatches_StorageLocations_StorageLocationId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches]  WITH CHECK ADD  CONSTRAINT [FK_ClassifiedBatches_Users_PlacedInClassificationAreaByStaffId] FOREIGN KEY([PlacedInClassificationAreaByStaffId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches] CHECK CONSTRAINT [FK_ClassifiedBatches_Users_PlacedInClassificationAreaByStaffId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches]  WITH CHECK ADD  CONSTRAINT [FK_ClassifiedBatches_Users_RemovedFromClassificationAreaByStaffId] FOREIGN KEY([RemovedFromClassificationAreaByStaffId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches] CHECK CONSTRAINT [FK_ClassifiedBatches_Users_RemovedFromClassificationAreaByStaffId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches]  WITH CHECK ADD  CONSTRAINT [FK_ClassifiedBatches_Users_SentToWarehouseByStaffId] FOREIGN KEY([SentToWarehouseByStaffId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches] CHECK CONSTRAINT [FK_ClassifiedBatches_Users_SentToWarehouseByStaffId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches]  WITH CHECK ADD  CONSTRAINT [FK_ClassifiedBatches_Users_StoredByStaffId] FOREIGN KEY([StoredByStaffId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches] CHECK CONSTRAINT [FK_ClassifiedBatches_Users_StoredByStaffId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches]  WITH CHECK ADD  CONSTRAINT [FK_ClassifiedBatches_Users_WarehouseReceivedByStaffId] FOREIGN KEY([WarehouseReceivedByStaffId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches] CHECK CONSTRAINT [FK_ClassifiedBatches_Users_WarehouseReceivedByStaffId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches]  WITH CHECK ADD  CONSTRAINT [FK_ClassifiedBatches_WarehouseAreas_AreaId] FOREIGN KEY([AreaId])
REFERENCES [dbo].[WarehouseAreas] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches] CHECK CONSTRAINT [FK_ClassifiedBatches_WarehouseAreas_AreaId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches]  WITH CHECK ADD  CONSTRAINT [FK_ClassifiedBatches_Warehouses_WarehouseId] FOREIGN KEY([WarehouseId])
REFERENCES [dbo].[Warehouses] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedBatches] CHECK CONSTRAINT [FK_ClassifiedBatches_Warehouses_WarehouseId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedItems]  WITH CHECK ADD  CONSTRAINT [FK_ClassifiedItems_ClassifiedBatches_ClassifiedBatchId] FOREIGN KEY([ClassifiedBatchId])
REFERENCES [dbo].[ClassifiedBatches] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedItems] CHECK CONSTRAINT [FK_ClassifiedItems_ClassifiedBatches_ClassifiedBatchId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedItems]  WITH CHECK ADD  CONSTRAINT [FK_ClassifiedItems_IntakeBatches_BatchId] FOREIGN KEY([BatchId])
REFERENCES [dbo].[IntakeBatches] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedItems] CHECK CONSTRAINT [FK_ClassifiedItems_IntakeBatches_BatchId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedItems]  WITH CHECK ADD  CONSTRAINT [FK_ClassifiedItems_Users_ClassifiedByStaffId] FOREIGN KEY([ClassifiedByStaffId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ClassifiedItems] CHECK CONSTRAINT [FK_ClassifiedItems_Users_ClassifiedByStaffId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ConditionAnswers]  WITH CHECK ADD  CONSTRAINT [FK_ConditionAnswers_ConditionQuestions_ConditionQuestionId] FOREIGN KEY([ConditionQuestionId])
REFERENCES [dbo].[ConditionQuestions] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ConditionAnswers] CHECK CONSTRAINT [FK_ConditionAnswers_ConditionQuestions_ConditionQuestionId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DirectChatMessages]  WITH CHECK ADD  CONSTRAINT [FK_DirectChatMessages_Users_RecipientId] FOREIGN KEY([RecipientId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DirectChatMessages] CHECK CONSTRAINT [FK_DirectChatMessages_Users_RecipientId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DirectChatMessages]  WITH CHECK ADD  CONSTRAINT [FK_DirectChatMessages_Users_SenderId] FOREIGN KEY([SenderId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DirectChatMessages] CHECK CONSTRAINT [FK_DirectChatMessages_Users_SenderId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionItems]  WITH CHECK ADD  CONSTRAINT [FK_DistributionItems_DistributionRequests_DistributionRequestId] FOREIGN KEY([DistributionRequestId])
REFERENCES [dbo].[DistributionRequests] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionItems] CHECK CONSTRAINT [FK_DistributionItems_DistributionRequests_DistributionRequestId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionItems]  WITH CHECK ADD  CONSTRAINT [FK_DistributionItems_Inventories_InventoryId] FOREIGN KEY([InventoryId])
REFERENCES [dbo].[Inventories] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionItems] CHECK CONSTRAINT [FK_DistributionItems_Inventories_InventoryId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionRequests]  WITH CHECK ADD  CONSTRAINT [FK_DistributionRequests_Users_ApprovedByManagerId] FOREIGN KEY([ApprovedByManagerId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionRequests] CHECK CONSTRAINT [FK_DistributionRequests_Users_ApprovedByManagerId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionRequests]  WITH CHECK ADD  CONSTRAINT [FK_DistributionRequests_Users_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionRequests] CHECK CONSTRAINT [FK_DistributionRequests_Users_UserId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionRequests]  WITH CHECK ADD  CONSTRAINT [FK_DistributionRequests_Users_WarehouseIssuedByStaffId] FOREIGN KEY([WarehouseIssuedByStaffId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionRequests] CHECK CONSTRAINT [FK_DistributionRequests_Users_WarehouseIssuedByStaffId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionRequests]  WITH CHECK ADD  CONSTRAINT [FK_DistributionRequests_Warehouses_WarehouseId] FOREIGN KEY([WarehouseId])
REFERENCES [dbo].[Warehouses] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DistributionRequests] CHECK CONSTRAINT [FK_DistributionRequests_Warehouses_WarehouseId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DonationChatMessages]  WITH CHECK ADD  CONSTRAINT [FK_DonationChatMessages_DonationRequests_DonationRequestId] FOREIGN KEY([DonationRequestId])
REFERENCES [dbo].[DonationRequests] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DonationChatMessages] CHECK CONSTRAINT [FK_DonationChatMessages_DonationRequests_DonationRequestId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DonationChatMessages]  WITH CHECK ADD  CONSTRAINT [FK_DonationChatMessages_Users_SenderId] FOREIGN KEY([SenderId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DonationChatMessages] CHECK CONSTRAINT [FK_DonationChatMessages_Users_SenderId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DonationPointTransactions]  WITH CHECK ADD  CONSTRAINT [FK_DonationPointTransactions_DonationRequests_DonationRequestId] FOREIGN KEY([DonationRequestId])
REFERENCES [dbo].[DonationRequests] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DonationPointTransactions] CHECK CONSTRAINT [FK_DonationPointTransactions_DonationRequests_DonationRequestId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DonationPointTransactions]  WITH CHECK ADD  CONSTRAINT [FK_DonationPointTransactions_Users_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DonationPointTransactions] CHECK CONSTRAINT [FK_DonationPointTransactions_Users_UserId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DonationRequests]  WITH CHECK ADD  CONSTRAINT [FK_DonationRequests_Users_DonorId] FOREIGN KEY([DonorId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DonationRequests] CHECK CONSTRAINT [FK_DonationRequests_Users_DonorId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DonationRequests]  WITH CHECK ADD  CONSTRAINT [FK_DonationRequests_Warehouses_WarehouseId] FOREIGN KEY([WarehouseId])
REFERENCES [dbo].[Warehouses] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[DonationRequests] CHECK CONSTRAINT [FK_DonationRequests_Warehouses_WarehouseId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[FundContribution]  WITH CHECK ADD  CONSTRAINT [FK_FundContribution_Users_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[FundContribution] CHECK CONSTRAINT [FK_FundContribution_Users_UserId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[FundExpense]  WITH CHECK ADD  CONSTRAINT [FK_FundExpense_Users_ManagerId] FOREIGN KEY([ManagerId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[FundExpense] CHECK CONSTRAINT [FK_FundExpense_Users_ManagerId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[FundStatement]  WITH CHECK ADD  CONSTRAINT [FK_FundStatement_Users_ManagerId] FOREIGN KEY([ManagerId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[FundStatement] CHECK CONSTRAINT [FK_FundStatement_Users_ManagerId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[FundStatementReminder]  WITH CHECK ADD  CONSTRAINT [FK_FundStatementReminder_Users_ManagerId] FOREIGN KEY([ManagerId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[FundStatementReminder] CHECK CONSTRAINT [FK_FundStatementReminder_Users_ManagerId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[InspectionAnswers]  WITH CHECK ADD  CONSTRAINT [FK_InspectionAnswers_ClassifiedItems_ClassifiedItemId] FOREIGN KEY([ClassifiedItemId])
REFERENCES [dbo].[ClassifiedItems] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[InspectionAnswers] CHECK CONSTRAINT [FK_InspectionAnswers_ClassifiedItems_ClassifiedItemId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[InspectionAnswers]  WITH CHECK ADD  CONSTRAINT [FK_InspectionAnswers_ConditionAnswers_ConditionAnswerId] FOREIGN KEY([ConditionAnswerId])
REFERENCES [dbo].[ConditionAnswers] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[InspectionAnswers] CHECK CONSTRAINT [FK_InspectionAnswers_ConditionAnswers_ConditionAnswerId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[InspectionAnswers]  WITH CHECK ADD  CONSTRAINT [FK_InspectionAnswers_ConditionQuestions_ConditionQuestionId] FOREIGN KEY([ConditionQuestionId])
REFERENCES [dbo].[ConditionQuestions] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[InspectionAnswers] CHECK CONSTRAINT [FK_InspectionAnswers_ConditionQuestions_ConditionQuestionId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatchDonationRequests]  WITH CHECK ADD  CONSTRAINT [FK_IntakeBatchDonationRequests_DonationRequests_DonationRequestId] FOREIGN KEY([DonationRequestId])
REFERENCES [dbo].[DonationRequests] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatchDonationRequests] CHECK CONSTRAINT [FK_IntakeBatchDonationRequests_DonationRequests_DonationRequestId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatchDonationRequests]  WITH CHECK ADD  CONSTRAINT [FK_IntakeBatchDonationRequests_IntakeBatches_IntakeBatchId] FOREIGN KEY([IntakeBatchId])
REFERENCES [dbo].[IntakeBatches] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatchDonationRequests] CHECK CONSTRAINT [FK_IntakeBatchDonationRequests_IntakeBatches_IntakeBatchId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatchDonationRequests]  WITH CHECK ADD  CONSTRAINT [FK_IntakeBatchDonationRequests_Users_AddedByStaffId] FOREIGN KEY([AddedByStaffId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatchDonationRequests] CHECK CONSTRAINT [FK_IntakeBatchDonationRequests_Users_AddedByStaffId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches]  WITH CHECK ADD  CONSTRAINT [FK_IntakeBatches_AreaGroups_CurrentAreaGroupId] FOREIGN KEY([CurrentAreaGroupId])
REFERENCES [dbo].[AreaGroups] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches] CHECK CONSTRAINT [FK_IntakeBatches_AreaGroups_CurrentAreaGroupId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches]  WITH CHECK ADD  CONSTRAINT [FK_IntakeBatches_OperationalTeams_ClassificationTeamId] FOREIGN KEY([ClassificationTeamId])
REFERENCES [dbo].[OperationalTeams] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches] CHECK CONSTRAINT [FK_IntakeBatches_OperationalTeams_ClassificationTeamId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches]  WITH CHECK ADD  CONSTRAINT [FK_IntakeBatches_OperationalTeams_ReceivingTeamId] FOREIGN KEY([ReceivingTeamId])
REFERENCES [dbo].[OperationalTeams] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches] CHECK CONSTRAINT [FK_IntakeBatches_OperationalTeams_ReceivingTeamId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches]  WITH CHECK ADD  CONSTRAINT [FK_IntakeBatches_ProcessingOperationOutputs_ProcessingOperationOutputId] FOREIGN KEY([ProcessingOperationOutputId])
REFERENCES [dbo].[ProcessingOperationOutputs] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches] CHECK CONSTRAINT [FK_IntakeBatches_ProcessingOperationOutputs_ProcessingOperationOutputId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches]  WITH CHECK ADD  CONSTRAINT [FK_IntakeBatches_Shifts_ShiftId] FOREIGN KEY([ShiftId])
REFERENCES [dbo].[Shifts] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches] CHECK CONSTRAINT [FK_IntakeBatches_Shifts_ShiftId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches]  WITH CHECK ADD  CONSTRAINT [FK_IntakeBatches_StorageLocations_CurrentStorageLocationId] FOREIGN KEY([CurrentStorageLocationId])
REFERENCES [dbo].[StorageLocations] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches] CHECK CONSTRAINT [FK_IntakeBatches_StorageLocations_CurrentStorageLocationId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches]  WITH CHECK ADD  CONSTRAINT [FK_IntakeBatches_Users_ClassificationAssignedByManagerId] FOREIGN KEY([ClassificationAssignedByManagerId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches] CHECK CONSTRAINT [FK_IntakeBatches_Users_ClassificationAssignedByManagerId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches]  WITH CHECK ADD  CONSTRAINT [FK_IntakeBatches_Users_ClassificationCompletedByStaffId] FOREIGN KEY([ClassificationCompletedByStaffId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches] CHECK CONSTRAINT [FK_IntakeBatches_Users_ClassificationCompletedByStaffId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches]  WITH CHECK ADD  CONSTRAINT [FK_IntakeBatches_Users_ClassificationReceivedByStaffId] FOREIGN KEY([ClassificationReceivedByStaffId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches] CHECK CONSTRAINT [FK_IntakeBatches_Users_ClassificationReceivedByStaffId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches]  WITH CHECK ADD  CONSTRAINT [FK_IntakeBatches_Users_ClassificationStartedByStaffId] FOREIGN KEY([ClassificationStartedByStaffId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches] CHECK CONSTRAINT [FK_IntakeBatches_Users_ClassificationStartedByStaffId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches]  WITH CHECK ADD  CONSTRAINT [FK_IntakeBatches_Users_ClassifiedAreaPlacedByStaffId] FOREIGN KEY([ClassifiedAreaPlacedByStaffId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches] CHECK CONSTRAINT [FK_IntakeBatches_Users_ClassifiedAreaPlacedByStaffId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches]  WITH CHECK ADD  CONSTRAINT [FK_IntakeBatches_Users_CountedByStaffId] FOREIGN KEY([CountedByStaffId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches] CHECK CONSTRAINT [FK_IntakeBatches_Users_CountedByStaffId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches]  WITH CHECK ADD  CONSTRAINT [FK_IntakeBatches_Users_WarehouseReceivedByStaffId] FOREIGN KEY([WarehouseReceivedByStaffId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches] CHECK CONSTRAINT [FK_IntakeBatches_Users_WarehouseReceivedByStaffId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches]  WITH CHECK ADD  CONSTRAINT [FK_IntakeBatches_WarehouseAreas_CurrentAreaId] FOREIGN KEY([CurrentAreaId])
REFERENCES [dbo].[WarehouseAreas] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches] CHECK CONSTRAINT [FK_IntakeBatches_WarehouseAreas_CurrentAreaId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches]  WITH CHECK ADD  CONSTRAINT [FK_IntakeBatches_Warehouses_WarehouseId] FOREIGN KEY([WarehouseId])
REFERENCES [dbo].[Warehouses] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[IntakeBatches] CHECK CONSTRAINT [FK_IntakeBatches_Warehouses_WarehouseId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Inventories]  WITH CHECK ADD  CONSTRAINT [FK_Inventories_AreaGroups_AreaGroupId] FOREIGN KEY([AreaGroupId])
REFERENCES [dbo].[AreaGroups] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Inventories] CHECK CONSTRAINT [FK_Inventories_AreaGroups_AreaGroupId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Inventories]  WITH CHECK ADD  CONSTRAINT [FK_Inventories_ClassifiedBatches_ClassifiedBatchId] FOREIGN KEY([ClassifiedBatchId])
REFERENCES [dbo].[ClassifiedBatches] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Inventories] CHECK CONSTRAINT [FK_Inventories_ClassifiedBatches_ClassifiedBatchId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Inventories]  WITH CHECK ADD  CONSTRAINT [FK_Inventories_StorageLocations_StorageLocationId] FOREIGN KEY([StorageLocationId])
REFERENCES [dbo].[StorageLocations] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Inventories] CHECK CONSTRAINT [FK_Inventories_StorageLocations_StorageLocationId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Inventories]  WITH CHECK ADD  CONSTRAINT [FK_Inventories_Warehouses_WarehouseId] FOREIGN KEY([WarehouseId])
REFERENCES [dbo].[Warehouses] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Inventories] CHECK CONSTRAINT [FK_Inventories_Warehouses_WarehouseId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[InventoryTransactions]  WITH CHECK ADD  CONSTRAINT [FK_InventoryTransactions_Users_PerformedByStaffId] FOREIGN KEY([PerformedByStaffId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[InventoryTransactions] CHECK CONSTRAINT [FK_InventoryTransactions_Users_PerformedByStaffId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[InventoryTransactions]  WITH CHECK ADD  CONSTRAINT [FK_InventoryTransactions_Warehouses_WarehouseId] FOREIGN KEY([WarehouseId])
REFERENCES [dbo].[Warehouses] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[InventoryTransactions] CHECK CONSTRAINT [FK_InventoryTransactions_Warehouses_WarehouseId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Notifications]  WITH CHECK ADD  CONSTRAINT [FK_Notifications_DonationRequests_DonationRequestId] FOREIGN KEY([DonationRequestId])
REFERENCES [dbo].[DonationRequests] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Notifications] CHECK CONSTRAINT [FK_Notifications_DonationRequests_DonationRequestId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Notifications]  WITH CHECK ADD  CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Notifications] CHECK CONSTRAINT [FK_Notifications_Users_UserId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[OperationalTeams]  WITH CHECK ADD  CONSTRAINT [FK_OperationalTeams_Shifts_ShiftId] FOREIGN KEY([ShiftId])
REFERENCES [dbo].[Shifts] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[OperationalTeams] CHECK CONSTRAINT [FK_OperationalTeams_Shifts_ShiftId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[PickupAssignments]  WITH CHECK ADD  CONSTRAINT [FK_PickupAssignments_DonationRequests_DonorRequestId] FOREIGN KEY([DonorRequestId])
REFERENCES [dbo].[DonationRequests] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[PickupAssignments] CHECK CONSTRAINT [FK_PickupAssignments_DonationRequests_DonorRequestId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[PickupAssignments]  WITH CHECK ADD  CONSTRAINT [FK_PickupAssignments_IntakeBatches_IntakeBatchId] FOREIGN KEY([IntakeBatchId])
REFERENCES [dbo].[IntakeBatches] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[PickupAssignments] CHECK CONSTRAINT [FK_PickupAssignments_IntakeBatches_IntakeBatchId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[PickupAssignments]  WITH CHECK ADD  CONSTRAINT [FK_PickupAssignments_OperationalTeams_TeamId] FOREIGN KEY([TeamId])
REFERENCES [dbo].[OperationalTeams] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[PickupAssignments] CHECK CONSTRAINT [FK_PickupAssignments_OperationalTeams_TeamId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[PickupAssignments]  WITH CHECK ADD  CONSTRAINT [FK_PickupAssignments_Shifts_ShiftId] FOREIGN KEY([ShiftId])
REFERENCES [dbo].[Shifts] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[PickupAssignments] CHECK CONSTRAINT [FK_PickupAssignments_Shifts_ShiftId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ProcessingOperationInputs]  WITH CHECK ADD  CONSTRAINT [FK_ProcessingOperationInputs_ClassifiedBatches_ClassifiedBatchId] FOREIGN KEY([ClassifiedBatchId])
REFERENCES [dbo].[ClassifiedBatches] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ProcessingOperationInputs] CHECK CONSTRAINT [FK_ProcessingOperationInputs_ClassifiedBatches_ClassifiedBatchId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ProcessingOperationInputs]  WITH CHECK ADD  CONSTRAINT [FK_ProcessingOperationInputs_Inventories_InventoryId] FOREIGN KEY([InventoryId])
REFERENCES [dbo].[Inventories] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ProcessingOperationInputs] CHECK CONSTRAINT [FK_ProcessingOperationInputs_Inventories_InventoryId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ProcessingOperationInputs]  WITH CHECK ADD  CONSTRAINT [FK_ProcessingOperationInputs_ProcessingOperations_ProcessingOperationId] FOREIGN KEY([ProcessingOperationId])
REFERENCES [dbo].[ProcessingOperations] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ProcessingOperationInputs] CHECK CONSTRAINT [FK_ProcessingOperationInputs_ProcessingOperations_ProcessingOperationId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ProcessingOperationOutputs]  WITH CHECK ADD  CONSTRAINT [FK_ProcessingOperationOutputs_ProcessingOperations_ProcessingOperationId] FOREIGN KEY([ProcessingOperationId])
REFERENCES [dbo].[ProcessingOperations] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ProcessingOperationOutputs] CHECK CONSTRAINT [FK_ProcessingOperationOutputs_ProcessingOperations_ProcessingOperationId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ProcessingOperationOutputs]  WITH CHECK ADD  CONSTRAINT [FK_ProcessingOperationOutputs_Users_RecordedByStaffId] FOREIGN KEY([RecordedByStaffId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ProcessingOperationOutputs] CHECK CONSTRAINT [FK_ProcessingOperationOutputs_Users_RecordedByStaffId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ProcessingOperations]  WITH CHECK ADD  CONSTRAINT [FK_ProcessingOperations_Users_ApprovedByManagerId] FOREIGN KEY([ApprovedByManagerId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ProcessingOperations] CHECK CONSTRAINT [FK_ProcessingOperations_Users_ApprovedByManagerId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ProcessingOperations]  WITH CHECK ADD  CONSTRAINT [FK_ProcessingOperations_Users_ApprovedByOrganizationId] FOREIGN KEY([ApprovedByOrganizationId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ProcessingOperations] CHECK CONSTRAINT [FK_ProcessingOperations_Users_ApprovedByOrganizationId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ProcessingOperations]  WITH CHECK ADD  CONSTRAINT [FK_ProcessingOperations_Users_CreatedByUserId] FOREIGN KEY([CreatedByUserId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ProcessingOperations] CHECK CONSTRAINT [FK_ProcessingOperations_Users_CreatedByUserId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ProcessingOperations]  WITH CHECK ADD  CONSTRAINT [FK_ProcessingOperations_Users_IssuedByStaffId] FOREIGN KEY([IssuedByStaffId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ProcessingOperations] CHECK CONSTRAINT [FK_ProcessingOperations_Users_IssuedByStaffId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ProcessingOperations]  WITH CHECK ADD  CONSTRAINT [FK_ProcessingOperations_Users_OrganizationId] FOREIGN KEY([OrganizationId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ProcessingOperations] CHECK CONSTRAINT [FK_ProcessingOperations_Users_OrganizationId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ProcessingOperations]  WITH CHECK ADD  CONSTRAINT [FK_ProcessingOperations_Users_RejectedByManagerId] FOREIGN KEY([RejectedByManagerId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ProcessingOperations] CHECK CONSTRAINT [FK_ProcessingOperations_Users_RejectedByManagerId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ProcessingOperations]  WITH CHECK ADD  CONSTRAINT [FK_ProcessingOperations_Warehouses_WarehouseId] FOREIGN KEY([WarehouseId])
REFERENCES [dbo].[Warehouses] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ProcessingOperations] CHECK CONSTRAINT [FK_ProcessingOperations_Warehouses_WarehouseId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ProcessingShipmentEvents]  WITH CHECK ADD  CONSTRAINT [FK_ProcessingShipmentEvents_ProcessingOperations_ProcessingOperationId] FOREIGN KEY([ProcessingOperationId])
REFERENCES [dbo].[ProcessingOperations] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ProcessingShipmentEvents] CHECK CONSTRAINT [FK_ProcessingShipmentEvents_ProcessingOperations_ProcessingOperationId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Shifts]  WITH CHECK ADD  CONSTRAINT [FK_Shifts_Warehouses_WarehouseId] FOREIGN KEY([WarehouseId])
REFERENCES [dbo].[Warehouses] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Shifts] CHECK CONSTRAINT [FK_Shifts_Warehouses_WarehouseId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ShipmentStatusHistories]  WITH CHECK ADD  CONSTRAINT [FK_ShipmentStatusHistories_DistributionRequests_DistributionRequestId] FOREIGN KEY([DistributionRequestId])
REFERENCES [dbo].[DistributionRequests] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[ShipmentStatusHistories] CHECK CONSTRAINT [FK_ShipmentStatusHistories_DistributionRequests_DistributionRequestId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[StorageLocations]  WITH CHECK ADD  CONSTRAINT [FK_StorageLocations_AreaGroups_AreaGroupId] FOREIGN KEY([AreaGroupId])
REFERENCES [dbo].[AreaGroups] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[StorageLocations] CHECK CONSTRAINT [FK_StorageLocations_AreaGroups_AreaGroupId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[StorageLocations]  WITH CHECK ADD  CONSTRAINT [FK_StorageLocations_WarehouseAreas_AreaId] FOREIGN KEY([AreaId])
REFERENCES [dbo].[WarehouseAreas] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[StorageLocations] CHECK CONSTRAINT [FK_StorageLocations_WarehouseAreas_AreaId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[StorageLocations]  WITH CHECK ADD  CONSTRAINT [FK_StorageLocations_Warehouses_WarehouseId] FOREIGN KEY([WarehouseId])
REFERENCES [dbo].[Warehouses] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[StorageLocations] CHECK CONSTRAINT [FK_StorageLocations_Warehouses_WarehouseId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TeamMembers]  WITH CHECK ADD  CONSTRAINT [FK_TeamMembers_OperationalTeams_TeamId] FOREIGN KEY([TeamId])
REFERENCES [dbo].[OperationalTeams] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TeamMembers] CHECK CONSTRAINT [FK_TeamMembers_OperationalTeams_TeamId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TeamMembers]  WITH CHECK ADD  CONSTRAINT [FK_TeamMembers_Users_StaffId] FOREIGN KEY([StaffId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TeamMembers] CHECK CONSTRAINT [FK_TeamMembers_Users_StaffId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransactionItems]  WITH CHECK ADD  CONSTRAINT [FK_TransactionItems_ClassifiedBatches_ClassifiedBatchId] FOREIGN KEY([ClassifiedBatchId])
REFERENCES [dbo].[ClassifiedBatches] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransactionItems] CHECK CONSTRAINT [FK_TransactionItems_ClassifiedBatches_ClassifiedBatchId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransactionItems]  WITH CHECK ADD  CONSTRAINT [FK_TransactionItems_Inventories_InventoryId] FOREIGN KEY([InventoryId])
REFERENCES [dbo].[Inventories] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransactionItems] CHECK CONSTRAINT [FK_TransactionItems_Inventories_InventoryId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransactionItems]  WITH CHECK ADD  CONSTRAINT [FK_TransactionItems_InventoryTransactions_TransactionId] FOREIGN KEY([TransactionId])
REFERENCES [dbo].[InventoryTransactions] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransactionItems] CHECK CONSTRAINT [FK_TransactionItems_InventoryTransactions_TransactionId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransactionItems]  WITH CHECK ADD  CONSTRAINT [FK_TransactionItems_StorageLocations_DestinationLocationId] FOREIGN KEY([DestinationLocationId])
REFERENCES [dbo].[StorageLocations] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransactionItems] CHECK CONSTRAINT [FK_TransactionItems_StorageLocations_DestinationLocationId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransactionItems]  WITH CHECK ADD  CONSTRAINT [FK_TransactionItems_StorageLocations_SourceLocationId] FOREIGN KEY([SourceLocationId])
REFERENCES [dbo].[StorageLocations] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransactionItems] CHECK CONSTRAINT [FK_TransactionItems_StorageLocations_SourceLocationId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransferItems]  WITH CHECK ADD  CONSTRAINT [FK_TransferItems_ClassifiedBatches_ClassifiedBatchId] FOREIGN KEY([ClassifiedBatchId])
REFERENCES [dbo].[ClassifiedBatches] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransferItems] CHECK CONSTRAINT [FK_TransferItems_ClassifiedBatches_ClassifiedBatchId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransferItems]  WITH CHECK ADD  CONSTRAINT [FK_TransferItems_IntakeBatches_BatchId] FOREIGN KEY([BatchId])
REFERENCES [dbo].[IntakeBatches] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransferItems] CHECK CONSTRAINT [FK_TransferItems_IntakeBatches_BatchId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransferItems]  WITH CHECK ADD  CONSTRAINT [FK_TransferItems_TransferRequests_TransferId] FOREIGN KEY([TransferId])
REFERENCES [dbo].[TransferRequests] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransferItems] CHECK CONSTRAINT [FK_TransferItems_TransferRequests_TransferId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransferItems]  WITH CHECK ADD  CONSTRAINT [FK_TransferItems_Users_ApproveStaffId] FOREIGN KEY([ApproveStaffId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransferItems] CHECK CONSTRAINT [FK_TransferItems_Users_ApproveStaffId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransferItems]  WITH CHECK ADD  CONSTRAINT [FK_TransferItems_Users_RequestStaffId] FOREIGN KEY([RequestStaffId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransferItems] CHECK CONSTRAINT [FK_TransferItems_Users_RequestStaffId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransferItems]  WITH CHECK ADD  CONSTRAINT [FK_TransferItems_WarehouseAreas_ToAreaId] FOREIGN KEY([ToAreaId])
REFERENCES [dbo].[WarehouseAreas] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransferItems] CHECK CONSTRAINT [FK_TransferItems_WarehouseAreas_ToAreaId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransferRequests]  WITH CHECK ADD  CONSTRAINT [FK_TransferRequests_WarehouseAreas_FromAreaId] FOREIGN KEY([FromAreaId])
REFERENCES [dbo].[WarehouseAreas] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransferRequests] CHECK CONSTRAINT [FK_TransferRequests_WarehouseAreas_FromAreaId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransferRequests]  WITH CHECK ADD  CONSTRAINT [FK_TransferRequests_WarehouseAreas_ToAreaId] FOREIGN KEY([ToAreaId])
REFERENCES [dbo].[WarehouseAreas] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransferRequests] CHECK CONSTRAINT [FK_TransferRequests_WarehouseAreas_ToAreaId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransferRequests]  WITH CHECK ADD  CONSTRAINT [FK_TransferRequests_Warehouses_WarehouseId] FOREIGN KEY([WarehouseId])
REFERENCES [dbo].[Warehouses] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[TransferRequests] CHECK CONSTRAINT [FK_TransferRequests_Warehouses_WarehouseId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Users]  WITH CHECK ADD  CONSTRAINT [FK_Users_Roles_RoleId] FOREIGN KEY([RoleId])
REFERENCES [dbo].[Roles] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Users] CHECK CONSTRAINT [FK_Users_Roles_RoleId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Users]  WITH CHECK ADD  CONSTRAINT [FK_Users_Warehouses_WarehouseId] FOREIGN KEY([WarehouseId])
REFERENCES [dbo].[Warehouses] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[Users] CHECK CONSTRAINT [FK_Users_Warehouses_WarehouseId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[UserVerificationCodes]  WITH CHECK ADD  CONSTRAINT [FK_UserVerificationCodes_Users_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[UserVerificationCodes] CHECK CONSTRAINT [FK_UserVerificationCodes_Users_UserId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[VoucherCodes]  WITH CHECK ADD  CONSTRAINT [FK_VoucherCodes_Users_RedeemedByUserId] FOREIGN KEY([RedeemedByUserId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[VoucherCodes] CHECK CONSTRAINT [FK_VoucherCodes_Users_RedeemedByUserId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[VoucherCodes]  WITH CHECK ADD  CONSTRAINT [FK_VoucherCodes_Vouchers_VoucherId] FOREIGN KEY([VoucherId])
REFERENCES [dbo].[Vouchers] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[VoucherCodes] CHECK CONSTRAINT [FK_VoucherCodes_Vouchers_VoucherId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[VoucherRedemptions]  WITH CHECK ADD  CONSTRAINT [FK_VoucherRedemptions_Users_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[VoucherRedemptions] CHECK CONSTRAINT [FK_VoucherRedemptions_Users_UserId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[VoucherRedemptions]  WITH CHECK ADD  CONSTRAINT [FK_VoucherRedemptions_VoucherCodes_VoucherCodeId] FOREIGN KEY([VoucherCodeId])
REFERENCES [dbo].[VoucherCodes] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[VoucherRedemptions] CHECK CONSTRAINT [FK_VoucherRedemptions_VoucherCodes_VoucherCodeId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[VoucherRedemptions]  WITH CHECK ADD  CONSTRAINT [FK_VoucherRedemptions_Vouchers_VoucherId] FOREIGN KEY([VoucherId])
REFERENCES [dbo].[Vouchers] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[VoucherRedemptions] CHECK CONSTRAINT [FK_VoucherRedemptions_Vouchers_VoucherId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[WarehouseAreas]  WITH CHECK ADD  CONSTRAINT [FK_WarehouseAreas_Warehouses_WarehouseId] FOREIGN KEY([WarehouseId])
REFERENCES [dbo].[Warehouses] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[WarehouseAreas] CHECK CONSTRAINT [FK_WarehouseAreas_Warehouses_WarehouseId]'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[WorkScheduleTemplates]  WITH CHECK ADD  CONSTRAINT [FK_WorkScheduleTemplates_Warehouses_WarehouseId] FOREIGN KEY([WarehouseId])
REFERENCES [dbo].[Warehouses] ([Id])'';
EXEC sys.sp_executesql N''ALTER TABLE [dbo].[WorkScheduleTemplates] CHECK CONSTRAINT [FK_WorkScheduleTemplates_Warehouses_WarehouseId]'';
EXEC sys.sp_addextendedproperty @name=N''ReThreads.Schema.Version'',@value=N''20260924.2'';
PRINT N''Schema installed. Run 02_ReThreads_Seed_Data.sql next.'';

    COMMIT;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    THROW;
END CATCH;
';
EXEC sys.sp_executesql @Sql;

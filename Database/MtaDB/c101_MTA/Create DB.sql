CREATE DATABASE [C101_MTA]
GO
USE [C101_MTA]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[c101_mta_transactionStatus](
	[mta_transactionStatus_id] [int] NOT NULL,
	[mta_transactionStatus_name] [nvarchar](50) NOT NULL
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[c101_mta_transaction](
	[mta_msg_id] [uniqueidentifier] NOT NULL,
	[mta_transaction_timestamp] [datetime] NOT NULL,
	[mta_transactionStatus_id] [int] NOT NULL,
	[mta_transaction_serverResponse] [nvarchar](max) NOT NULL
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[c101_mta_queue](
	[mta_msg_id] [uniqueidentifier] NOT NULL,
	[mta_queue_queuedTimestamp] [datetime] NOT NULL,
	[mta_queue_attemptSendAfter] [datetime] NOT NULL,
	[mta_queue_isPickupLocked] [bit] NOT NULL
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[c101_mta_msg](
	[mta_msg_id] [uniqueidentifier] NOT NULL,
	[mta_msg_rcptTo] [nvarchar](max) NOT NULL,
	[mta_msg_mailFrom] [nvarchar](max) NULL,
	[mta_msg_dataPath] [varchar](max) NULL,
	[mta_msg_outboundIP] [varchar](50) NOT NULL
) ON [PRIMARY]
GO

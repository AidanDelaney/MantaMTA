CREATE TABLE [dbo].[c101_mta_msg](
	[mta_msg_id] [uniqueidentifier] NOT NULL,
	[mta_msg_rcptTo] [nvarchar](max) COLLATE Latin1_General_CI_AS NOT NULL,
	[mta_msg_mailFrom] [nvarchar](max) COLLATE Latin1_General_CI_AS NOT NULL,
	[mta_msg_dataPath] [varchar](max) COLLATE Latin1_General_CI_AS NULL,
	[mta_msg_outboundIP] [varchar](50) COLLATE Latin1_General_CI_AS NOT NULL
) ON [PRIMARY]

GO


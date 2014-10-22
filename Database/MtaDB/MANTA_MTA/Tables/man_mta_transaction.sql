CREATE TABLE [dbo].[man_mta_transaction](
	[mta_msg_id] [uniqueidentifier] NOT NULL,
	[ip_ipAddress_id] [int] NULL,
	[mta_transaction_timestamp] [datetime] NOT NULL,
	[mta_transactionStatus_id] [int] NOT NULL,
	[mta_transaction_serverResponse] [nvarchar](max) COLLATE Latin1_General_CI_AS NOT NULL,
	[mta_transaction_serverHostname] [nvarchar](max) COLLATE Latin1_General_CI_AS NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

CREATE CLUSTERED INDEX [IX_man_mta_transaction] ON [dbo].[man_mta_transaction] 
(
	[mta_msg_id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [Status] ON [dbo].[man_mta_transaction] 
(
	[mta_msg_id] ASC,
	[mta_transactionStatus_id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [Timestamp] ON [dbo].[man_mta_transaction] 
(
	[mta_transaction_timestamp] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO


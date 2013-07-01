CREATE TABLE [dbo].[c101_mta_queue](
	[mta_msg_id] [uniqueidentifier] NOT NULL,
	[mta_queue_queuedTimestamp] [datetime] NOT NULL,
	[mta_queue_attemptSendAfter] [datetime] NOT NULL,
	[mta_queue_isPickupLocked] [bit] NOT NULL,
	[mta_queue_dataPath] [nvarchar](max) COLLATE Latin1_General_CI_AS NOT NULL,
	[ip_group_id] [int] NOT NULL
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[c101_mta_queue] ADD  CONSTRAINT [PK_c101_mta_queue] PRIMARY KEY CLUSTERED 
(
	[mta_msg_id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO


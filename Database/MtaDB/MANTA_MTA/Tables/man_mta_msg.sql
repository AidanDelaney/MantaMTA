CREATE TABLE [dbo].[man_mta_msg](
	[mta_msg_id] [uniqueidentifier] NOT NULL,
	[mta_send_internalId] [int] NOT NULL,
	[mta_msg_rcptTo] [nvarchar](max) COLLATE Latin1_General_CI_AS NOT NULL,
	[mta_msg_mailFrom] [nvarchar](max) COLLATE Latin1_General_CI_AS NOT NULL
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[man_mta_msg] ADD  CONSTRAINT [PK_man_mta_msg] PRIMARY KEY CLUSTERED 
(
	[mta_msg_id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO


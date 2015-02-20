CREATE TABLE [dbo].[man_mta_send](
	[mta_send_internalId] [int] NOT NULL,
	[mta_send_id] [nvarchar](20) COLLATE Latin1_General_CI_AS NOT NULL,
	[mta_sendStatus_id] [int] NOT NULL,
	[mta_send_createdTimestamp] [datetime] NOT NULL,
	[mta_send_messages] [int] NOT NULL,
	[mta_send_accepted] [int] NOT NULL,
	[mta_send_rejected] [int] NOT NULL
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[man_mta_send] ADD  CONSTRAINT [DF_man_mta_send_mta_send_messages]  DEFAULT ((0)) FOR [mta_send_messages]
GO

ALTER TABLE [dbo].[man_mta_send] ADD  CONSTRAINT [DF_man_mta_send_mta_send_accepted]  DEFAULT ((0)) FOR [mta_send_accepted]
GO

ALTER TABLE [dbo].[man_mta_send] ADD  CONSTRAINT [DF_man_mta_send_mta_send_rejected]  DEFAULT ((0)) FOR [mta_send_rejected]
GO

ALTER TABLE [dbo].[man_mta_send] ADD  CONSTRAINT [PK_man_mta_send] PRIMARY KEY CLUSTERED 
(
	[mta_send_internalId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO

CREATE UNIQUE NONCLUSTERED INDEX [SendID] ON [dbo].[man_mta_send] 
(
	[mta_send_id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO


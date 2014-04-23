CREATE TABLE [dbo].[man_mta_sendMeta](
	[mta_send_internalId] [int] NOT NULL,
	[mta_sendMeta_name] [nvarchar](max) COLLATE Latin1_General_CI_AS NOT NULL,
	[mta_sendMeta_value] [nvarchar](max) COLLATE Latin1_General_CI_AS NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

CREATE NONCLUSTERED INDEX [IX_man_mta_sendMeta] ON [dbo].[man_mta_sendMeta] 
(
	[mta_send_internalId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO


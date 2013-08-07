CREATE TABLE [dbo].[man_evn_event](
	[evn_event_id] [int] IDENTITY(1,1) NOT NULL,
	[evn_type_id] [int] NOT NULL,
	[evn_event_timestamp] [datetime] NOT NULL,
	[evn_event_emailAddress] [nvarchar](320) COLLATE Latin1_General_CI_AS NOT NULL,
	[snd_send_id] [nvarchar](20) COLLATE Latin1_General_CI_AS NOT NULL,
	[evn_event_forwarded] [bit] NOT NULL
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[man_evn_event] ADD  CONSTRAINT [DF_man_evn_event_evn_event_forwarded]  DEFAULT ((0)) FOR [evn_event_forwarded]
GO

ALTER TABLE [dbo].[man_evn_event] ADD  CONSTRAINT [PK_man_evn_bounce] PRIMARY KEY CLUSTERED 
(
	[evn_event_id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO


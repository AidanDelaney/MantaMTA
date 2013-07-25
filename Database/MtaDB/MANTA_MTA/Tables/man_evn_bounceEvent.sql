CREATE TABLE [dbo].[man_evn_bounceEvent](
	[evn_event_id] [int] NOT NULL,
	[evn_bounceCode_id] [int] NOT NULL,
	[evn_bounceType_id] [int] NOT NULL,
	[evn_bounceEvent_message] [nvarchar](max) COLLATE Latin1_General_CI_AS NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

ALTER TABLE [dbo].[man_evn_bounceEvent] ADD  CONSTRAINT [PK_man_evn_bounceEvent] PRIMARY KEY CLUSTERED 
(
	[evn_event_id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO


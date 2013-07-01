CREATE TABLE [dbo].[man_ip_group](
	[ip_group_id] [int] NOT NULL,
	[ip_group_name] [nvarchar](50) COLLATE Latin1_General_CI_AS NOT NULL,
	[ip_group_description] [nvarchar](250) COLLATE Latin1_General_CI_AS NULL
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[man_ip_group] ADD  CONSTRAINT [PK_man_ip_group] PRIMARY KEY CLUSTERED 
(
	[ip_group_id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO


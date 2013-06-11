CREATE TABLE [dbo].[c101_ip_groupMembership](
	[ip_group_id] [int] NOT NULL,
	[ip_ipAddress_id] [int] NOT NULL
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[c101_ip_groupMembership] ADD  CONSTRAINT [PK_c101_ip_groupMembership] PRIMARY KEY CLUSTERED 
(
	[ip_group_id] ASC,
	[ip_ipAddress_id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO


CREATE TABLE [dbo].[man_ip_ipAddress](
	[ip_ipAddress_id] [int] NOT NULL,
	[ip_ipAddress_ipAddress] [varchar](45) COLLATE Latin1_General_CI_AS NOT NULL,
	[ip_ipAddress_hostname] [varchar](255) COLLATE Latin1_General_CI_AS NULL,
	[ip_ipAddress_isInbound] [bit] NULL,
	[ip_ipAddress_isOutbound] [bit] NULL,
 CONSTRAINT [UK_c101_ip_ipAddress_ipAddress] UNIQUE NONCLUSTERED 
(
	[ip_ipAddress_ipAddress] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[man_ip_ipAddress] ADD  CONSTRAINT [PK_man_ip_ipAddress_id] PRIMARY KEY CLUSTERED 
(
	[ip_ipAddress_id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO

ALTER TABLE [dbo].[man_ip_ipAddress] ADD  CONSTRAINT [UK_c101_ip_ipAddress_ipAddress] UNIQUE NONCLUSTERED 
(
	[ip_ipAddress_ipAddress] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO


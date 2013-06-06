CREATE TABLE [dbo].[c101_cfg_relayingPermittedIp](
	[cfg_relayingPermittedIp_ip] [varchar](45) COLLATE Latin1_General_CI_AS NOT NULL,
	[cfg_relayingPermittedIp_name] [nvarchar](50) COLLATE Latin1_General_CI_AS NULL,
	[cfg_relayingPermittedIp_description] [nvarchar](250) COLLATE Latin1_General_CI_AS NULL
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[c101_cfg_relayingPermittedIp] ADD  CONSTRAINT [PK_c101_cfg_relayingPermittedIps] PRIMARY KEY CLUSTERED 
(
	[cfg_relayingPermittedIp_ip] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO


CREATE TABLE [dbo].[man_cfg_localDomain](
	[cfg_localDomain_id] [int] IDENTITY(1,1) NOT NULL,
	[cfg_localDomain_domain] [nvarchar](255) COLLATE Latin1_General_CI_AS NOT NULL,
	[cfg_localDomain_name] [nvarchar](50) COLLATE Latin1_General_CI_AS NULL,
	[cfg_localDomain_description] [nvarchar](250) COLLATE Latin1_General_CI_AS NULL
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[man_cfg_localDomain] ADD  CONSTRAINT [PK_man_cfg_localDomain] PRIMARY KEY CLUSTERED 
(
	[cfg_localDomain_domain] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO


CREATE TABLE [dbo].[man_mta_fblAddress](
	[mta_fblAddress_address] [nvarchar](320) COLLATE Latin1_General_CI_AS NOT NULL,
	[mta_fblAddress_name] [nvarchar](50) COLLATE Latin1_General_CI_AS NOT NULL,
	[mta_fblAddress_description] [nvarchar](250) COLLATE Latin1_General_CI_AS NULL
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[man_mta_fblAddress] ADD  CONSTRAINT [PK_man_mta_fblAddress] PRIMARY KEY CLUSTERED 
(
	[mta_fblAddress_address] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO


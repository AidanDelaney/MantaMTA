CREATE TABLE [dbo].[c101_rle_mxPattern](
	[rle_mxPattern_id] [int] NOT NULL,
	[rle_mxPattern_name] [nvarchar](50) COLLATE Latin1_General_CI_AS NOT NULL,
	[rle_mxPattern_description] [nvarchar](250) COLLATE Latin1_General_CI_AS NULL,
	[rle_patternType_id] [int] NOT NULL,
	[rle_mxPattern_value] [nvarchar](250) COLLATE Latin1_General_CI_AS NOT NULL,
	[ip_ipAddress_id] [int] NULL
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[c101_rle_mxPattern] ADD  CONSTRAINT [PK_c101_rle_mxPattern] PRIMARY KEY CLUSTERED 
(
	[rle_mxPattern_id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO


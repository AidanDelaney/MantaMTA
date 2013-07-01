CREATE TABLE [dbo].[man_rle_rule](
	[rle_mxPattern_id] [int] NOT NULL,
	[rle_ruleType_id] [int] NOT NULL,
	[rle_rule_value] [nvarchar](250) COLLATE Latin1_General_CI_AS NOT NULL
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[man_rle_rule] ADD  CONSTRAINT [PK_sm_rle_rule] PRIMARY KEY CLUSTERED 
(
	[rle_mxPattern_id] ASC,
	[rle_ruleType_id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO


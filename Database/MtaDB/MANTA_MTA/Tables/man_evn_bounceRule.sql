CREATE TABLE [dbo].[man_evn_bounceRule](
	[evn_bounceRule_id] [int] NOT NULL,
	[evn_bounceRule_name] [nvarchar](50) COLLATE Latin1_General_CI_AS NOT NULL,
	[evn_bounceRule_description] [nvarchar](250) COLLATE Latin1_General_CI_AS NULL,
	[evn_bounceRule_executionOrder] [int] NOT NULL,
	[evn_bounceRule_isBuiltIn] [bit] NOT NULL,
	[evn_bounceRuleCriteriaType_id] [int] NOT NULL,
	[evn_bounceRule_criteria] [nvarchar](max) COLLATE Latin1_General_CI_AS NOT NULL,
	[evn_bounceRule_mantaBounceType] [int] NOT NULL,
	[evn_bounceRule_mantaBounceCode] [int] NOT NULL
) ON [PRIMARY]

GO


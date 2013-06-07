CREATE TABLE [dbo].[c101_cfg_para](
	[cfg_para_dropFolder] [nvarchar](255) COLLATE Latin1_General_CI_AS NOT NULL,
	[cfg_para_queueFolder] [nvarchar](255) COLLATE Latin1_General_CI_AS NOT NULL,
	[cfg_para_logFolder] [nvarchar](255) COLLATE Latin1_General_CI_AS NOT NULL,
	[cfg_para_listenPorts] [nvarchar](255) COLLATE Latin1_General_CI_AS NOT NULL,
	[cfg_para_retryIntervalMinutes] [int] NOT NULL,
	[cfg_para_maxTimeInQueueMinutes] [int] NOT NULL
) ON [PRIMARY]

GO


CREATE TABLE [dbo].[c101_mta_queue](
	[mta_msg_id] [uniqueidentifier] NOT NULL,
	[mta_queue_queuedTimestamp] [datetime] NOT NULL,
	[mta_queue_attemptSendAfter] [datetime] NOT NULL,
	[mta_queue_isPickupLocked] [bit] NOT NULL
) ON [PRIMARY]

GO


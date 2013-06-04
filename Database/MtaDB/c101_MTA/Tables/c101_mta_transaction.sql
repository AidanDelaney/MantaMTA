CREATE TABLE [dbo].[c101_mta_transaction](
	[mta_transaction_msgID] [uniqueidentifier] NOT NULL,
	[mta_transaction_timestamp] [datetime] NOT NULL,
	[mta_transactionStatus_id] [int] NOT NULL,
	[mta_transaction_serverResponse] [nvarchar](max) COLLATE Latin1_General_CI_AS NOT NULL
) ON [PRIMARY]

GO


USE [VIC_ES1]
GO

/****** Object:  Table [dbo].[Commits]    Script Date: 27/11/2014 4:35:07 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[Commits](
	[Counter] [int] IDENTITY(1,1) NOT NULL,
	[StreamId] [uniqueidentifier] NOT NULL,
	[EventVersion] [int] NOT NULL,
	[EventType] [varchar](max) NOT NULL,
	[CommitStamp] [datetime] NOT NULL,
	[EventData] [varbinary](max) NOT NULL,
	[CommitId] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_Commits] PRIMARY KEY CLUSTERED 
(
	[Counter] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
 CONSTRAINT [UC_Stream] UNIQUE NONCLUSTERED 
(
	[EventVersion] ASC,
	[StreamId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO



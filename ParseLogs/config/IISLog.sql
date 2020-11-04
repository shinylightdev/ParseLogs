CREATE TABLE [dbo].[IISLog](
	[logfile] [NVARCHAR](255) NULL,
	[datestamp] [DATETIME] NULL,
	[cs_method] [NVARCHAR](255) NULL,
	[cs_uri_stem] [NVARCHAR](4000) NULL,
	[cs_uri_query] [NVARCHAR](4000) NULL,
	[cs_username] [NVARCHAR](255) NULL,
	[c_ip] [NVARCHAR](255) NULL,
	[cs_User_Agent] [NVARCHAR](4000) NULL,
	[cs_referer] [NVARCHAR](4000) NULL,
	[sc_status] [NVARCHAR](255) NULL,
	[sc_win32_status] [NVARCHAR](255) NULL,
	[time_taken_ms] [INT] NULL
) ON [PRIMARY]
GO
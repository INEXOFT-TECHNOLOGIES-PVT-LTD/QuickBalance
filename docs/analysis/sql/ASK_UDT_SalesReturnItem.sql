CREATE TYPE [dbo].[ASK_UDT_SalesReturnItem] AS TABLE(
	[SLNO] [int] NULL,
	[PRCODE] [int] NULL,
	[QTY] [decimal](18, 6) NULL,
	[RATE] [decimal](18, 6) NULL,
	[PRRATE] [decimal](18, 6) NULL,
	[MRP] [decimal](18, 6) NULL,
	[LDISCPERC] [decimal](18, 6) NULL,
	[LDISCPRICE] [decimal](18, 6) NULL,
	[DISCPERC] [decimal](18, 6) NULL,
	[DISCPRICE] [decimal](18, 6) NULL,
	[BATCH] [nvarchar](max) NULL,
	[MFGDATE] [datetime] NULL,
	[EXPDATE] [datetime] NULL,
	[UNITCODE] [int] NULL,
	[GDWCODE] [int] NULL,
	[ALCODE] [nvarchar](max) NULL,
	[ALNAME] [nvarchar](max) NULL,
	[CONVTYPE] [int] NULL,
	[CONVFACT] [decimal](18, 6) NULL,
	[TAXABLE] [decimal](18, 6) NULL,
	[TAXCODE] [int] NULL,
	[TAXPERC] [decimal](18, 6) NULL,
	[TAXAMOUNT] [decimal](18, 6) NULL,
	[NETAMOUNT] [decimal](18, 6) NULL
)



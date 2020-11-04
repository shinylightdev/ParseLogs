# IIS Log Parser

Combines all the IIS log files inside a directory and combines it into one, removing log comments. Then it dumps it in a database table. It uses [fast-member](https://github.com/mgravell/fast-member) and SQLBulkCopy to import gigs of data in a short amount of time.

## Usage

First, create a mapping function that maps column headers from the log to the IISEntry class properties. The array index is the column order number:

```csharp
Func<string, IISEntry> mapper = line => new IISEntry()
{
  logfile         = Utility.SplitString(line)[0]),
  datestamp       = Convert.ToDateTime(Utility.SplitString(line)[1] + " " + Utility.SplitString(line)[2]),
  cs_method       = Utility.SplitString(line)[4],
  cs_uri_stem     = Utility.SplitString(line)[5],
  cs_uri_query    = Utility.SplitString(line)[6] == "-" ? null : Utility.SplitString(line)[6],
  cs_username     = null,
  c_ip            = Utility.SplitString(line)[9] == "-" ? null : Utility.SplitString(line)[9],
  cs_User_Agent   = Utility.SplitString(line)[10],
  cs_referer      = Utility.SplitString(line)[11] == "-" ? null : Utility.SplitString(line)[11],
  sc_status       = Utility.SplitString(line)[12],
  sc_win32_status = null,
  time_taken_ms   = Convert.ToInt32(Utility.SplitString(line)[15])
};

```

Then make a list of entries from the files in a directory.
```csharp

List<IISEntry> entries = IIS.GetIISEntries(logFile, mapper, maxEntries);
```

Lastly, save it to a database table.
```csharp
IIS.SaveIISLogFileToDatabase(entries, connectionString, databaseTable, maxEntries);
```


# Setup

First create the table. SQL file inside config/
```sql
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
```

Lastly, update the App.config to target your environment.


## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

Please make sure to update tests as appropriate.

## License
[MIT](https://choosealicense.com/licenses/mit/)

using ParseLogs.Lib;
using ParseLogs.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace ParseLogs
{
  class Program
  {
    static void Main(string[] args)
    {
      string connectionString = ConfigurationManager.AppSettings["ConnectionString"];
      string logDirectory     = ConfigurationManager.AppSettings["LogFilesDirectory"];
      string databaseTable    = ConfigurationManager.AppSettings["DatabaseTable"];
      int maxEntries          = Convert.ToInt32(ConfigurationManager.AppSettings["MaxEntriesToSaveToDatabase"]);

      string logFile          = logDirectory + @"\" + Guid.NewGuid().ToString().Split('-')[0] + ".log";
      string headers          = IIS.GetHeadersFromLogFile(logDirectory);

      Console.Write("Merging all IIS Logs from directory...\n");
      IIS.MergeIISLogsFromDirectory(headers, logDirectory, logFile);

      Console.Write(Messages.IIS_WAITING);

      try
      {
        // Let's make a mapping function to match class property names. 
        // If property is not assigned, then the column is ignored from 
        // the IIS file.
        Func<string, IISEntry> mapper = line => new IISEntry()
        {
          logfile         = Utility.SplitString(line)[0].Replace("PROD-Server_", "").Replace(".log", ""),
          datestamp       = Convert.ToDateTime(Utility.SplitString(line)[1] + " " + Utility.SplitString(line)[2]),
          cs_method       = Utility.SplitString(line)[4],
          cs_uri_stem     = Utility.SplitString(line)[5],
          cs_uri_query    = Utility.SplitString(line)[6] == "-" ? null : Utility.SplitString(line)[6],
          cs_username     = null,
          c_ip            = Utility.SplitString(line)[9] == "-" ? null : Utility.SplitString(line)[9],
          cs_User_Agent   = Utility.SplitString(line)[10],
          cs_referer      = Utility.SplitString(line)[11] == "-" ? null : Utility.SplitString(line)[11].Replace("https://[WEBSITE]", ""),
          sc_status       = Utility.SplitString(line)[12],
          sc_win32_status = null,
          time_taken_ms   = Convert.ToInt32(Utility.SplitString(line)[15])

          //sc_bytes       = Utility.SplitString(line)[9],
          //cs_bytes       = Utility.SplitString(line)[10],
          //s_computername = Utility.SplitString(line)[6].Substring(Math.Max(0, Utility.SplitString(line)[6].Length - 1)),
          //time_local     = null,
          //s_contentpath  = null,
        };

        List<IISEntry> entries = IIS.GetIISEntries(logFile, mapper, maxEntries);

        IIS.SaveIISLogFileToDatabase(entries, connectionString, databaseTable, maxEntries);

        Console.WriteLine("Complete!");
      }
      catch (Exception e)
      {
        Console.WriteLine("Stopped! Error: \n\n" + e.Message);
      }
      finally
      {
        // Let's delete the temp file if one was created.  
        File.Delete(logFile);
      }      
      
      Console.WriteLine("done");
      Console.ReadLine();
    }
  }
}
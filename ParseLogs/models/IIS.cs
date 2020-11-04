using FastMember;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using ParseLogs.Lib;

namespace ParseLogs.Models
{
  public static class IIS
  {
    /// <summary>
    /// Dynamically bulk save.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="iisEntries"></param>
    /// <param name="connectionString"></param>
    /// <param name="databaseTable"></param>
    /// <param name="maxEntries"></param>
    public static void SaveIISLogFileToDatabase<T>(IEnumerable<T> iisEntries, string connectionString, string databaseTable, int maxEntries = 100000)
    {
      using (var bcp = new SqlBulkCopy(connectionString))
      {
        using (var reader = ObjectReader.Create(iisEntries, "logfile", "datestamp", "cs_uri_stem", "cs_uri_query", "s_contentpath", "sc_status", "s_computername", "cs_referer", "sc_win32_status", "sc_bytes", "cs_bytes", "c_ip", "cs_method", "time_taken_ms", "time_local", "cs_User_Agent", "cs_username"))
        {
          bcp.BatchSize = 5000;
          bcp.BulkCopyTimeout = 30;
          bcp.NotifyAfter = 1000;

          bcp.SqlRowsCopied += (sender, e) =>
          {
            // Console.WriteLine("Wrote " + e.RowsCopied.ToString() + " records."); 
            var percentProgress = Math.Round((e.RowsCopied / (maxEntries * 1.0)) * 100, 0);

            Console.CursorLeft = Messages.IIS_WAITING.Length + 1;
            Console.CursorTop = 1;
            Console.WriteLine(percentProgress.ToString() + "% complete.");
          };


          bcp.DestinationTableName = databaseTable;

          // An unhandled exception of type 'System.Data.SqlClient.SqlException' occurred in System.Data.dll
          // Additional information: A transport-level error has occurred when receiving results from the server. 
          // (provider: TCP Provider, error: 0 - An existing connection was forcibly closed by the remote host.)

          bcp.WriteToServer(reader);

        }
      }
    }

    /// <summary>
    /// Get List of all IIS entries frSpinnerProgressom log file. 
    /// </summary>
    /// <param name="headers"></param>
    /// <param name="logFile"></param>
    /// <param name="maxEntries"></param>
    /// <returns></returns>
    public static List<IISEntry> GetIISEntries(string headers, string logFile, int maxEntries = 1000000)
    {
      List<IISEntry> list = new List<IISEntry>();

      // Open the file stream
      using (FileStream fs = File.Open(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
      {
        using (BufferedStream bs = new BufferedStream(fs))
        {
          using (StreamReader sr = new StreamReader(bs))
          {
            // Represents the entire line in the file.
            string line;

            // Skip the headers. 
            sr.ReadLine();

            // If there's a problem mapping columns from the IIS Log to an IISEntry object, let's save to a log file.
            //List<string> errorLines = new List<string>();

            int lineCount = 0;

            // Read the Line into the string variable, ommitting headers if they were inserted already 
            while ((line = sr.ReadLine()) != null && lineCount <= maxEntries)
            {
              if (line.IndexOf("#", 0) != 0)
              {
                try
                {
                  IISEntry entry = new IISEntry()
                  {
                    cs_uri_stem = Utility.SplitString(line)[2],
                    sc_status = Utility.SplitString(line)[5],
                    sc_bytes = Utility.SplitString(line)[9],
                    cs_bytes = Utility.SplitString(line)[10],
                    cs_method = Utility.SplitString(line)[12],
                    time_taken_ms = Convert.ToInt32(Utility.SplitString(line)[13]),
                    cs_User_Agent = Utility.SplitString(line)[15],

                    // Modified
                    datestamp = Convert.ToDateTime(Utility.SplitString(line)[1] + " " + Utility.SplitString(line)[14]),
                    s_computername = Utility.SplitString(line)[6].Substring(Math.Max(0, Utility.SplitString(line)[6].Length - 1)),
                    cs_referer = Utility.SplitString(line)[7] == "-" ? null : Utility.SplitString(line)[7].Replace("https://[WEBSITE]", ""),
                    cs_uri_query = Utility.SplitString(line)[3] == "-" ? null : Utility.SplitString(line)[3],
                    logfile = Utility.SplitString(line)[0].Replace("PROD-Server_", "").Replace(".log", ""),
                    c_ip = Utility.SplitString(line)[11] == "-" ? null : Utility.SplitString(line)[11],

                    // Set as empty
                    time_local = null,
                    sc_win32_status = null,
                    s_contentpath = null,
                    cs_username = null
                  };
                  list.Add(entry);
                }
                catch
                {
                }

                lineCount++;
              }
            }
          }
        }
      }
      return list;
    }

    /// <summary>
    /// Merges all IIS Logs in a directory into 1 IIS Log, after scrubbing them. 
    /// </summary>
    /// <param name="headers"></param>
    /// <param name="logDirectory"></param>
    /// <param name="saveToFilePath"></param>
    public static void MergeIISLogsFromDirectory(string headers, string logDirectory, string saveToFilePath)
    {
      var iisLogFiles = Directory.GetFiles(logDirectory, "*.log", SearchOption.AllDirectories);
      var iisLogFilesForToday = new List<string>();

      // Let's just get the files that were generated today. 
      foreach (var item in iisLogFiles)
      {
        // Go far back 0 days; meaning today's files. If 1, then goes back 1 day, 2, then two days, and so on. 
        int dateOffset = 0;
        if (File.GetLastWriteTime(item) >= DateTime.Now.AddDays(-dateOffset).Date)
        {
          iisLogFilesForToday.Add(item);
        }
      }

      // Loop through the files in directory.
      using (StreamWriter sw = new StreamWriter(saveToFilePath))
      {
        sw.WriteLine(headers);
        foreach (string logFilePath in iisLogFilesForToday)
        {
          ScrubIISLog(sw, logFilePath);
        }
      }
    }


    /// <summary>
    /// Cleans up IIS logs by removing all headers except the first one. 
    /// </summary>    
    /// <param name="sw"></param>
    /// <param name="path"></param>
    private static void ScrubIISLog(StreamWriter sw, string path)
    {
      // Open the file stream
      using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
      {
        using (BufferedStream bs = new BufferedStream(fs))
        {
          using (StreamReader sr = new StreamReader(bs))
          {
            // Represents the entire line in the file.
            string line;

            // Read the Line into the string variable, ommitting headers if they were inserted already 
            while ((line = sr.ReadLine()) != null)
            {
              if (line.IndexOf("#", 0) != 0)
              {
                sw.WriteLine(path.Split('\\').Last() + " " + line);
              }
            }
          }
        }
      }
    }


  }
}

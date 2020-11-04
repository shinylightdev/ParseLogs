using FastMember;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

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
      string[] propertyNames = (new IISEntry()).GetType().GetProperties().Select(property => property.Name).ToArray();      

      using (var bcp = new SqlBulkCopy(connectionString))
      {        
        using (var reader = ObjectReader.Create(iisEntries, propertyNames))
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
    /// Get List of all IIS entries.
    /// </summary>
    /// <param name="logFile"></param>
    /// <param name="mapper"></param>
    /// <param name="maxEntries"></param>
    /// <returns></returns>
    public static List<IISEntry> GetIISEntries(string logFile, Func<string, IISEntry> mapper, int maxEntries = 1000000)
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
            // List<string> errorLines = new List<string>();

            int lineCount = 0;

            // Read the Line into the string variable, ommitting headers if they were inserted already 
            while ((line = sr.ReadLine()) != null && lineCount <= maxEntries)
            {
              // Ignore IIS log comments. 
              if (line.IndexOf("#", 0) != 0)
              {
                try
                {
                  IISEntry entry = mapper(line);
                  list.Add(entry);
                }
                catch
                {
                  // TODO: Do something in case error. 
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
    /// Merges all IIS Logs in a directory into 1 IIS Log file, after scrubbing them. Removes all # comments and keeps headers. 
    /// Prepends the column "logfile" to signify the file associated with the log entries. 
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
    /// Gets a string containing log file header names, comma separated.
    /// </summary>
    /// <returns></returns>
    public static string GetHeadersFromLogFile(string logDirectory)
    {
      string headers = "";
      string firstFile = Directory.GetFiles(logDirectory).First();

      using (FileStream fs = File.Open(firstFile, FileMode.Open, FileAccess.Read))
      {
        using (BufferedStream bs = new BufferedStream(fs))
        {
          using (StreamReader sr = new StreamReader(bs))
          {
            // Represents the entire line in the file.
            string line;

            while ((line = sr.ReadLine()) != null)
            {
              string fieldsHeader = "#Fields";
              if (line.IndexOf(fieldsHeader, 0) == 0)
              {
                headers = line.Replace(fieldsHeader + ": ", "");
                headers = "logfile " + headers;
                break;
              }
            }
          }
        }
      }
      return headers;
    }


    /// <summary>
    /// Cleans up IIS logs by removing all headers except the first one. 
    /// </summary>    
    /// <param name="sw"></param>
    /// <param name="path"></param>
    public static void ScrubIISLog(StreamWriter sw, string path)
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
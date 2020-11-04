using FastMember;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading;

namespace ParseLogs
{

  /*
 
   * 
  CREATE TABLE IISLog
    (
      datestamp DATETIME ,
      cs_uri_stem VARCHAR(5000) , 
      cs_uri_query VARCHAR(5000) ,
      s_contentpath VARCHAR(2000) ,
      sc_status VARCHAR(255) ,
      s_computername VARCHAR(50) ,
      cs_referer VARCHAR(5000) ,
      sc_win32_status VARCHAR(255) ,
      sc_bytes VARCHAR(50) ,
      cs_bytes VARCHAR(50) ,
      c_ip VARCHAR(255) ,
      cs_method VARCHAR(255) ,
      time_taken_ms INT ,
      time_local DATETIME ,
      cs_User_Agent VARCHAR(5000) ,
      cs_username VARCHAR(255)
    )
 
   * 
   * 
   */


  public static class MESSAGES
  {
    public const string IIS_WAITING = "Saving IIS log entries to database...";
  }


  public class IISEntry
  {
    public string logfile { get; set; }
    public DateTime datestamp { get; set; }
    public string cs_uri_stem { get; set; }
    public string cs_uri_query { get; set; }
    public string s_contentpath { get; set; }
    public string sc_status { get; set; }
    public string s_computername { get; set; }
    public string cs_referer { get; set; }
    public string sc_win32_status { get; set; }
    public string sc_bytes { get; set; }
    public string cs_bytes { get; set; }
    public string c_ip { get; set; }
    public string cs_method { get; set; }
    public int time_taken_ms { get; set; }
    public Nullable<DateTime> time_local { get; set; }
    public string cs_User_Agent { get; set; }
    public string cs_username { get; set; }
  }


  class Program
  {

    /// <summary>
    /// MAIN!!
    /// </summary>
    /// <param name="args"></param>
    static void Main(string[] args)
    {
      string connectionString = ConfigurationManager.AppSettings["ConnectionString"];
      string headers = "logfile date cs-uri-stem cs-uri-query s-contentpath sc-status s-computername cs(Referer) sc-win32-status sc-bytes cs-bytes c-ip cs-method TimeTakenMS time-local cs(User-Agent) cs-username";
      string logDirectory = ConfigurationManager.AppSettings["LogFilesDirectory"];
      string logFile = logDirectory + @"\" + Guid.NewGuid().ToString().Split('-')[0] + ".log";
      
      string databaseTable = ConfigurationManager.AppSettings["DatabaseTable"];
      int maxEntries = Convert.ToInt32(ConfigurationManager.AppSettings["MaxEntriesToSaveToDatabase"]);

      Console.Write("Merging all IIS Logs from directory...\n");
      MergeIISLogsFromDirectory(headers, logDirectory, logFile);

      Console.Write(MESSAGES.IIS_WAITING);
      try
      {
        List<IISEntry> entries = GetIISEntries(headers, logFile, maxEntries);
        SaveIISLogFileToDatabase(entries, connectionString, databaseTable, maxEntries);
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
      // Console.ReadLine();
    }


    /// <summary>
    /// Dynamically Bulk Save
    /// </summary>
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

            Console.CursorLeft = MESSAGES.IIS_WAITING.Length + 1;
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
    private static List<IISEntry> GetIISEntries(string headers, string logFile, int maxEntries = 1000000)
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
                    cs_uri_stem = SplitString(line)[2],
                    sc_status = SplitString(line)[5],
                    sc_bytes = SplitString(line)[9],
                    cs_bytes = SplitString(line)[10],
                    cs_method = SplitString(line)[12],
                    time_taken_ms = Convert.ToInt32(SplitString(line)[13]),
                    cs_User_Agent = SplitString(line)[15],

                    // Modified
                    datestamp = Convert.ToDateTime(SplitString(line)[1] + " " + SplitString(line)[14]),
                    s_computername = SplitString(line)[6].Substring(Math.Max(0, SplitString(line)[6].Length - 1)),
                    cs_referer = SplitString(line)[7] == "-" ? null : SplitString(line)[7].Replace("https://[WEBSITE]", ""),
                    cs_uri_query = SplitString(line)[3] == "-" ? null : SplitString(line)[3],
                    logfile = SplitString(line)[0].Replace("WEB-PROD3-Server_", "").Replace(".log", ""),
                    c_ip = SplitString(line)[11] == "-" ? null : SplitString(line)[11],

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
          ScrubIISLog(headers, sw, logFilePath);
        }
      }
    }


    /// <summary>
    /// Split the string 
    /// </summary>
    /// <param name="stringToSplit"></param>
    /// <param name="delimiter"></param>
    /// <param name="qualifier"></param>
    /// <returns></returns>
    public static List<string> SplitString(string stringToSplit, char delimiter = ' ', char qualifier = '"')
    {
      var parts = stringToSplit.Split(qualifier)
                   .Select((element, index) => index % 2 == 0
                                           ? element.Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries)
                                           : new string[] { element })
                   .SelectMany(element => element).ToList();
      return parts;
    }



    public static void SpinnerProgress(int top = 0, int left = 0, int frameDelay = 500, string charFrames = @"|/-\")
    {
      foreach (char frame in charFrames)
      {
        Console.CursorTop = top;
        Console.CursorLeft = left;
        Thread.Sleep(frameDelay);
        Console.Write(frame);
      }
    }


    /// <summary>
    /// Cleans up IIS logs by removing all headers except the first one. 
    /// </summary>
    /// <param name="headers"></param>
    /// <param name="sw"></param>
    /// <param name="path"></param>
    private static void ScrubIISLog(string headers, StreamWriter sw, string path)
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
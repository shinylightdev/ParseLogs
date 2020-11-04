using ParseLogs.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

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



  
  class Program
  {

    /// <summary>
    /// MAIN!!
    /// </summary>
    /// <param name="args"></param>
    static void Main(string[] args)
    {
      string connectionString = ConfigurationManager.AppSettings["ConnectionString"];
      string logDirectory = ConfigurationManager.AppSettings["LogFilesDirectory"];
      string databaseTable = ConfigurationManager.AppSettings["DatabaseTable"];
      int maxEntries = Convert.ToInt32(ConfigurationManager.AppSettings["MaxEntriesToSaveToDatabase"]);

      string logFile = logDirectory + @"\" + Guid.NewGuid().ToString().Split('-')[0] + ".log";
      string headers = IIS.GetHeadersFromLogFile(logDirectory);

      Console.Write("Merging all IIS Logs from directory...\n");
      IIS.MergeIISLogsFromDirectory(headers, logDirectory, logFile);

      Console.Write(Messages.IIS_WAITING);

      try
      {
        //List<IISEntry> entries = IIS.GetIISEntries(logFile, maxEntries);


        //IIS.SaveIISLogFileToDatabase(entries, connectionString, databaseTable, maxEntries);

        Console.WriteLine("Complete!");
      }
      catch (Exception e)
      {
        Console.WriteLine("Stopped! Error: \n\n" + e.Message);
      }
      finally
      {
        // Let's delete the temp file if one was created.  
        //File.Delete(logFile);
      }
      
      Console.WriteLine("done");
      Console.ReadLine();

    }

    

  }
}
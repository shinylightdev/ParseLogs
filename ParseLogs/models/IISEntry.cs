using System;

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
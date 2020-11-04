using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ParseLogs.Lib
{
  public static class Utility
  {
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

    /// <summary>
    /// Rotating characters to show activity. 
    /// </summary>
    /// <param name="top"></param>
    /// <param name="left"></param>
    /// <param name="frameDelay"></param>
    /// <param name="charFrames"></param>
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
  }
}

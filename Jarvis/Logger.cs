using System;
using System.IO;

namespace Jarvis
{
  public static class Logger
  {
    private static StreamWriter writer;

    public const int FATAL = 0;
    public const int ERROR = 1;
    public const int WARN = 2;
    public const int INFO = 3; 
    public const int DEBUG = 4; 
    public const int TRACE = 5;
    public const int TEST = 6;
    public const int ALL = 7;

    private static bool[] enabledLevels = new bool[8];


    static Logger ()
    {
      string logFile = Jarvis.Config.AppSettings.Settings ["workingDir"].Value + "/jarvis.log";
          
      writer = File.AppendText (logFile);
      writer.AutoFlush = true;
    }
     
    public static void EnableLevel(int level)
    {
      enabledLevels [level] = true;
    }

    public static void DisableLevel(int level)
    {
      enabledLevels [level] = false;
    }

    public static void Fatal(string text, params object[] items)
    {
      if (enabledLevels [FATAL] || enabledLevels [ALL]) 
      {
        text = DateTime.Now.ToString ("MM/dd/yyyy hh:mm:ss tt") + " [FATAL]: " + text;
        writer.WriteLine (text, items);
      }
    }

    public static void Error(string text, params object[] items)
    {
      if (enabledLevels [ERROR] || enabledLevels [ALL]) 
      {
        text = DateTime.Now.ToString ("MM/dd/yyyy hh:mm:ss tt") + " [ERROR]: " + text;
        writer.WriteLine (text, items);
      }
    }

    public static void Warn(string text, params object[] items)
    {
      if (enabledLevels [WARN] || enabledLevels [ALL]) 
      {
        text = DateTime.Now.ToString ("MM/dd/yyyy hh:mm:ss tt") + "  [WARN]: " + text;
        writer.WriteLine (text, items);
      }
    }

    public static void Info(string text, params object[] items)
    {
      if (enabledLevels [INFO] || enabledLevels [ALL]) 
      {
         text = DateTime.Now.ToString ("MM/dd/yyyy hh:mm:ss tt") + "  [INFO]: " + text;
         writer.WriteLine(text, items);
      }
    }

    public static void Debug(string text, params object[] items)
    {
      if (enabledLevels [DEBUG] || enabledLevels [ALL]) 
      {
        text = DateTime.Now.ToString ("MM/dd/yyyy hh:mm:ss tt") + " [DEBUG]: " + text;
        writer.WriteLine (text, items);
      }
    }

    public static void Trace(string text, params object[] items)
    {
      if (enabledLevels [TRACE] || enabledLevels [ALL]) 
      {
        text = DateTime.Now.ToString ("MM/dd/yyyy hh:mm:ss tt") + " [TRACE]: " + text;
        writer.WriteLine (text, items);
      }
    }

    public static void Test(string text, params object[] items)
    {
      if (enabledLevels [TEST] || enabledLevels [ALL]) 
      {
        text = DateTime.Now.ToString ("MM/dd/yyyy hh:mm:ss tt") + "  [TEST]: " + text;
        writer.WriteLine (text, items);
      }
    }
  }
}


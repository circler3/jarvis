using System.IO;
using System.Configuration;
using System;
using Nancy.Hosting.Self;
using System.Diagnostics;
using System.Timers;
using System.Threading;
using System.Text.RegularExpressions;
using System.Text;
using System.Xml;
using System.Collections.Generic;

namespace Jarvis
{
  public class Jarvis
  {
    public static Configuration Config = null;
    private static AutoResetEvent autoEvent = new AutoResetEvent(false);
    private static System.Timers.Timer processReaper = new System.Timers.Timer(10000);

    public static void Main()
    {      
      Init();
      string port = Jarvis.Config.AppSettings.Settings["port"].Value;
      Logger.Info("~Jarvis started on port {0}~", port);

      // Start Nancy
      var uri = new Uri("http://localhost:" + port);
      var config = new HostConfiguration();
      config.UrlReservations.CreateAutomatically = true;
      
      using (var host = new NancyHost(config, uri))
      {
        host.Start();
        autoEvent.WaitOne();
      }

      Cleanup();
    }

    private static void Init()
    {
      bool isLocalConfig = false;
      // Load config file
      ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();

      if (File.Exists("jarvis.local.config"))
      {       
        configMap.ExeConfigFilename = "jarvis.local.config";
        isLocalConfig = true;
      }
      else
      {
        configMap.ExeConfigFilename = "jarvis.config";        
        isLocalConfig = false;
      }

      Config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);

      // Turn on logger
      int logLevel = int.Parse(Jarvis.Config.AppSettings.Settings["logLevel"].Value);
      Logger.EnableLevel(logLevel);

      if (isLocalConfig)
      {
        Logger.Info("Loaded local config file");
      }

      // Register Unhandled exceptions
      AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(ExceptionHandler);

      // Catch Ctrl-C
      Console.CancelKeyPress += Console_CancelKeyPress;

      // Clean up student processes
      processReaper.AutoReset = true;
      processReaper.Elapsed += ProcessReaper_Elapsed;
      processReaper.Start();
        
      // Setup course directory if needed
      string baseDir = Config.AppSettings.Settings["workingDir"].Value;
 
      Directory.CreateDirectory(baseDir + "/courses");
      CreateCourseDirectory();
    }

    private static void Cleanup()
    {
      Logger.Info("~Jarvis stopped~");
      processReaper.Stop();
    }

    private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
    {      
      autoEvent.Set(); // Unblocks the main thread
    }

    private static void ProcessReaper_Elapsed(object sender, ElapsedEventArgs e)
    {
      Regex regex = new Regex(@"^A\d{8}");
      Process[] processes = Process.GetProcesses();

      foreach (Process p in processes)
      {
        if (regex.IsMatch(p.ProcessName))
        {
          // Nexted if statements instead of compound conditional due to access priveleges
          TimeSpan runtime = DateTime.Now - p.StartTime;
          if (runtime.TotalMinutes > 1)
          {
            Logger.Warn("Killing process with name {0}", p.ProcessName);
            //p.Kill();
          }
        }
      }
    }

    // Catches all unhandled exceptions and logs them to a file
    public static void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
    {
      Exception e = (Exception)args.ExceptionObject;
           
      StringBuilder sb = new StringBuilder();
      sb.AppendLine("");
      sb.AppendLine("-------------------------------");
      sb.AppendLine(e.TargetSite.ToString());
      sb.AppendLine(e.Message);
      sb.AppendLine(e.StackTrace);
      sb.AppendLine("-------------------------------");
      sb.AppendLine("");

      Logger.Fatal(sb.ToString());
    }

    private static void CreateCourseDirectory()
    {
      string coursesFile = Config.AppSettings.Settings["workingDir"].Value + "/courses.xml";
      string baseDir = Config.AppSettings.Settings["workingDir"].Value + "/courses/";
      string courseName = "";
      int assignmentCount = 0;

      if (!File.Exists(coursesFile))
      {
        Logger.Fatal("Can't find courses file at {0}", coursesFile);
      }
      else
      {
        using (XmlReader reader = XmlReader.Create(File.OpenRead(coursesFile)))
        {
          while (reader.Read())
          {
            switch (reader.NodeType)
            {
              case XmlNodeType.Element:
                if (reader.Name.ToLower() == "course")
                {                
                  courseName = reader.GetAttribute("name");
                  assignmentCount = int.Parse(reader.GetAttribute("assignmentCount"));

                  Directory.CreateDirectory(baseDir + courseName);
                  for (int i = 1; i <= assignmentCount; ++i)
                  {
                    Directory.CreateDirectory(baseDir + courseName + "/hw" + i.ToString());
                  }
                }
                else if (reader.Name.ToLower() == "section")
                {              
                  int id = int.Parse(reader.GetAttribute("id"));
                  string leader = reader.GetAttribute("leader");

                  for (int i = 1; i <= assignmentCount; ++i)
                  {
                    Directory.CreateDirectory(baseDir + courseName + "/hw" + i.ToString() + "/section" + id);
                    File.WriteAllText(baseDir + courseName + "/hw" + i.ToString() + "/section" + id + "/leader.txt", leader);
                  }
                }
                break;
            }
          }
        }
      }
    }

    public static string ToHtmlEncoding(string text)
    {
      text = text.Replace(" ", "&nbsp;");
      text = text.Replace("\n", "<br />");

      return text;
    }

    public static string ToTextEncoding(string text)
    {
      text = text.Replace("&nbsp;", " ");
      text = text.Replace("<br />", "\n");

      return text;
    }

    public static void DirectoryCopy(string sourceDirName, string destDirName)
    {
      // Get the subdirectories for the specified directory.
      DirectoryInfo dir = new DirectoryInfo(sourceDirName);

      if (!dir.Exists)
      {
        throw new DirectoryNotFoundException(
          "Source directory does not exist or could not be found: "
          + sourceDirName);
      }

      DirectoryInfo[] dirs = dir.GetDirectories();
      // If the destination directory doesn't exist, create it.
      if (!Directory.Exists(destDirName))
      {
        Directory.CreateDirectory(destDirName);
      }

      // Get the files in the directory and copy them to the new location.
      FileInfo[] files = dir.GetFiles();
      foreach (FileInfo file in files)
      {
        string temppath = Path.Combine(destDirName, file.Name);
        file.CopyTo(temppath, false);
      }

      foreach (DirectoryInfo subdir in dirs)
      {
        string temppath = Path.Combine(destDirName, subdir.Name);
        DirectoryCopy(subdir.FullName, temppath);
      }
    }
  }
}

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
    public static StatsManager Stats { get; set; }
    public static Configuration Config = null;
    public  static List<int> StudentProcesses = new List<int>();
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
        
      Stats = new StatsManager();
      Stats.ReadStats();

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
      List<int> expiredPids = new List<int>();

      foreach (int pid in StudentProcesses)
      {
        try 
        {
          Process process = Process.GetProcessById(pid);

          if (process.HasExited)
          {
            expiredPids.Add(pid);
          }
          else
          {
            TimeSpan runtime = DateTime.Now - process.StartTime;
            if (runtime.TotalSeconds > 20)
            {
              Logger.Warn("Killing process with name {0}", process.ProcessName);
              process.Kill();
            }
          }
        }
        catch (ArgumentException)
        {
          // Porcess is not running
          expiredPids.Add(pid);
        }
      }

      foreach (int pid in expiredPids)
      {
        StudentProcesses.Remove(pid);
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

                  File.WriteAllText(baseDir + courseName + "/grader.txt", reader.GetAttribute("grader"));

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
  }
}

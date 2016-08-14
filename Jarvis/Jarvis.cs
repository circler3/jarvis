using System.IO;
using System.Configuration;
using System;
using Nancy.Hosting.Self;
using System.Diagnostics;
using System.Timers;
using System.Threading;
using System.Text.RegularExpressions;

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

      // Start Nancy
      var uri = new Uri("http://localhost:8080");
      var config = new HostConfiguration();
      config.UrlReservations.CreateAutomatically = true;
      
      using (var host = new NancyHost(config, uri))
      {
        host.Start();

        Console.WriteLine("Jarvis is running on " + uri);
        Console.WriteLine("Press any [Enter] to close Jarvis.");
        autoEvent.WaitOne();
      }

      Cleanup();
    }

    private static void Init()
    {
      // Register Unhandled exceptions
      AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(ExceptionHandler);

      // Catch Ctrl-C
      Console.CancelKeyPress += Console_CancelKeyPress;

      // Clean up student processes
      processReaper.AutoReset = true;
      processReaper.Elapsed += ProcessReaper_Elapsed;
      processReaper.Start();

      // Setup logger
      Trace.Listeners.Add(new TextWriterTraceListener("jarvis.log"));

      // Load config file
      ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
      configMap.ExeConfigFilename = "jarvis.config";
      Config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
      
      // Setup course directory if needed
      string baseDir = Config.AppSettings.Settings["workingDir"].Value;
      if (!Directory.Exists(baseDir + "/courses"))
      {
        Directory.CreateDirectory(baseDir + "/courses");
      }

    }

    private static void Cleanup()
    {
      Trace.Flush();
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
            Trace.TraceWarning("Killing process with name {0}", p.ProcessName);
            p.Kill();
          }
        }
      }
    }

    // Catches all unhandled exceptions and logs them to a file
    public static void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
    {
      Exception e = (Exception)args.ExceptionObject;
      StreamWriter writer = new StreamWriter("jarvisExceptionLog.txt", true);
      writer.WriteLine("-------------------------------");
      writer.WriteLine(DateTime.Now.ToString());
      writer.WriteLine(e.TargetSite);
      writer.WriteLine(e.Message);
      writer.WriteLine(e.StackTrace);
      writer.Flush();
      writer.Close();
    }
  }
}

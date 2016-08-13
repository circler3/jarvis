using System.IO;
using System.Configuration;

namespace Jarvis
{
  using System;
  using Nancy.Hosting.Self;
  using System.Diagnostics;
  using System.Timers;

  public class Jarvis
  {
    public static Configuration Config = null;

    public static void Main(string[] args)
    {
      Timer processReaper = new Timer(10000);
      processReaper.AutoReset = true;
      processReaper.Elapsed += ProcessReaper_Elapsed;
      processReaper.Start();

      Trace.Listeners.Add(new TextWriterTraceListener("jarvis.log"));
      
      // Load config file
      ExeConfigurationFileMap configMap = new ExeConfigurationFileMap ();
      configMap.ExeConfigFilename = "jarvis.config";
      Config = ConfigurationManager.OpenMappedExeConfiguration (configMap, ConfigurationUserLevel.None);

      // Register Unhandled exceptions
      AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler (ExceptionHandler);

      // Setup course directory if needed
      string baseDir = Config.AppSettings.Settings["workingDir"].Value;
      if (!Directory.Exists(baseDir + "/courses"))
      {
        Directory.CreateDirectory (baseDir + "/courses");
      }

      // Start Nancy
      var uri = new Uri("http://localhost:8080");
      var config = new HostConfiguration();
      config.UrlReservations.CreateAutomatically = true;

      using (var host = new NancyHost(config, uri))
      {
        host.Start();

        Console.WriteLine("Jarvis is running on " + uri);
        Console.WriteLine("Press any [Enter] to close Jarvis.");
        Console.ReadLine();
      }

      Trace.Flush();
      processReaper.Stop();
    }

    private static void ProcessReaper_Elapsed(object sender, ElapsedEventArgs e)
    {
      Process[] processes = Process.GetProcesses();

      foreach (Process p in processes)
      {
        TimeSpan runtime = DateTime.Now - p.StartTime;

        if (!p.ProcessName.Contains("Jarvis") && runtime.TotalMinutes > 1)
        {
          Trace.TraceWarning("Killing process with name {0}", p.ProcessName);
          p.Kill();
        }
      }
    }

    // Catches all unhandled exceptions and logs them to a file
    public static void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
    {
      Exception e = (Exception)args.ExceptionObject;
      StreamWriter writer = new StreamWriter ("jarvisExceptionLog.txt", true);
      writer.WriteLine ("-------------------------------");
      writer.WriteLine (DateTime.Now.ToString ());
      writer.WriteLine (e.TargetSite);
      writer.WriteLine (e.Message);
      writer.WriteLine (e.StackTrace);
      writer.Flush ();
      writer.Close ();
    }
  }
}

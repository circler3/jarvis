using System.Threading;
using System.IO;
using System.Configuration;

namespace Jarvis
{
  using System;
  using Nancy.Hosting.Self;

  public class Jarvis
  {
    public static Configuration Config = null;

    public static void Main(string[] args)
    {
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

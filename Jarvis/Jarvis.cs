using System.Threading;
using System.IO;

namespace Jarvis
{
  using System;
  using Nancy.Hosting.Self;

  public class Jarvis
  {
    public static void Main(string[] args)
    {
      AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler (ExceptionHandler);

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

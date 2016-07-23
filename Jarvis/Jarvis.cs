using System.Threading;

namespace Jarvis
{
  using System;
  using Nancy.Hosting.Self;

  class Jarvis
  {
    static void Main(string[] args)
    {
      var uri =
          new Uri("http://localhost:8080");

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
  }
}

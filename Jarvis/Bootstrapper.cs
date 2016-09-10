using Nancy.Bootstrapper;
using System.Text;

namespace Jarvis
{
  using Nancy;


  public class Bootstrapper : DefaultNancyBootstrapper
  {
    // The bootstrapper enables you to reconfigure the composition of the framework,
    // by overriding the various methods and properties.
    // For more information https://github.com/NancyFx/Nancy/wiki/Bootstrapper

    protected override void ApplicationStartup(Nancy.TinyIoc.TinyIoCContainer container, IPipelines pipelines)
    {
      StaticConfiguration.EnableRequestTracing = true;
      StaticConfiguration.DisableErrorTraces = false;

      pipelines.OnError += (ctx, e) => {

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("");
        sb.AppendLine("-------------------------------");
        sb.AppendLine(e.TargetSite.ToString());
        sb.AppendLine(e.Message);
        sb.AppendLine(e.StackTrace);
        sb.AppendLine("-------------------------------");
        sb.AppendLine("");

        Logger.Fatal(sb.ToString());

        return null;
      };
    }


  }
}

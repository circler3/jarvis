using Nancy.Bootstrapper;

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
      var enableTraces = Jarvis.Config.AppSettings.Settings ["enableTraces"];

      if (enableTraces != null && enableTraces.Value.Equals ("true")) 
      {
        StaticConfiguration.DisableErrorTraces = false;
      }
    }
  }
}
using System;
using Nancy;
using Nancy.ModelBinding;

namespace Jarvis
{
  public class HastingsModules : NancyModule
  {
    private FileUploadHandler uploadHandler = new FileUploadHandler();

    public HastingsModules()
    {
      Get["hastings"] = _ =>
      {
        Logger.Trace("Handling get for /hastings");
        return View["hastings", Hastings.GetPlayers()];
      };

      Post["/uploadHastingsPlayer"] = _ =>
      {
        Logger.Trace("Handling post for /uploadHastingsPlayer");
        string result = string.Empty;

        FileUploadRequest request = this.Bind<FileUploadRequest>();
        Assignment assignment = uploadHandler.HandleStudentUpload(request.Files);

        if (!assignment.ValidHeader)
        {
          result = string.Format("<p>{0}</p>", assignment.ErrorMessage);
        }
        else
        {
          result = Hastings.BuildHastings(assignment);
        }

        return result;
      };

      Post["/loadHastingsBattle"] = _ =>
      {
        Logger.Trace("Handling post for /loadHastingsBattle");
        string player1Name = this.Request.Form["player1"];
        string player2Name = this.Request.Form["player2"];

        Logger.Trace("Loading battle between {0} and {1}", player1Name, player2Name);

        Random rand = new Random();
        string result = string.Empty;

        // Randomly choose sides
        if (rand.Next() % 2 == 1)
        {
          result = Hastings.DoBattle(player1Name, player2Name);
        }
        else
        {
          result = Hastings.DoBattle(player2Name, player1Name);
        }

        return result;
      };
    }
  }
}


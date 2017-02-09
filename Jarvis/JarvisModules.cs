using Nancy.ModelBinding;
using System.Text;

namespace Jarvis
{
  using Nancy;
  using System;
  using System.Collections.Generic;


  public class JarvisModules : NancyModule
  {
    private FileUploadHandler uploadHandler = new FileUploadHandler();
    
    public JarvisModules()
    {
      #region Gets
      Get["/"] = _ =>
      {
        Logger.Trace("Handling get for /");
        return View["index"];
      };

      Get["/help"] = _ =>
      {
        Logger.Trace("Handling get for /help");
        return View["help"];
      };

      Get["/grade"] = _ =>
      {
        Logger.Trace("Handling get for /grade");
        return View["grade"];
      };

      Get["/stats"] = _ =>
      {
        Logger.Trace("Handling get for /stats");
        return View["stats", Jarvis.Stats];
      };
      #endregion

      #region Posts
      Post["/run"] = _ =>
      {
        Logger.Trace("Handling post for /run");
        Runner runner = new Runner();
        RunResult result = null;

        StringBuilder builder = new StringBuilder();

        FileUploadRequest request = this.Bind<FileUploadRequest>();
        Assignment assignment = uploadHandler.HandleStudentUpload(request.Files);
        
        Logger.Info("Received assignment from {0} for {1} HW#{2} with {3} header", assignment.StudentId, assignment.Course, assignment.HomeworkId, assignment.ValidHeader ? "true" : "false");
        
        if (assignment.ValidHeader)
        {
          // Run grader
          Logger.Debug("Assignment header was valid");
          result = runner.Run(assignment);
          builder.Append(result.ToHtml());

          StatsManager.UpdateStats(assignment, result);
        }
        else
        {
          builder.AppendLine("<p>");
          builder.AppendLine("The uploaded files did not contain a valid header, sir. I suggest you review the <a href='/help'>help</a>.");
          builder.AppendFormat("<br />Parser error message: {0}", assignment.ErrorMessage);
          builder.AppendLine("</p>");

          Jarvis.Stats.TotalBadHeaders++;
        }

        return builder.ToString();
      };

      Post["/grade"] = _ =>
      {
        Logger.Trace("Handling post for /grade");
        Guid temp = Guid.NewGuid();
        string baseDir = Jarvis.Config.AppSettings.Settings["workingDir"].Value;
        string gradingDir = baseDir + "/grading/" + temp.ToString() + "/";  
        bool runMoss = this.Request.Form["runMoss"];
        bool runCode = this.Request.Form["runCode"];

        var request = this.Bind<FileUploadRequest>();
        List<Assignment> assignments = uploadHandler.HandleGraderUpload(gradingDir, request.Files);

        Grader grader = new Grader(runMoss, runCode);

        return grader.Grade(gradingDir + "canvas.csv", baseDir, assignments);
      };
      #endregion
    }
  }
}
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
        Assignment assignment = uploadHandler.HandleStudentUpload(request.File);
        
        Logger.Info("Received assignment from {0} for {1} HW#{2} with {3} header", assignment.StudentId, assignment.Course, assignment.HomeworkId, assignment.ValidHeader ? "true" : "false");
        
        if (assignment.ValidHeader)
        {
          // Run grader
          Logger.Debug("Assignment header was valid");
          result = runner.Run(assignment);
          builder.Append(result.ToHtml());
        }
        else
        {
          builder.AppendLine("<p>");
          builder.AppendLine("The uploaded file contains an invalid header, sir. I suggest you review the <a href='/help'>help</a>.");
          builder.AppendFormat("<br />Parser error message: {0}", assignment.ErrorMessage);
          builder.AppendLine("</p>");

          Jarvis.Stats.TotalBadHeaders++;
        }

        UpdateStats(assignment, result);

        return builder.ToString();
      };

      Post["/grade"] = _ =>
      {
        Logger.Trace("Handling post for /grade");
        Guid temp = Guid.NewGuid();
        string baseDir = Jarvis.Config.AppSettings.Settings["workingDir"].Value;
        string gradingDir = baseDir + "/grading/" + temp.ToString() + "/";  

        var request = this.Bind<FileUploadRequest>();
        List<Assignment> assignments = uploadHandler.HandleGraderUpload(gradingDir, request.File);
     
        Grader grader = new Grader();

        return grader.Grade(baseDir, assignments);
      };
      #endregion
    }

    private void UpdateStats(Assignment homework, RunResult result)
    {
      AssignmentStats stats = null;
      string name = homework.Course + " - hw" + homework.HomeworkId;

      if (!Jarvis.Stats.AssignmentData.ContainsKey(name))
      {
        stats = new AssignmentStats();
        stats.Name = name;
        Jarvis.Stats.AssignmentData.Add(name, stats);
      }
      else
      {
        stats = Jarvis.Stats.AssignmentData[name];
      }
      
      stats.TotalSubmissions++;
      
      if (!stats.TotalUniqueStudentsSubmissions.ContainsKey(homework.StudentId))
      {
        stats.TotalUniqueStudentsSubmissions.Add(homework.StudentId, string.Empty);
      }

      if (result != null)
      {
        stats.TotalUniqueStudentsSubmissions[homework.StudentId] = result.Grade.ToString();
      
        if (!result.CompileMessage.Contains("Success!!"))
        {
          stats.TotalNonCompile++;
        }
      
        if (!result.StyleMessage.Contains("Total&nbsp;errors&nbsp;found:&nbsp;0"))
        {
          stats.TotalBadStyle++;
        }
      }
      Jarvis.Stats.WriteStats();
    }
  }
}
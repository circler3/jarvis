using Nancy.ModelBinding;

namespace Jarvis
{
  using Nancy;
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.IO.Compression;
  using System.Net.Mail;
  using System.Text;

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
      #endregion

      #region Posts
      Post["/practiceRun"] = _ =>
      {
        Logger.Trace("Handling post for /practiceRun");
        Grader grader = new Grader();
        GradingResult result = null;

        var request = this.Bind<FileUploadRequest>();
        var assignment = uploadHandler.HandleStudentUpload(request.File);
        
        Logger.Info("Received assignment from {0} for {1} HW#{2} with {3} header", assignment.StudentId, assignment.Course, assignment.HomeworkId, assignment.ValidHeader ? "true" : "false");
        
        if (assignment.ValidHeader)
        {
          // Run grader
          Logger.Debug("Assignment header was valid");
          result = grader.Grade(assignment);          
        }
                    
        if (assignment.ValidHeader)
        {
          return View["results", result];
        }
        else
        {
          return View["error"];
        }
      };

      Post["/runForRecord"] = _ =>
      {
        Logger.Trace("Handling post for /runForRecord");
        
        // extract to temp directory
        // parse headers
        Logger.Trace("Extracting grader zip file");

        Guid temp = Guid.NewGuid();
        string baseDir = Jarvis.Config.AppSettings.Settings["workingDir"].Value;
        string gradingDir = baseDir + "/grading/" + temp.ToString() + "/";                    

        var request = this.Bind<FileUploadRequest>();
        List<Assignment> assignments = uploadHandler.HandleGraderUpload(gradingDir, request.File);
          
        // copy to course directory structure
        string currentHomework = assignments[0].HomeworkId;
        string currentCourse = assignments[0].Course;
        string hwPath = string.Format("{0}/courses/{1}/hw{2}/", baseDir, currentCourse, currentHomework);


        Logger.Info("Grading {0} assignments for course: {1} - HW#: {2}", assignments.Count, currentCourse, currentHomework);

        foreach (Assignment a in assignments)
        {
          string oldPath = a.FullPath;
          a.Path = string.Format("{0}section{1}/{2}", hwPath, a.Section, a.StudentId);
          
          Directory.CreateDirectory(a.Path);
          if (File.Exists(a.FullPath))
          {
            File.Delete(a.FullPath);
          }

          File.Move(oldPath, a.FullPath);
        }

        // run grader
        foreach (Assignment a in assignments)
        {
          using (StreamWriter writer = File.AppendText(a.Path + "/../grades.txt"))
          {
            writer.AutoFlush = true;
            writer.WriteLine("-----------------------------------------------");
            
            if (a.ValidHeader)
            {
              // run grader on each file and save grading result
              Grader grader = new Grader();
              
              GradingResult result = grader.Grade(a);
              Logger.Info("Result: {0}", result.Grade);
              
              // write grade to section report              
              writer.WriteLine(string.Format("{0} : {1}", a.StudentId, result.Grade));
              writer.WriteLine(result.GradingComment);
            }
            else
            {
              writer.WriteLine("Invalid header from " + a.StudentId);
            }
            
            writer.Close();
          }
        }

        // zip contents
        // email to section leader
        string[] directories = Directory.GetDirectories(hwPath, "section*", SearchOption.AllDirectories);
        SmtpClient mailClient = new SmtpClient("localhost", 25);
        StringBuilder gradingReport = new StringBuilder();

        foreach (string section in directories)
        {
          char sectionNumber = section[section.Length - 1];                    
          string zipFile = string.Format("{0}/../section{1}.zip", section, sectionNumber);
          
          // zip contents
          if (File.Exists(zipFile))
          {
            File.Delete(zipFile);
          }

          ZipFile.CreateFromDirectory(section, zipFile);

          string leader = File.ReadAllText(section + "/leader.txt");          

          // attach to email to section leader
          MailMessage mail = new MailMessage("jarvis@jarvis.cs.usu.edu", leader);
          mail.Subject = "Grades for " + currentCourse + " " + currentHomework;
          mail.Body = "Hello! Attached are the grades for " + currentCourse + " " + currentHomework + ". Happy grading, sir.";
          mail.Attachments.Add(new Attachment(zipFile));

          mailClient.Send(mail);

          gradingReport.AppendLine(string.Format("Emailed section {0} grading materials to {1} <br />", sectionNumber, leader));
        }

        // Generate some kind of grading report

        return View["gradingReport", gradingReport.ToString()];
      };
      #endregion
      // Need to provide a way to close an assignment and get MOSS report
      // MOSS - To be written to a file
    }
  }
}
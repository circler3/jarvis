﻿using Nancy.ModelBinding;

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
          Logger.Debug("Assignment header was valid");
          // Run grader
          result = grader.Grade(assignment);
          result.ValidHeader = true;
        }
        else
        {
          Logger.Debug("Assignment header was not valid");
          result = new GradingResult();
          result.ValidHeader = false;
        }

        //var response = new FileUploadResponse() { Identifier = uploadResult.Identifier };
        if (result.ValidHeader)
        {
          return View["results", result];
        }
        else
        {
          return View["error"];
        }
      };

      Get["/grade"] = _ =>
      {
        Logger.Trace("Handling get for /grade");
        return View["grade"];
      };

      Post["/runForRecord"] = _ =>
      {
        Logger.Trace("Handling post for /runForRecord");
        SmtpClient mailClient = new SmtpClient("localhost", 25);
        string currentHomework = string.Empty;
        string currentCourse = string.Empty;
        StringBuilder gradingReport = new StringBuilder();

        Guid temp = Guid.NewGuid();
        string gradingDir = "/tmp/jarvis/grading/" + temp.ToString() + "/";

        // handle file upload
        var request = this.Bind<FileUploadRequest>();
        List<Assignment> assignments = uploadHandler.HandleGraderUpload(gradingDir, request.File);

        currentHomework = string.Format("HW {0}", assignments[0].HomeworkId);
        currentCourse = assignments[0].Course;

        // foreach assignment
        foreach (Assignment a in assignments)
        {
          string sectionReport = string.Format("{0}section{1}/grades.txt", gradingDir, a.Section);
          StreamWriter writer = new StreamWriter(File.OpenWrite(sectionReport));
          if (a.ValidHeader)
          {
            // run grader on each file and save grading result
            Grader grader = new Grader();

            GradingResult result = grader.Grade(a);
            // write grade to section report
            writer.WriteLine("-----------------------------------------------");
            writer.WriteLine(string.Format("{0} : {1}", a.StudentId, result.Grade));
            writer.WriteLine(result.GradingComment);
          }
          else
          {
            writer.WriteLine("Invalid header for file " + a.Path);
          }

          writer.Close();
        }

        // foreach section
        string[] directories = Directory.GetDirectories(gradingDir, "section", SearchOption.AllDirectories);

        foreach (string section in directories)
        {
          char sectionNumber = section[section.Length];
          string zipFile = string.Format("{0}/section{1}.zip", gradingDir, sectionNumber);
          // zip contents
          ZipFile.CreateFromDirectory(section, zipFile);

          string leader = Jarvis.Config.AppSettings.Settings[string.Format("sectionLeader{0}", sectionNumber)].Value;

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

      // Need to provide a way to close an assignment and get MOSS report
      // MOSS - To be written to a file
    }
  }
}
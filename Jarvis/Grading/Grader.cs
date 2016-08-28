using System;
using System.Diagnostics;
using System.Text;
using HtmlDiff;
using System.IO;
using System.Timers;
using System.Collections.Generic;
using System.Net.Mail;
using System.IO.Compression;

namespace Jarvis
{
  public class Grader
  {
    private bool forcedKill = false;
    private Process executionProcess;

    public Grader()
    {

    }
      
    public GradingResult Grade(Assignment homework)
    {
      GradingResult result = new GradingResult(homework);

      // Style check
      Logger.Info("Running style check on {0} {1}", homework.StudentId, homework.HomeworkId);
      result.StyleMessage = StyleCheck(homework);

      // Compile      
      Logger.Info("Compiling {0} {1}", homework.StudentId, homework.HomeworkId);      
      result.CompileMessage = Compile(homework);

      // Run tests
      if (result.CompileMessage == "Success!!")
      {
        Logger.Info("Running {0} {1}", homework.StudentId, homework.HomeworkId);        
        result.OutputMessage = GetExecutionOutput(homework);

        result.CorrectOutput = result.OutputMessage.Contains("No difference");
      }
      else
      {
        result.OutputMessage = "Didn't compile... :(";
      }

      // Write result into results file, writes a new entry for each run
      RecordResult(homework, result);

      return result;
    }

    private void RecordResult(Assignment homework, GradingResult result)
    {
      string timestamp = DateTime.Now.ToString();

      StreamWriter writer = new StreamWriter (homework.Path + "results.txt", true);
      writer.WriteLine (timestamp + " " + homework.StudentId + " " + result.Grade); 
      writer.Flush();
      writer.Close();      
    }

    private string StyleCheck (Assignment homework)
    {
      Process p = new Process ();
      
      p.StartInfo.UseShellExecute = false;
      p.StartInfo.RedirectStandardOutput = true;
      p.StartInfo.RedirectStandardError = true;

      string styleExe = Jarvis.Config.AppSettings.Settings["styleExe"].Value;
      p.StartInfo.FileName = styleExe;
      p.StartInfo.Arguments = Jarvis.Config.AppSettings.Settings["styleExemptions"].Value + " " + homework.FullPath;

      Logger.Trace("Style checking with {0} and arguments {1}", styleExe, p.StartInfo.Arguments);

      p.Start();

      string result = p.StandardError.ReadToEnd ();
      result = result.Replace (homework.Path, "");
      result = Jarvis.ToHtmlEncoding(result);
      p.WaitForExit();

      p.Close();
      p.Dispose();

      return result;
    }

    private string Compile(Assignment homework)
    {
      Process p = new Process();

      p.StartInfo.UseShellExecute = false;
      p.StartInfo.RedirectStandardOutput = true;
      p.StartInfo.RedirectStandardError = true;

      p.StartInfo.FileName = "g++";
      p.StartInfo.Arguments = "-Werror " + homework.FullPath + " -o" + homework.Path + homework.StudentId;
      p.Start();

      string result = p.StandardError.ReadToEnd();
      result = result.Replace(homework.Path, "");
      result = Jarvis.ToHtmlEncoding(result);

      p.WaitForExit();

      p.Close();
      p.Dispose();

      Logger.Trace("Compile result: {0}", result);

      return (!string.IsNullOrEmpty(result)) ? result : "Success!!";
    }

    private string GetExecutionOutput(Assignment homework)
    {
      // todo Loop and call Execute Program multiple times
      string[] inputFiles = Directory.GetFiles(homework.Path + "../../", "input*");
      string[] outputFiles = Directory.GetFiles(homework.Path + "../../", "output*");
      StringBuilder result = new StringBuilder();

      for (int i = 0; i < outputFiles.Length; ++i)
      {
        string input = "";
        if (inputFiles.Length > i)
        {
          input = inputFiles[i];
        }
        string actualOutput = ExecuteProgram(homework, input);
        string expectedOutput = GetExpectedOutput(outputFiles[i]);       

        result.AppendLine("<h3>Test case: " + i.ToString() + "</h3>");
        result.AppendLine("<h3>Actual</h3>");
        result.AppendLine("<p>" + Jarvis.ToHtmlEncoding(actualOutput) + "</p>");
        result.AppendLine("<br />");
        result.AppendLine("<h3>Expected</h3>");
        result.AppendLine("<p>" + Jarvis.ToHtmlEncoding(expectedOutput) + "</p>");
        result.AppendLine("<br />");
        result.AppendLine("<h3>Diff</h3>");

        string testDiff = string.Empty;

        if (actualOutput.Equals(expectedOutput, StringComparison.Ordinal))
        {
          testDiff = "No difference";
        }
        else
        {
          testDiff = HtmlDiff.HtmlDiff.Execute(actualOutput, expectedOutput);
        }

        result.AppendLine("<p>" + testDiff + "</p>");
        result.AppendLine("------------------------------------------------------------------");
      }

      // Don't leave binaries hanging around
      File.Delete(homework.Path + homework.StudentId);

      return result.ToString();
    }

    private string ExecuteProgram(Assignment homework, string inputFile)
    {      
      string output = string.Empty;
      executionProcess = new Process();

      executionProcess.StartInfo.UseShellExecute = false;
      executionProcess.StartInfo.RedirectStandardOutput = true;
      executionProcess.StartInfo.RedirectStandardError = true;
      executionProcess.StartInfo.RedirectStandardInput = true;

      if (!File.Exists(homework.Path + homework.StudentId))
      {
        Logger.Fatal("Executable " + homework.Path + homework.StudentId + " did not exist!!");
      }

      executionProcess.StartInfo.FileName = homework.Path + homework.StudentId;      
      executionProcess.Start();

      using (Timer executionTimer = new Timer(10000))
      {
        executionTimer.Elapsed += ExecutionTimer_Elapsed;
        executionTimer.Enabled = true;

        if (File.Exists(inputFile))
        {
          StreamReader reader = new StreamReader(inputFile);

          while (!reader.EndOfStream)
          {
            executionProcess.StandardInput.WriteLine(reader.ReadLine());
          }
        }

        output = executionProcess.StandardOutput.ReadToEnd();

        executionTimer.Enabled = false;

        if (forcedKill)
        {
          output = "Oh no! I found an infinite loop, sir.";
        }
      }

      executionProcess.Close();
      executionProcess.Dispose();

      return output;
    }

    private void ExecutionTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
      // It's been long enough... kill the process
      Logger.Error("Grader is killing {0} because it has been running too long", executionProcess.ProcessName);
      executionProcess.Kill();
      forcedKill = true;
    }

    private string GetExpectedOutput(string path)
    {
      StreamReader reader = new StreamReader(path);

      return reader.ReadToEnd();
    }

    public string GenerateGrades(string baseDir, List<Assignment> assignments)
    {
      List<GradingResult> gradingResults = new List<GradingResult>();
      // extract to temp directory
      // parse headers
      Logger.Trace("Extracting grader zip file");

      // copy to course directory structure
      string currentHomework = assignments[0].HomeworkId;
      string currentCourse = assignments[0].Course;
      string hwPath = string.Format("{0}/courses/{1}/hw{2}/", baseDir, currentCourse, currentHomework);

      string[] sections = Directory.GetDirectories(hwPath, "section*", SearchOption.AllDirectories);
      foreach (string section in sections)
      {
        if (File.Exists(section + "/grades.txt"))
        {
          File.Delete(section + "/grades.txt");
        }
      }

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
            gradingResults.Add(result);
            Logger.Info("Result: {0}", result.Grade);

            string gradingComment = Jarvis.ToTextEncoding(result.ToText());

            // write grade to section report              
            writer.WriteLine(string.Format("{0} : {1}", a.StudentId, result.Grade));
            writer.WriteLine(gradingComment);
          }
          else
          {
            writer.WriteLine("Invalid header from " + a.StudentId);
          }

          writer.Close();
        }
      }

      string gradingReport = SendFilesToSectionLeaders(hwPath, currentCourse, currentHomework);

      string graderEmail = File.ReadAllText(hwPath + "../grader.txt");

      Logger.Info("Sending Canvas CSV to {0}", graderEmail);

      CanvasFormatter canvasFormatter = new CanvasFormatter();

      string canvasCsvPath = hwPath + "canvas.csv";
      canvasFormatter.GenerateCanvasCsv(canvasCsvPath, currentHomework, gradingResults);

      SendEmail(graderEmail, 
                "Grades for " + currentCourse + " " + currentHomework, 
                "Hello! Attached are the grades for " + currentCourse + " " + currentHomework + ". Happy grading!", 
                canvasCsvPath);
           
      // Generate some kind of grading report
      return gradingReport;
    }

    private void SendEmail(string to, string subject, string body, string attachment)
    {
      SmtpClient mailClient = new SmtpClient("localhost", 25);

      MailMessage mail = new MailMessage("jarvis@jarvis.cs.usu.edu", to);
      mail.Subject = subject;
      mail.Body = body;
      mail.Attachments.Add(new Attachment(attachment));

      mailClient.Send(mail);
    }

    private string SendFilesToSectionLeaders(string hwPath, string currentCourse, string currentHomework)
    {
      // zip contents
      // email to section leader
      string[] directories = Directory.GetDirectories(hwPath, "section*", SearchOption.AllDirectories);
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
        SendEmail(leader, 
          "Grades for " + currentCourse + " " + currentHomework,
          "Hello! Attached are the grades for " + currentCourse + " " + currentHomework + ". Happy grading!",
          zipFile);        

        gradingReport.AppendLine(string.Format("Emailed section {0} grading materials to {1} <br />", sectionNumber, leader));
      }

      return gradingReport.ToString();
    }
  }
}


using System;
using System.Diagnostics;
using System.Text;
using HtmlDiff;
using System.IO;
using System.Timers;

namespace Jarvis
{
  public class Grader
  {
    private Process executionProcess;
    private Timer executionTimer = new Timer(10000);

    public Grader()
    {
      executionTimer.Elapsed += ExecutionTimer_Elapsed;
    }

    public GradingResult Grade(Assignment homework)
    {
      GradingResult result = new GradingResult();

      // Style check
      Trace.TraceInformation("Running style check on {0} {1}", homework.StudentId, homework.HomeworkId);
      result.StyleMessage = StyleCheck(homework);

      // Compile      
      Trace.TraceInformation("Compiling {0} {1}", homework.StudentId, homework.HomeworkId);      
      result.CompileMessage = Compile(homework);

      // Run tests
      if (result.CompileMessage == "Success!!")
      {
        Trace.TraceInformation("Running {0} {1}", homework.StudentId, homework.HomeworkId);        
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

      StreamWriter writer = new StreamWriter(homework.Path + "results.txt", true);
      writer.WriteLine(timestamp + " " + homework.StudentId + " " + result.Grade);
      writer.Flush();
      writer.Close();
    }

    private string StyleCheck(Assignment homework)
    {
      Process p = new Process();

      p.StartInfo.UseShellExecute = false;
      p.StartInfo.RedirectStandardOutput = true;
      p.StartInfo.RedirectStandardError = true;

      string styleExe = Jarvis.Config.AppSettings.Settings["styleExe"].Value;
      p.StartInfo.FileName = styleExe;
      p.StartInfo.Arguments = homework.FullName;
      p.Start();

      string result = p.StandardError.ReadToEnd();
      result = result.Replace(homework.Path, "");
      result = result.Replace(" ", "&nbsp;");
      result = result.Replace("\n", "<br />");
      p.WaitForExit();

      return result;
    }

    private string Compile(Assignment homework)
    {
      Process p = new Process();

      p.StartInfo.UseShellExecute = false;
      p.StartInfo.RedirectStandardOutput = true;
      p.StartInfo.RedirectStandardError = true;

      p.StartInfo.FileName = "g++";
      p.StartInfo.Arguments = homework.FullName + " -o" + homework.Path + homework.StudentId;
      p.Start();

      string result = p.StandardError.ReadToEnd();
      result = result.Replace(homework.Path, "");
      result = ToHtmlEncoding(result);

      p.WaitForExit();

      return (!string.IsNullOrEmpty(result)) ? result : "Success!!";
    }

    private string GetExecutionOutput(Assignment homework)
    {
      string actualOutput = ExecuteProgram(homework);
      string expectedOutput = GetExpectedOutput(homework);

      StringBuilder result = new StringBuilder();

      result.AppendLine("<h3>Actual</h3>");
      result.AppendLine("<p>" + ToHtmlEncoding(actualOutput) + "</p>");
      result.AppendLine("<br />");
      result.AppendLine("<h3>Expected</h3>");
      result.AppendLine("<p>" + ToHtmlEncoding(expectedOutput) + "</p>");
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

      return !string.IsNullOrEmpty(actualOutput) ? result.ToString() : "No output...";
    }

    private string ExecuteProgram(Assignment homework)
    {      
      executionProcess = new Process();

      executionProcess.StartInfo.UseShellExecute = false;
      executionProcess.StartInfo.RedirectStandardOutput = true;
      executionProcess.StartInfo.RedirectStandardError = true;
      executionProcess.StartInfo.RedirectStandardInput = true;

      executionProcess.StartInfo.FileName = homework.Path + homework.StudentId;      
      executionProcess.Start();

      executionTimer.Enabled = true;

      if (File.Exists(homework.Path + "input.txt"))
      {
        StreamReader reader = new StreamReader(homework.Path + "input.txt");

        while (!reader.EndOfStream)
        {
          executionProcess.StandardInput.WriteLine(reader.ReadLine());
        }
      }

      string output = executionProcess.StandardOutput.ReadToEnd();


      executionTimer.Enabled = false;

      executionProcess.Close();
      executionProcess.Dispose();

      // Don't leave binaries hanging around
      File.Delete(homework.Path + homework.StudentId);

      return output;
    }

    private void ExecutionTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
      // It's been long enough... kill the process
      Trace.TraceWarning("Grader is killing {0} because it has been running too long", executionProcess.ProcessName);
      executionProcess.Kill();
    }

    private string GetExpectedOutput(Assignment homework)
    {
      StreamReader reader = new StreamReader(homework.Path + "output.txt");

      return reader.ReadToEnd();
    }

    private string ToHtmlEncoding(string text)
    {
      text = text.Replace(" ", "&nbsp;");
      text = text.Replace("\n", "<br />");

      return text;
    }


  }
}


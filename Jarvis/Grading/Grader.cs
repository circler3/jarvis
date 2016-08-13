using System;
using System.Diagnostics;
using System.Text;
using HtmlDiff;
using System.IO;

namespace Jarvis
{
  public class Grader
  {
    public Grader ()
    {

    }

    public GradingResult Grade(Assignment homework)
    {
      GradingResult result = new GradingResult ();

      // Style check
      result.StyleMessage = StyleCheck(homework);

      // Compile
      result.CompileMessage = Compile (homework);     

      // Run tests
      if (result.CompileMessage == "Success!!") 
      {
        result.OutputMessage = RunProgram (homework);

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
      string timestamp = DateTime.Now.ToString ();

      StreamWriter writer = new StreamWriter (homework.Path + "results.txt", true);
      writer.WriteLine (timestamp + " " + homework.StudentId + " " + result.Grade); 
      writer.Flush ();
      writer.Close ();      
    }


    private string StyleCheck (Assignment homework)
    {
      Process p = new Process ();
      
      p.StartInfo.UseShellExecute = false;
      p.StartInfo.RedirectStandardOutput = true;
      p.StartInfo.RedirectStandardError = true;

      string styleExe = Jarvis.Config.AppSettings.Settings ["styleExe"].Value;
      p.StartInfo.FileName = styleExe;
      p.StartInfo.Arguments = Jarvis.Config.AppSettings.Settings["styleExemptions"].Value + " " + homework.FullName;
      p.Start();
      
      string result = p.StandardError.ReadToEnd ();
      result = result.Replace (homework.Path, "");
      result = result.Replace (" ", "&nbsp;");
      result = result.Replace("\n", "<br />");
      p.WaitForExit ();
      
      return result;
    }

    private string Compile(Assignment homework)
    {
      Process p = new Process ();

      p.StartInfo.UseShellExecute = false;
      p.StartInfo.RedirectStandardOutput = true;
      p.StartInfo.RedirectStandardError = true;

      p.StartInfo.FileName = "g++";
      p.StartInfo.Arguments = homework.FullName + " -o" + homework.Path + homework.StudentId;
      p.Start ();

      string result = p.StandardError.ReadToEnd ();
      result = result.Replace (homework.Path, "");
      result = ToHtmlEncoding (result);

      p.WaitForExit ();

      return (!string.IsNullOrEmpty (result)) ? result : "Success!!";
    }

    private string RunProgram (Assignment homework)
    {
      Process p = new Process ();

      p.StartInfo.UseShellExecute = false;
      p.StartInfo.RedirectStandardOutput = true;
      p.StartInfo.RedirectStandardError = true;
      p.StartInfo.RedirectStandardInput = true;

      p.StartInfo.FileName = homework.Path + homework.StudentId;
      //p.StartInfo.Arguments = file;
      p.Start ();

      if (File.Exists(homework.Path + "input.txt"))
      {
        StreamReader reader = new StreamReader (homework.Path + "input.txt");

        while (!reader.EndOfStream)
        {          
          p.StandardInput.WriteLine (reader.ReadLine ());
        }
      }
      string actualOutput = p.StandardOutput.ReadToEnd ();

      p.WaitForExit ();

      StringBuilder result = new StringBuilder();
      string expectedOutput = GetExpectedOutput (homework);

      result.AppendLine ("<h3>Actual</h3>");
      result.AppendLine ("<p>" + ToHtmlEncoding(actualOutput) + "</p>");
      result.AppendLine ("<br />");
      result.AppendLine ("<h3>Expected</h3>");
      result.AppendLine ("<p>" + ToHtmlEncoding(expectedOutput) + "</p>");
      result.AppendLine ("<br />");
      result.AppendLine ("<h3>Diff</h3>");

      string testDiff = string.Empty;

      if (actualOutput.Equals (expectedOutput, StringComparison.Ordinal)) 
      {
        testDiff = "No difference";
      }
      else
      {
        testDiff = HtmlDiff.HtmlDiff.Execute (actualOutput, expectedOutput);
      }

      result.AppendLine ("<p>" + testDiff + "</p>");

      // Don't leave binaries hanging around
      File.Delete (homework.Path + homework.StudentId);

      return !string.IsNullOrEmpty (actualOutput) ? result.ToString() : "No output...";
    }

    private string GetExpectedOutput(Assignment homework)
    {
      StreamReader reader = new StreamReader (homework.Path + "output.txt");

      return reader.ReadToEnd ();
    }

    private string ToHtmlEncoding(string text)
    {
      text = text.Replace (" ", "&nbsp;");
      text = text.Replace("\n", "<br />");

      return text;
    }


  }
}


using System;
using System.Diagnostics;

namespace Jarvis
{
  public class Grader
  {
    public Grader ()
    {

    }

    public GradingResult Grade(FileUploadResult uploadedFile)
    {
      GradingResult result = new GradingResult ();

      // Style check
      result.StyleMessage = "Style Message here";

      // Compile
      string compileMessage = Compile (uploadedFile.FullName, uploadedFile.Path);     
      result.CompileMessage = (!string.IsNullOrEmpty (compileMessage)) ? compileMessage : "Success!!";

      // Run tests
      if (result.CompileMessage == "Success!!") 
      {
        string outputMessage = RunProgram (uploadedFile.Path);
        result.OutputMessage = !string.IsNullOrEmpty (outputMessage) ? outputMessage : "No output...";
      }
      else
      {
        result.OutputMessage = "Didn't compile... :(";
      }

      return result;
    }

    private string Compile(string file, string path)
    {
      Process p = new Process ();

      p.StartInfo.UseShellExecute = false;
      p.StartInfo.RedirectStandardOutput = true;
      p.StartInfo.RedirectStandardError = true;

      p.StartInfo.FileName = "g++";
      p.StartInfo.Arguments = file + " -o" + path + "/a.out";
      p.Start ();

      string result = p.StandardError.ReadToEnd ();
      result = result.Replace (path + "/", "");
      result = result.Replace (" ", "&nbsp;");
      result = result.Replace("\n", "<br />");

      p.WaitForExit ();

      return result;
    }

    private string StyleCheck (string file, string path)
    {
      Process p = new Process ();

      p.StartInfo.UseShellExecute = false;
      p.StartInfo.RedirectStandardOutput = true;
      p.StartInfo.RedirectStandardError = true;

      p.StartInfo.FileName = "g++";
      p.StartInfo.Arguments = file;
      p.Start ();

      string output = p.StandardOutput.ReadToEnd ();
      string result = p.StandardError.ReadToEnd ();
      //result = result.Replace (path + "/", "");
      //result = result.Replace("\n", "<br />");
      p.WaitForExit ();

      return result;
    }

    private string RunProgram (string path)
    {
      Process p = new Process ();

      p.StartInfo.UseShellExecute = false;
      p.StartInfo.RedirectStandardOutput = true;
      p.StartInfo.RedirectStandardError = true;

      p.StartInfo.FileName = path + "/a.out";
      //p.StartInfo.Arguments = file;
      p.Start ();

      string result = p.StandardOutput.ReadToEnd ();
      //result = result.Replace (path + "/", "");
      //result = result.Replace("\n", "<br />");
      p.WaitForExit ();

      return result;
    }


  }
}


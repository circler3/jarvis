using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Jarvis
{
  public static class Compiler
  {
    public static string CompileCpp(string outputName, string path, List<string> includeDirs, List<string> sourceFiles)
    {
      string result = "";
      using (Process p = new Process())
      {
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardError = true;
        p.StartInfo.FileName = "g++";
        p.StartInfo.Arguments = "-DJARVIS -g -std=c++11 -Werror -o" + outputName;

        foreach (string include in includeDirs)
        {
          p.StartInfo.Arguments += " -I" + include;
        }

        // Expects the source to be full name
        foreach (string source in sourceFiles)
        {
          p.StartInfo.Arguments += " " + source;
        }

        p.Start();

        Logger.Trace("Compilation string: {0} {1}", p.StartInfo.FileName, p.StartInfo.Arguments);

        Jarvis.StudentProcesses.Add(p.Id);

        result = p.StandardError.ReadToEnd();
        result = result.Replace(path, "");
        result = JarvisEncoding.ToHtmlEncoding(result);

        p.WaitForExit();

        p.Close();
      }
      Logger.Trace("Compile result: {0}", result);

      return (!string.IsNullOrEmpty(result)) ? result : "Success!!";
    }
  }
}


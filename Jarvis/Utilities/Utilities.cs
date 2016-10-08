using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Text;

namespace Jarvis
{
  public class Utilities
  {
    public static List<TestCase> ReadTestCases(string path)
    {
      List<TestCase> testCases = new List<TestCase>();
      TestCase currentTest = null;
      int id = 0;

      if (File.Exists(path))
      {        
        FileStream stream = File.OpenRead(path);

        using (XmlReader reader = XmlReader.Create(stream))
        {
          while (reader.Read())
          {
            switch (reader.NodeType)
            {
              case XmlNodeType.Element:
                switch (reader.Name)
                {
                  case "config":
                    // Do nothing
                    break;

                  case "test":
                    if (currentTest != null)
                    {
                      testCases.Add(currentTest);
                    }

                    currentTest = new TestCase(id);
                    ++id;
                    break;

                  case "stdin":
                    currentTest.StdInputFile = reader.GetAttribute("file");
                    break;

                  case "stdout":
                    currentTest.StdOutputFile = reader.GetAttribute("file");
                    break;

                  case "filein":
                    {
                      string courseFile = reader.GetAttribute("courseFile");
                      string studentFile = reader.GetAttribute("studentFile");
                      currentTest.FileInputFiles.Add(new Tuple<string,string>(courseFile, studentFile));
                    }
                    break;

                  case "fileout":
                    {
                      string courseFile = reader.GetAttribute("courseFile");
                      string studentFile = reader.GetAttribute("studentFile");
                      currentTest.FileOutputFiles.Add(new Tuple<string,string>(courseFile, studentFile));
                    }
                    break;

                  case "ppmout":
                    {
                      string courseFile = reader.GetAttribute("courseFile");
                      string studentFile = reader.GetAttribute("studentFile");
                      currentTest.PpmOutputFiles.Add(new Tuple<string,string>(courseFile, studentFile));
                    }
                    break;
                }
                break;
            }
          }
        }

        if (currentTest != null)
        {
          testCases.Add(currentTest);
        }

        stream.Close();
        stream.Dispose();
      }

      return testCases;
    }

    public static string ReadFileContents(string path)
    {
      StreamReader reader = new StreamReader(path);

      return reader.ReadToEnd();
    }

    public static string ToHtmlEncoding(string text)
    {
      StringBuilder builder = new StringBuilder(text);

      builder.Replace("<", "&lt;");
      builder.Replace(">", "&gt;");
      builder.Replace(" ", "&nbsp;");
      builder.Replace("\n", "<br />");

      return builder.ToString();
    }

    public static string ToHtmlEncodingWithNewLines(string text)
    {
      StringBuilder builder = new StringBuilder(text);

      builder.Replace("<", "&lt;");
      builder.Replace(">", "&gt;");
      builder.Replace(" ", "&nbsp;");
      builder.Replace("\0", "<span style='color: #888888; font-size: 10px;'>\\0</span>");
      builder.Replace("\n", "<span style='color: #888888; font-size: 10px;'>\\n</span><br />");

      return builder.ToString();
    }

    public static string ToTextEncoding(string text)
    {
      StringBuilder builder = new StringBuilder(text);

      builder.Replace("<", "&lt;");
      builder.Replace(">", "&gt;");
      builder.Replace("&nbsp;", " ");
      builder.Replace("<br />", "\n");

      return builder.ToString();
    }

    public static void DirectoryCopy(string sourceDirName, string destDirName)
    {
      // Get the subdirectories for the specified directory.
      DirectoryInfo dir = new DirectoryInfo(sourceDirName);

      if (!dir.Exists)
      {
        throw new DirectoryNotFoundException(
          "Source directory does not exist or could not be found: "
          + sourceDirName);
      }

      DirectoryInfo[] dirs = dir.GetDirectories();
      // If the destination directory doesn't exist, create it.
      if (!Directory.Exists(destDirName))
      {
        Directory.CreateDirectory(destDirName);
      }

      // Get the files in the directory and copy them to the new location.
      FileInfo[] files = dir.GetFiles();
      foreach (FileInfo file in files)
      {
        string temppath = Path.Combine(destDirName, file.Name);
        file.CopyTo(temppath, false);
      }

      foreach (DirectoryInfo subdir in dirs)
      {
        string temppath = Path.Combine(destDirName, subdir.Name);
        DirectoryCopy(subdir.FullName, temppath);
      }
    }

    /// <summary>
    /// Converts the provided PPM file to a PNG file
    /// </summary>
    /// <returns>Error message returned by convert utility (if any)</returns>
    /// <param name="ppmFile">Full path to PPM file to convert</param>
    /// <param name="pngFile">Full path to PNG file output location</param>
    public static string convertPpmToPng(string ppmFile, string pngFile)
    {
      Logger.Info("Converting {0} to {1}", ppmFile, pngFile);
      string errorMsg = "";

      using (Process executionProcess = new Process())
      {
        executionProcess.StartInfo.RedirectStandardError = true;
        executionProcess.StartInfo.UseShellExecute = false;
        executionProcess.StartInfo.FileName = "convert";
        executionProcess.StartInfo.Arguments = ppmFile + " " + pngFile;
        executionProcess.Start();

        errorMsg = executionProcess.StandardError.ReadToEnd();
      }

      return errorMsg;
    }
  }
}


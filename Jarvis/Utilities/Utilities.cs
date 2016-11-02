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
                  
                  case "animation":
                    currentTest.Viewers.Add(new AnimationViewer(reader.GetAttribute("keywords")));
                    break;

                  case "source":
                    currentTest.ProvidedSourceFiles.Add(reader.GetAttribute("file"));
                    break;

                  case "stdin":
                    currentTest.StdInputFile = reader.GetAttribute("file");
                    break;

                  case "stdout":
                    currentTest.StdOutputFile = reader.GetAttribute("file");
                    currentTest.Viewers.Add(new StdoutViewer());
                    break;

                  case "filein":
                    {
                      string courseFile = reader.GetAttribute("courseFile");
                      string studentFile = reader.GetAttribute("studentFile");

                      currentTest.FileInputFiles.Add(new InputFile(courseFile, studentFile));
                    }
                    break;

                  case "fileout":
                    {
                      string courseFile = reader.GetAttribute("courseFile");
                      string studentFile = reader.GetAttribute("studentFile");
                      OutputFile.Type fileType = (OutputFile.Type)Enum.Parse(typeof(OutputFile.Type), reader.GetAttribute("type").ToUpper());

                      switch (fileType)
                      {
                        case OutputFile.Type.PPM:
                          currentTest.Viewers.Add(new PpmViewer());
                          break;

                        case OutputFile.Type.TEXT:
                          currentTest.Viewers.Add(new TextFileViewer());
                          break;
                      }

                      currentTest.FileOutputFiles.Add(new OutputFile(courseFile, studentFile, fileType));
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
      //Handle ASCII non-printables
      builder.Replace("\0", "<span style='color: #888888; font-size: 10px;'>\\0</span>");
      builder.Replace("\a", "<span style='color: #888888; font-size: 10px;'>\\a</span>");
      builder.Replace("\b", "<span style='color: #888888; font-size: 10px;'>\\b</span>");
      builder.Replace("\t", "<span style='color: #888888;'>&#8677;</span>"); //replace ascii tab with Unicode "RIGHT ARROW TO BAR"
      builder.Replace("\n", "<span style='color: #888888; font-size: 10px;'>\\n</span><br />");
      builder.Replace("\v", "<span style='color: #888888; font-size: 10px;'>\\v</span>");
      builder.Replace("\f", "<span style='color: #888888; font-size: 10px;'>\\f</span>");
      builder.Replace("\r", "<span style='color: #888888; font-size: 10px;'>\\r</span>");

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

    public static string GetDiff(string actual, string expected)
    {
      string diff = string.Empty;

      if (actual.Equals(expected, StringComparison.Ordinal))
      {
        diff = "No difference";
      }
      else
      {        
        diff = HtmlDiff.HtmlDiff.Execute(actual, expected);
      }

      return diff;
    }

    /// <summary>
    /// Builds the diff block.
    /// </summary>
    /// <returns>The diff block.</returns>
    /// <param name="source">Text that denotes from which output this diff block was generated.</param>
    /// <param name="htmlActualOutput">Html actual output.</param>
    /// <param name="htmlExpectedOutput">Html expected output.</param>
    /// <param name="htmlDiff">Html diff.</param>
    public static string BuildDiffBlock(string source, string htmlActualOutput, string htmlExpectedOutput, string htmlDiff)
    {
      StringBuilder result = new StringBuilder();
      result.Append("<table>");
      result.Append("<tr class='header expand' >");
//      result.Append("<th colspan='1'><span class='sign'></span></th>");
      result.Append("<th colspan='3'><p>" + source + "<span>[+]</span></p></th>");
      result.Append("</tr>");
      result.Append("<tr>");
      result.Append("<td>");
      result.Append("<h3>Actual</h3>");
      result.Append("<p>" + htmlActualOutput + "</p>");
      result.Append("</td>");
      result.Append("<td>");
      result.Append("<h3>Expected</h3>");
      result.Append("<p>" + htmlExpectedOutput + "</p>");
      result.Append("</td>");
      result.Append("<td>");
      result.Append("<h3>Diff</h3>");
      result.Append("<p>" + htmlDiff + "</p>");
      result.Append("</td>");
      result.Append("</tr>");
      result.Append("</table>");

      return result.ToString();
    }

    public static string ConvertToBase64(string pngFile)
    {
      string result = string.Empty;

      if (File.Exists(pngFile))
      {
        byte[] pngBytes = File.ReadAllBytes(pngFile);
        File.Delete(pngFile);

        result = Convert.ToBase64String(pngBytes);
      }

      return result;
    }
  }
}


using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;

namespace Jarvis
{
  public class PpmViewer : IViewer
  {
    public string ToHtml(TestCase test)
    {
      StringBuilder result = new StringBuilder();

      foreach (OutputFile file in test.FileOutputFiles)
      {
        if (file.FileType == OutputFile.Type.PPM)
        {
          string actualBase64Png = ConvertPpmToBase64Png(test.HomeworkPath + file.StudentFile);
          string expectedBase64Png = ConvertPpmToBase64Png(test.TestsPath + file.CourseFile);
          string htmlActual = string.Format("<img src='data:image/png;base64,{0}' />", actualBase64Png);
          string htmlExpected = string.Format("<img src='data:image/png;base64,{0}' />", expectedBase64Png);

          string ppmActual = File.ReadAllText(test.HomeworkPath + file.StudentFile);
          string ppmExpected = File.ReadAllText(test.TestsPath + file.CourseFile);

          string htmlDiff = string.Empty;

          if (ppmExpected.Equals(ppmActual, StringComparison.Ordinal))
          {
            htmlDiff = "No difference";
            test.Passed = true;
          }
          else
          {
            htmlDiff = "Differences detected";
            test.Passed = false;
          }

          string diffBlock = Utilities.BuildDiffBlock("From PPM image:", htmlActual, htmlExpected, htmlDiff);

          result.Append(diffBlock);
        }
      }

      return result.ToString();
    }

    /// <summary>
    /// Converts the provided PPM file to a PNG file
    /// </summary>
    /// <returns>Error message returned by convert utility (if any)</returns>
    /// <param name="ppmFile">Full path to PPM file to convert</param>
    /// <param name="pngFile">Full path to PNG file output location</param>
    public string ConvertPpmToBase64Png(string ppmFile)
    {
      string pngFile = string.Format("/tmp/{0}.png", Guid.NewGuid().ToString());
      Logger.Info("Converting {0} to {1}", ppmFile, pngFile);
      string result = string.Empty;

      using (Process executionProcess = new Process())
      {
        executionProcess.StartInfo.RedirectStandardError = true;
        executionProcess.StartInfo.UseShellExecute = false;
        executionProcess.StartInfo.FileName = "convert";
        executionProcess.StartInfo.Arguments = ppmFile + " " + pngFile;
        executionProcess.Start();

        executionProcess.WaitForExit();
      }

      byte[] pngBytes = File.ReadAllBytes(pngFile);

      if (File.Exists(pngFile))
      {
        File.Delete(pngFile);
      }

      result = Convert.ToBase64String(pngBytes);

      return result;
    }
  }
}


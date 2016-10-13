using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

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
          //string ppmActual = File.ReadAllText(test.HomeworkPath + file.StudentFile);
          //string ppmExpected = File.ReadAllText(test.TestsPath + file.CourseFile);

          string pngActual = ConvertPpmToPng(test.HomeworkPath + file.StudentFile);
          string pngExpected = ConvertPpmToPng(test.TestsPath + file.CourseFile);

          Bitmap actual = new Bitmap(pngActual);
          Bitmap expected = new Bitmap(pngExpected);

          bool match = true;
          bool sizeMismatch = false;
          if ((actual.Width == expected.Width) && (actual.Height == expected.Width))
          {            
            for (int i = 0; i < actual.Width; ++i)
            {
              for (int j = 0; j < actual.Height; ++j)
              {
                Color actualColor = expected.GetPixel(i, j);
                Color expectedColor = actual.GetPixel(i, j);

                if (Math.Abs(actualColor.R - expectedColor.R) > 10)
                {
                  match = false;
                }

                if (Math.Abs(actualColor.G - expectedColor.G) > 10)
                {
                  match = false;
                }

                if (Math.Abs(actualColor.B - expectedColor.B) > 10)
                {
                  match = false;
                }
              }
            }
          }
          else
          {
            match = false;
            sizeMismatch = true;
          }
            
          string actualBase64Png = ConvertToBase64(pngActual);
          string expectedBase64Png = ConvertToBase64(pngExpected);
          string htmlActual = string.Format("<img src='data:image/png;base64,{0}' />", actualBase64Png);
          string htmlExpected = string.Format("<img src='data:image/png;base64,{0}' />", expectedBase64Png);

          string htmlDiff = string.Empty;

          if (match)
          {
            htmlDiff = "No difference, or close enough... ;-]";
            test.Passed = true;
          }
          else
          {            
            htmlDiff = "Differences detected!";

            if (sizeMismatch)
            {
              htmlDiff += "<br />Actual Size: " + actual.Width + "x" + actual.Height + ", Expected Size: " + expected.Width + "x" + expected.Height;
            }
            //htmlDiff += "<br /><a href='data:text/html;base64," + Convert.ToBase64String( Encoding.ASCII.GetBytes()) + "'>Link here</a>";
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
    private string ConvertPpmToPng(string ppmFile)
    {
      string pngFile = string.Format("/tmp/{0}.png", Guid.NewGuid().ToString());
      Logger.Info("Converting {0} to {1}", ppmFile, pngFile);
      //string result = string.Empty;

      using (Process executionProcess = new Process())
      {
        executionProcess.StartInfo.RedirectStandardError = true;
        executionProcess.StartInfo.UseShellExecute = false;
        executionProcess.StartInfo.FileName = "convert";
        executionProcess.StartInfo.Arguments = ppmFile + " " + pngFile;
        executionProcess.Start();

        executionProcess.WaitForExit();
      }

      return pngFile;
    }

    private string ConvertToBase64(string pngFile)
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


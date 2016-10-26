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
    private TestCase testCase;
    public string ToHtml(TestCase test)
    {
      testCase = test;
      StringBuilder result = new StringBuilder();

      foreach (OutputFile file in test.FileOutputFiles)
      {
        if (file.FileType == OutputFile.Type.PPM)
        {
          string htmlDiff = string.Empty;
          string htmlActual = string.Empty;
          string htmlExpected = string.Empty;

          if (CheckPpmHeader(test.HomeworkPath + file.StudentFile))
          {
            string pngExpected = ConvertPpmToPng(test.TestsPath + file.CourseFile);
            Bitmap expected = new Bitmap(pngExpected);
            string expectedBase64Png = ConvertToBase64(pngExpected);
            htmlExpected = string.Format("<img src='data:image/png;base64,{0}' />", expectedBase64Png);

            try
            {
            
              bool match = true;
              bool sizeMismatch = false;
              string pngActual = ConvertPpmToPng(test.HomeworkPath + file.StudentFile);

              if (File.Exists(pngActual))
              {
                Bitmap actual = new Bitmap(pngActual);
                
                if ((actual.Width == expected.Width) && (actual.Height == expected.Height))
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
                htmlActual = string.Format("<img src='data:image/png;base64,{0}' />", actualBase64Png);

                
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
              }
              else
              {
                htmlDiff = "Differences detected!";
                htmlActual = "No image found!";
                test.Passed = false;
              }
            }
            catch
            {
              htmlDiff = "Invalid image!";
              htmlActual = "Invalid image!";
              test.Passed = false;
            }
          }
          else // Invalid header
          {
            htmlDiff = "Didn't run due to invalid PPM header.";
            htmlExpected = "Didn't run due to invalid PPM header.";
            htmlActual = "Invalid PPM header!<br />Please check for correct PPM before uploading to Jarvis.";
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
      
      string pngFile = string.Format("{0}{1}.png", testCase.HomeworkPath, Guid.NewGuid().ToString());
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

    private bool CheckPpmHeader(string ppmFile)
    {
      bool result = true;
      List<string> header = new List<string>();

      using (StreamReader reader = new StreamReader(ppmFile))
      {
        for (int i = 0; i < 3 && !reader.EndOfStream; ++i)
        {
          header.Add(reader.ReadLine().ToLower());
        }
      }

      if (header.Count < 3)
      {
        result = false;
      }
      else if (header[0].Trim() != "p3")
      {
        result = false;
      }
      else if (header[2].Trim() != "255")
      {
        result = false;
      }

      return result;
    }
  }
}


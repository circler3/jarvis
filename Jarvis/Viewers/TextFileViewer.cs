using System;
using System.Text;
using System.IO;

namespace Jarvis
{
  public class TextFileViewer : IViewer
  {
    public string ToHtml(TestCase test)
    {
      StringBuilder result = new StringBuilder();

      // check for file output files
      if (test.FileOutputFiles.Count > 0)
      {
        foreach (OutputFile fileout in test.FileOutputFiles)
        {
          string expectedOutput = Utilities.ReadFileContents(test.TestsPath + fileout.CourseFile);
          FileInfo info = new FileInfo(test.HomeworkPath + fileout.StudentFile);
          if (File.Exists(test.HomeworkPath + fileout.StudentFile) && info.Length < 1000000)
          {
            string actualOutput = Utilities.ReadFileContents(test.HomeworkPath + fileout.StudentFile);

            string htmlExpectedOutput = Utilities.ToHtmlEncodingWithNewLines(expectedOutput);
            string htmlActualOutput = Utilities.ToHtmlEncodingWithNewLines(actualOutput);

            string htmlDiff = Utilities.GetDiff(htmlActualOutput, htmlExpectedOutput);

            result.Append(Utilities.BuildDiffBlock("From " + fileout.StudentFile + ":", htmlActualOutput, htmlExpectedOutput, htmlDiff));

            test.Passed = htmlDiff.Contains("No difference");
          }
          else if (!File.Exists(test.HomeworkPath + fileout.StudentFile))
          {
            test.Passed = false;
            result.Append("<p>Cannot find output file: " + fileout.StudentFile + "</p>");
          }
          else if (info.Length >= 1000000)
          {
            test.Passed = false;
            result.Append("<p>The file output was too large [" + info.Length.ToString() + "] bytes!!!");
          }
        }
      }

      return result.ToString();
    }
  }
}

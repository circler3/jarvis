using System;
using System.Text;
using System.IO;

namespace Jarvis
{
  public class StdoutViewer : IViewer
  {
    public string ToHtml(TestCase test)
    {
      StringBuilder result = new StringBuilder();

      // Check for std input
      if (!string.IsNullOrEmpty(test.StdInText))
      {
        string htmlStdInput = JarvisEncoding.ToHtmlEncodingWithNewLines(test.StdInText);
        string caseHeaderText = "Test Input:";

        result.Append(Utilities.BuildInputBlock(caseHeaderText, htmlStdInput));
      }

      // check for std output file
      if (!string.IsNullOrEmpty(test.StdOutputFile))
      {

        if (test.StdOutText.Length < 100000)
        {
          string expectedStdOutput = Utilities.ReadFileContents(test.TestsPath + test.StdOutputFile);

          string htmlActualStdOutput = JarvisEncoding.ToHtmlEncodingWithNewLines(test.StdOutText);
          string htmlExpectedStdOutput = JarvisEncoding.ToHtmlEncodingWithNewLines(expectedStdOutput);
          string htmlDiff = JarvisEncoding.GetDiff(htmlActualStdOutput, htmlExpectedStdOutput);

          string caseHeaderText = "Test Output:";

          if (string.IsNullOrWhiteSpace(test.StdOutText))
          {
            caseHeaderText += "<br/ ><span style=\"color:#ff0000\">Warning: actual output was empty!</span>";
          }

          result.Append(Utilities.BuildDiffBlock(caseHeaderText, htmlActualStdOutput, htmlExpectedStdOutput, htmlDiff));

          test.Passed = htmlDiff.Contains("No difference");
        }
        else
        {
          test.Passed = false;
          result.Append("<p>Too much output!</p>");
        }
      }

      return result.ToString();
    }
  }
}

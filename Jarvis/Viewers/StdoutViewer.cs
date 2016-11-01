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

      // check for std output file
      if (!string.IsNullOrEmpty(test.StdOutputFile))
      {
        string expectedStdOutput = Utilities.ReadFileContents(test.TestsPath + test.StdOutputFile);

        string htmlActualStdOutput = Utilities.ToHtmlEncodingWithNewLines(test.StdOutText);
        string htmlExpectedStdOutput = Utilities.ToHtmlEncodingWithNewLines(expectedStdOutput);
        string htmlDiff = Utilities.GetDiff(htmlActualStdOutput, htmlExpectedStdOutput);

        string caseHeaderText = "From stdout:";

        if (string.IsNullOrWhiteSpace(test.StdOutText))
        {
          caseHeaderText += "<br/ ><span style=\"color:#ff0000\">Warning: actual output was empty!</span>";
        }

        result.Append(Utilities.BuildDiffBlock(caseHeaderText, htmlActualStdOutput, htmlExpectedStdOutput, htmlDiff));

        test.Passed = htmlDiff.Contains("No difference");
      }


      return result.ToString();
    }
  }
}

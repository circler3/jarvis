using System;
using System.Text;

namespace Jarvis
{
  public class GradingResult
  {
    public bool ValidHeader { get; set; }
    public string CompileMessage { get; set; }
    public string StyleMessage { get; set; }
    public string OutputMessage { get; set; }
    public bool CorrectOutput { get; set; }

    public string Grade
    {
      get
      {
        int score = 100;

        if (!StyleMessage.Contains("Total&nbsp;errors&nbsp;found:&nbsp;0"))
        {
          score -= 20;
        }

        if (!CorrectOutput)
        {
          score -= 80;
        }

        return score.ToString() + "%";
      }
    }

    public string GradingComment
    {
      get
      {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Header: " + ValidHeader.ToString());
        builder.AppendLine("Compile: " + CompileMessage);
        builder.AppendLine("Style: " + StyleMessage);
        builder.AppendLine("Correct output: " + CorrectOutput.ToString());

        return builder.ToString();
      }
    }
  }
}


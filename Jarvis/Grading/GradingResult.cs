using System;
using System.Text;

namespace Jarvis
{
  public class GradingResult
  {
    public GradingResult(Assignment assignment)
    {
      Assignment = assignment;
      StyleMessage = string.Empty;
      OutputMessage = string.Empty;
    }

    public Assignment Assignment { get; set; }
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

        return score.ToString();
      }
    }
      
    public string ToHtml()
    {
      StringBuilder builder = new StringBuilder();

      builder.AppendFormat("<h1>Results - {0}%</h1>", Grade);
      builder.AppendFormat("<h2>Style Check</h2>");
      builder.AppendFormat("<p>{0}</p>", StyleMessage);
      builder.AppendFormat("<h2>Compile</h2>");
      builder.AppendFormat("<p>{0}</p>", CompileMessage);
      builder.AppendFormat("<h2>Output</h2>");
      builder.AppendFormat("{0}", OutputMessage);

      return builder.ToString();        
    }

    public string ToText()
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


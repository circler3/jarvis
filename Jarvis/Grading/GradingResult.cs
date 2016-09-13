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
      InvalidOutputPercentage = 0.0;
    }

    public Assignment Assignment { get; set; }
    public bool ValidHeader { get; set; }
    public string CompileMessage { get; set; }
    public string StyleMessage { get; set; }
    public string OutputMessage { get; set; }
    public double InvalidOutputPercentage { get; set; }

    public double Grade
    {
      get
      {
        double score = 10.0f;

        if (!StyleMessage.Contains("Total&nbsp;errors&nbsp;found:&nbsp;0"))
        {
          score -= 2.0f;
        }

        if (InvalidOutputPercentage > 0.0)
        {
          score -= (int)(8.0f * InvalidOutputPercentage);
        }

        return score;
      }
    }
      
    public string ToHtml()
    {
      StringBuilder builder = new StringBuilder();

      builder.AppendFormat("<h1>Results - {0}%</h1>", Grade * 10);
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
      double correctPercentage = (1.0 - InvalidOutputPercentage) * 10;
      builder.AppendLine("Correct output: " + correctPercentage + "%");
      
      return builder.ToString();      
    }
  }
}


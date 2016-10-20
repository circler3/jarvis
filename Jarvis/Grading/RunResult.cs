using System;
using System.Text;

namespace Jarvis
{
  public class RunResult
  {
    public RunResult(Assignment assignment)
    {
      Assignment = assignment;
      StyleMessage = string.Empty;
      OutputMessage = string.Empty;
      OutputPercentage = 0.0;
    }

    public Assignment Assignment { get; set; }
    public bool ValidHeader { get; set; }
    public string CompileMessage { get; set; }
    public string StyleMessage { get; set; }
    public string JarvisStyleMessage { get; set; }
    public string OutputMessage { get; set; }
    public double OutputPercentage { get; set; }

    public double Grade
    {
      get
      {
        double score = 0.0f;

        // 20% of grade for style
        //if (StyleMessage.Contains("Total&nbsp;errors&nbsp;found:&nbsp;0"))
        //{
          score += 2.0f;
        //}

        // 80% of grade for correct execution
        if (CompileMessage == "Success!!")
        {
          // Assign points for percentage of correct test cases
          score += (int)(8.0f * OutputPercentage);
        }
        else
        {
          // Lose all 80% if the assignment doesn't compile
        }

        return score;
      }
    }
      
    public string ToHtml()
    {
      StringBuilder builder = new StringBuilder();

      builder.AppendFormat("<h1>Results - {0}%</h1>", Grade * 10);
      builder.AppendFormat("<h2>Google Style Check</h2>");
      builder.AppendFormat("<p>{0}</p>", StyleMessage);
      builder.AppendFormat("<h2>Jarvis Style Check</h2>");
      builder.AppendFormat("<p>{0}</p>", JarvisStyleMessage);
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
      builder.AppendLine("Score: " + (Grade * 10).ToString() + "%");
      
      return builder.ToString();
    }
  }
}


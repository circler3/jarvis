using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Jarvis
{
  public class StyleExecutor
  {
    private int lineCount = 0;

    private List<AbstractStyleChecker> checkers = new List<AbstractStyleChecker>();
    private List<string> errors = new List<string>();

    public StyleExecutor()
    {
      // TODO determine which checkers to use?
      checkers.Add(new IndentationChecker(this));
    }

    public string Run(string file)
    {
      StringBuilder result = new StringBuilder();
      using (StreamReader reader = new StreamReader(File.OpenRead(file)))
      {
        while (!reader.EndOfStream)
        {
          string line = reader.ReadLine();
          lineCount++;

          foreach (AbstractStyleChecker checker in checkers)
          {
            checker.Check(line);
          }
        }

        reader.Close();
      }
      foreach (string message in errors)
      {
        result.AppendLine(message);
      }

      if (result.Length == 0)
      {
        result.Append("No errors!");
      }

      return result.ToString();
    }

    public void ReportStyleError(string text, params object[] items)
    {
      string message = string.Format(text, items);

      message = string.Format("Line {0}: {1}", lineCount, message);

      errors.Add(message);
    }
  }
}


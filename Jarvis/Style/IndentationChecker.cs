using System;
using System.Text;

namespace Jarvis
{
  public class IndentationChecker : AbstractStyleChecker
  {
    private const int INDENT_SIZE = 2;
    private int currentIndentLevel = 0;
    private bool inComment = false;

    public IndentationChecker(StyleExecutor executor) : base(executor)
    {
      // empty
    }

    public override void Check(string line)
    {
      int spaceCount = 0;
      if (line.Contains("}"))
      {
        currentIndentLevel--;
      }

      if (line.Contains("/*"))
      {
        inComment = true;
      }

      for (int i = 0; i < line.Length; ++i)
      {
        if (line[i] == ' ')
        {
          spaceCount++;
        }
        else
        {
          break;
        }
      }

      if (line != "" && !inComment && spaceCount != currentIndentLevel * INDENT_SIZE)
      {
        executor.ReportStyleError("Indentation should be {0} spaces", currentIndentLevel * INDENT_SIZE);
      }

      if (line.Contains("{"))
      {
        currentIndentLevel++;
      }

      if (line.Contains("*/"))
      {
        inComment = false;
      }
    }

    private string GetIndentString(int level)
    {
      StringBuilder result = new StringBuilder();

      for (int i = 0; i < level; ++i)
      {
        for (int j = 0; j < INDENT_SIZE; ++j)
        {
          result.Append(" ");
        }
      }

      return result.ToString();
    }
  }
}


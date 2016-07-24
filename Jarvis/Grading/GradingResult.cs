using System;

namespace Jarvis
{
  public class GradingResult
  {
    public bool ValidHeader { get; set;}
    public string CompileMessage { get; set; }
    public string StyleMessage { get; set; }
    public string OutputMessage { get; set; }
    public bool CorrectOutput { get; set; }

    public string Grade
    {
      get
      {
        string grade = "Pass";

        if (CompileMessage != "Success!!")
        {
          grade = "Fail";
        }

        if (!StyleMessage.Contains("Total&nbsp;errors&nbsp;found:&nbsp;0"))
        {
          grade = "Fail";
        }

        if (!CorrectOutput)
        {
          grade = "Fail";
        }

        return grade;
      }
    }

  }
}


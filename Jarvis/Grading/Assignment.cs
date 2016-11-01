using System;
using System.IO;
using System.Collections.Generic;

namespace Jarvis
{
  public class Assignment
  {
    public Assignment()
    {
      FileNames = new List<string>();
    }

    public bool IsGrade { get; set; }
    public bool ValidHeader { get; set; }
    public string ErrorMessage { get; set; }
    public string StudentId { get; set; }
    public string Course { get; set; }
    public string HomeworkId { get; set; }
    public string Section { get; set; }
    public string Path { get; set; }
    public List<string> FileNames { get; set; }
  }
}

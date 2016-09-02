using System;
using System.IO;

namespace Jarvis
{
  public class Assignment
  {
    public bool IsGrade { get; set; }
    public bool ValidHeader { get; set; }
    public string ErrorMessage { get; set; }
    public string StudentId { get; set; }
    public string Course { get; set; }
    public string HomeworkId { get; set; }
    public string Section { get; set; }
    public string Path { get; set; }

    public string Filename 
    { 
      get
      {
        string name;

        if (IsGrade)
        {
          name = StudentId + "_grade.cpp";
        }
        else
        {
          name = StudentId + ".cpp";
        }

        return name;
      }
    }

    public string FullPath 
    {
      get
      {
        return Path + Filename;
      }
    }
  }
}


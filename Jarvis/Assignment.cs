using System;
using System.IO;

namespace Jarvis
{
  public class Assignment
  {
    public bool ValidHeader { get; set; }
    public string StudentId { get; set; }
    public string Course { get; set; }
    public string HomeworkId { get; set; }
    public string Section { get; set; }
    public string Path { get; set; }
    public string Filename 
    { 
      get 
      { 
        return StudentId + ".cpp"; 
      }
    }

    public string FullName
    {
      get
      {
        return Path + Filename;
      }
    }
  }
}


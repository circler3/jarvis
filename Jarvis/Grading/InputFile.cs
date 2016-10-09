using System;

namespace Jarvis
{
  public class InputFile
  {
    public string CourseFile { get; set; }
    public string StudentFile { get; set; }

    public InputFile(string course, string student)
    {
      CourseFile = course;
      StudentFile = student;
    }
  }
}


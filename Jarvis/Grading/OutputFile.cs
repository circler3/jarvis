using System;

namespace Jarvis
{
  public class OutputFile
  {
    public enum Type
    {
      TEXT,
      PPM
    }

    public string CourseFile { get; private set; }
    public string StudentFile { get; private set; }
    public Type FileType { get; private set; }

    public OutputFile(string course, string student, Type type)
    {
      CourseFile = course;
      StudentFile = student;
      FileType = type;
    }
  }
}


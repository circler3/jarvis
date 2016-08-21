using Nancy;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Jarvis
{
  public class FileUploadHandler
  {
    // Note: Students upload single cpp files  
    public Assignment HandleStudentUpload(HttpFile file)
    {
      Logger.Trace ("Handling student upload");
      // Check file header
      StreamReader reader = new StreamReader(file.Value);

      List<string> header = new List<string>();
      for (int i = 0; i < 5 && !reader.EndOfStream; ++i)
      {
        header.Add(reader.ReadLine().ToLower());
      }

      Assignment homework = ParseHeader(header);

      if (homework.ValidHeader)
      {
        string path = string.Format("{0}/courses/{1}/hw{2}/section{3}/{4}", Jarvis.Config.AppSettings.Settings["workingDir"].Value, homework.Course.ToLower(), homework.HomeworkId, homework.Section, homework.StudentId);

        homework.Path = path;
        Logger.Trace ("Checking if {0} exists", path);

        Directory.CreateDirectory(path);

        using (FileStream destinationStream = File.Create(homework.FullPath))
        {
          file.Value.Position = 0;
          file.Value.CopyTo(destinationStream);
        }
      }

      return homework;
    }

    // Note: Graders upload zip files containing many cpp files
    public List<Assignment> HandleGraderUpload(string gradingDir, HttpFile file)
    {      
      Logger.Trace ("Handling grader upload");
      List<Assignment> assignments = new List<Assignment>();

      Logger.Trace("Creating grading directory {0}", gradingDir);
      // create grading directory
      Directory.CreateDirectory(gradingDir);

      // Copy zip file to grading directory
      using (FileStream destinationStream = File.Create(gradingDir + "files.zip"))
      {
        file.Value.Position = 0;
        file.Value.CopyTo(destinationStream);
      }

      // unzip contents
      ZipFile.ExtractToDirectory(gradingDir + "files.zip", gradingDir + "files");

      string[] files = Directory.GetFiles(gradingDir + "files");
      Logger.Trace("Found {0} files in grader zip file", files.Length);

      // for each file
      foreach (string cppFile in files)
      {
        StreamReader reader = new StreamReader(File.OpenRead(cppFile));
        List<string> header = new List<string>();
        for (int i = 0; i < 5 && !reader.EndOfStream; ++i)
        {
          header.Add(reader.ReadLine().ToLower());
        }

        reader.Close();

        // check header and make assigment object
        Assignment assignment = ParseHeader(header);

        Logger.Trace("Found assignment with A#: {0}, Course: {1}, Section: {2}, HW#: {3}", assignment.StudentId, assignment.Course, assignment.Section, assignment.HomeworkId);

        if (assignment.ValidHeader)
        {
          string path = string.Format("{0}section{1}", gradingDir, assignment.Section);
          Directory.CreateDirectory(path);
          assignment.Path = path;
          // move to section and rename each file
          File.Move(cppFile, assignment.FullPath);
        }
        else
        {
          assignment.Path = cppFile;
        }

        assignments.Add(assignment);
      }
      
      return assignments;
    }

    private Assignment ParseHeader(List<string> header)
    {
      Assignment homework = new Assignment();

      foreach (String s in header)
      {
        if (s.Contains("a#:"))
        {
          homework.StudentId = s.Split(':')[1].Trim();
          Logger.Trace ("Parse header student id: {0}", homework.StudentId);
        }
        else if (s.Contains("course:"))
        {
          homework.Course = s.Split(':')[1].Trim();
          Logger.Trace ("Parse header course: {0}", homework.Course);
        }
        else if (s.Contains("section:"))
        {
          homework.Section = s.Split(':')[1].Trim();
          Logger.Trace ("Parse header section: {0}", homework.Section);
        }
        else if (s.Contains("hw#:"))
        {
          homework.HomeworkId = s.Split(':')[1].Trim();
          Logger.Trace ("Parse header homework id: {0}", homework.HomeworkId);
        }
      }

      if (homework.StudentId != String.Empty && homework.Course != String.Empty && homework.Section != String.Empty && homework.HomeworkId != String.Empty)
      {
        Logger.Trace ("Parse found valid header");
        homework.ValidHeader = true;
      }
      else
      {
        Logger.Trace ("Parse found invalid header");
        // Invalid header, reject assignment
        homework.ValidHeader = false;
      }

      return homework;
    }
  }
}

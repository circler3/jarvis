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
        homework.AssignmentPath = Jarvis.Config.AppSettings.Settings["workingDir"].Value + "/courses/" + homework.Course.ToLower() + "/hw" + homework.HomeworkId + "/";
        string sectionDir = homework.AssignmentPath + "section" + homework.Section;

        // Check that directories exist
        if (Directory.Exists(sectionDir))
        {
          // Upload to correct directory
          homework.Path = sectionDir + "/" + homework.StudentId + "/";

          if (!Directory.Exists(homework.Path))
          {
            Directory.CreateDirectory(homework.Path);
          }

          using (FileStream destinationStream = File.Create(homework.Path + "/" + homework.Filename))
          {
            file.Value.Position = 0;
            file.Value.CopyTo(destinationStream);
          }
        }
        else
        {
          homework.ValidHeader = false;
        }
      }

      return homework;
    }

    // Note: Graders upload zip files containing many cpp files
    public List<Assignment> HandleGraderUpload(string gradingDir, HttpFile file)
    {
      List<Assignment> assignments = new List<Assignment>();

      // create grading directory
      Directory.CreateDirectory(gradingDir);

      // create section directories
      int sectionCount = int.Parse(Jarvis.Config.AppSettings.Settings["sectionCount"].Value);

      for (int i = 1; i <= sectionCount; ++i)
      {
        Directory.CreateDirectory(gradingDir + "section" + i.ToString());
      }

      // Copy zip file to grading directory
      using (FileStream destinationStream = File.Create(gradingDir + "files.zip"))
      {
        file.Value.Position = 0;
        file.Value.CopyTo(destinationStream);
      }

      // unzip contents
      ZipFile.ExtractToDirectory(gradingDir + "files.zip", gradingDir + "files");

      string[] files = Directory.GetFiles(gradingDir + "files");
      // for each file
      foreach (string cppFile in files)
      {
        StreamReader reader = new StreamReader(File.OpenRead(cppFile));
        List<string> header = new List<string>();
        for (int i = 0; i < 5 && !reader.EndOfStream; ++i)
        {
          header.Add(reader.ReadLine().ToLower());
        }

        // check header and make assigment object
        Assignment assignment = ParseHeader(header);
        if (assignment.ValidHeader)
        {
          assignment.Path = string.Format("{0}/section{1}/{2}_{3}.cpp", gradingDir, assignment.Section, assignment.StudentId, assignment.HomeworkId);
          // move to section and rename each file
          File.Move(cppFile, assignment.Path);
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
        }
        else if (s.Contains("course:"))
        {
          homework.Course = s.Split(':')[1].Trim();
        }
        else if (s.Contains("section:"))
        {
          homework.Section = s.Split(':')[1].Trim();
        }
        else if (s.Contains("hw#:"))
        {
          homework.HomeworkId = s.Split(':')[1].Trim();
        }
      }

      if (homework.StudentId != String.Empty && homework.Course != String.Empty && homework.Section != String.Empty && homework.HomeworkId != String.Empty)
      {
        homework.ValidHeader = true;
      }
      else
      {
        // Invalid header, reject assignment
        homework.ValidHeader = false;
      }

      return homework;
    }
  }
}

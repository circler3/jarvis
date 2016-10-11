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
      Jarvis.Stats.TotalFilesProcessed++;

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
        string path = string.Format("{0}/courses/{1}/hw{2}/section{3}/{4}/", Jarvis.Config.AppSettings.Settings["workingDir"].Value, homework.Course.ToLower(), homework.HomeworkId, homework.Section, homework.StudentId);

        homework.Path = path;
        Logger.Trace ("Checking if {0} exists", path);

        Directory.CreateDirectory(path);

        if (File.Exists(homework.FullPath))
        {
          File.Delete(homework.FullPath);
        }

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

        assignment.Path = string.Format("{0}section{1}/", gradingDir, assignment.Section);
        if (assignment.ValidHeader)
        {
          Directory.CreateDirectory(assignment.Path);
          // move to section and rename each file

          try
          {
            File.Move(cppFile, assignment.FullPath);
            assignments.Add(assignment);
          }
          catch (IOException)
          {
            Logger.Warn("File " + assignment.FullPath + " already exists! Possible cheating?!");
          }
        }
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
          homework.Section = s.Split(':')[1].TrimStart(new char[] { ' ', '0' }).Trim(); // Trim spaces and leading 0's
          Logger.Trace ("Parse header section: {0}", homework.Section);
        }
        else if (s.Contains("hw#:"))
        {
          homework.HomeworkId = s.Split(':')[1].Trim();
          Logger.Trace ("Parse header homework id: {0}", homework.HomeworkId);
        }
      }

      // Quality check on header
      if (string.IsNullOrEmpty(homework.StudentId))
      {
        homework.ValidHeader = false;
        homework.ErrorMessage = "Header is missing a student ID.";
        Logger.Warn(homework.ErrorMessage);
      }
      else if (!homework.StudentId.Contains("a"))
      {
        homework.ValidHeader = false;
        homework.ErrorMessage = "A# should be in the format: a09999999, including the 'a'";
        Logger.Warn(homework.ErrorMessage);
      }
      else if (homework.StudentId.Equals("a09999999", StringComparison.OrdinalIgnoreCase))
      {
        homework.ValidHeader = false;
        homework.ErrorMessage = "Header contains default A#: A09999999.";
        Logger.Warn(homework.ErrorMessage);
      }
      else if (string.IsNullOrEmpty(homework.Course))
      {
        homework.ValidHeader = false;
        homework.ErrorMessage = "Header is missing course information.";
        Logger.Warn(homework.ErrorMessage);
      }
      else if (ValidateCourse(homework.Course))
      {
        homework.ValidHeader = false;
        homework.ErrorMessage = string.Format("{0} is not a valid course.", homework.Course);
        Logger.Warn(homework.ErrorMessage);
      }
      else if (string.IsNullOrEmpty(homework.HomeworkId))
      {
        homework.ValidHeader = false;
        homework.ErrorMessage = "Header is missing homework ID.";
        Logger.Warn(homework.ErrorMessage);
      }
      else if (ValidateHomeworkId(homework.Course, homework.HomeworkId))
      {
        homework.ValidHeader = false;
        homework.ErrorMessage = string.Format("{0} is not a valid homework ID.", homework.HomeworkId);
        Logger.Warn(homework.ErrorMessage);
      }
      else if (string.IsNullOrEmpty(homework.Section))
      {
        homework.ValidHeader = false;
        homework.ErrorMessage = "Header is missing section information.";
        Logger.Warn(homework.ErrorMessage);
      }
      else if (ValidateSection(homework.Course, homework.HomeworkId, homework.Section))
      {
        homework.ValidHeader = false;
        homework.ErrorMessage = string.Format("{0} is not a valid section.", homework.Section);
        Logger.Warn(homework.ErrorMessage);
      }
      else
      {
        homework.ValidHeader = true;
        Logger.Info ("Parse found valid header");
      }

      return homework;
    }

    private bool ValidateCourse(string course)
    {      
      string coursePath = string.Format("{0}/courses/{1}", Jarvis.Config.AppSettings.Settings["workingDir"].Value, course);

      Logger.Trace("Validating course with path {0}", coursePath);

      return !Directory.Exists(coursePath);          
    }

    private bool ValidateHomeworkId(string course, string homeworkdId)
    {
      string hwPath = string.Format("{0}/courses/{1}/hw{2}", Jarvis.Config.AppSettings.Settings["workingDir"].Value, course, homeworkdId);

      Logger.Trace("Validating course with path {0}", hwPath);

      return !Directory.Exists(hwPath);
    }

    private bool ValidateSection(string course, string hwId, string section)
    {
      string sectionPath = string.Format("{0}/courses/{1}/hw{2}/section{3}", Jarvis.Config.AppSettings.Settings["workingDir"].Value, course, hwId, section);

      Logger.Trace("Validating course with path {0}", sectionPath);

      return !Directory.Exists(sectionPath);
    }

  }
}

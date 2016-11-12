using Nancy;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;


namespace Jarvis
{
  public class FileUploadHandler
  {
    // Note: Students upload single cpp files  
    public Assignment HandleStudentUpload(List<HttpFile> files)
    {
      Jarvis.Stats.TotalFilesProcessed++;

      Logger.Trace ("Handling student upload of {0} files", files.Count);

      // Look through uploaded files for a valid header
      Assignment homework = null;
      foreach (HttpFile file in files)
      {
        homework = ParseHeader(file.Value);

        if (homework.ValidHeader)
        {
          Logger.Trace("Found valid header in file {0}", file.Name);
          break;
        }
      }

      if (homework == null)
      {
        homework = new Assignment();
        homework.ValidHeader = false;
        homework.ErrorMessage = "Invalid file upload!";
      }

      if (homework.ValidHeader)
      {
        string path = string.Format("{0}/courses/{1}/hw{2}/section{3}/{4}/", Jarvis.Config.AppSettings.Settings["workingDir"].Value, homework.Course.ToLower(), homework.HomeworkId, homework.Section, homework.StudentId);

        homework.Path = path;

        Directory.CreateDirectory(path);

        foreach (HttpFile file in files)
        {
          Logger.Trace("file.Name: {0}", file.Name);

          homework.FileNames.Add(file.Name);

          if (File.Exists(homework.Path + file.Name))
          {
            File.Delete(homework.Path + file.Name);
          }

          Logger.Trace("Copying {0} to {1}", file.Name, homework.Path);

          using (FileStream destinationStream = File.Create(homework.Path + file.Name))
          {
            file.Value.Position = 0;
            file.Value.CopyTo(destinationStream);
          }
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

      string[] allFiles = Directory.GetFiles(gradingDir + "files");
      Logger.Trace("Found {0} files in grader zip file", allFiles.Length);

      Dictionary<string, List<string>> assignmentFiles = GetAssignmentFiles(allFiles);

      // for each file
      foreach (List<string> files in assignmentFiles.Values)
      {
        // check header and make assigment object
        Assignment assignment = ParseHeader(File.OpenRead(files[0]));

        if (assignment.ValidHeader)
        {
          foreach (string oneFile in files)
          {
            string newFilename = Path.GetFileName(oneFile);
            newFilename = newFilename.Substring(newFilename.LastIndexOf("_") + 1);
            assignment.FileNames.Add(newFilename);
          }
          
          Logger.Trace("Found assignment with A#: {0}, Course: {1}, Section: {2}, HW#: {3} and {4} files", 
            assignment.StudentId, assignment.Course, assignment.Section, assignment.HomeworkId, assignment.FileNames.Count);
          
          assignment.Path = string.Format("{0}section{1}/{2}/", gradingDir, assignment.Section, assignment.StudentId);
          Directory.CreateDirectory(assignment.Path);

          try
          {
            for (int i = 0; i < files.Count; ++i)
            {
              // move to section and rename each file
              File.Move(files[i], assignment.Path + assignment.FileNames[i]);
            }

            assignments.Add(assignment);
          }
          catch (IOException)
          {
            Logger.Warn("File " + assignment.Path + " already exists! Possible cheating?!");
          }
        }
      }
      
      return assignments;
    }

    private Dictionary<string, List<string>> GetAssignmentFiles(string[] files)
    {
      Dictionary<string, List<string>> assignmentFiles = new Dictionary<string, List<string>>();

      foreach (string file in files)
      {
        // parse file to unique portion
        string key = file.Substring(0, file.LastIndexOf('_') - 2);

        // look in dictionary, and add or create new item
        if (!assignmentFiles.ContainsKey(key))
        {
          assignmentFiles.Add(key, new List<string>());
        }

        assignmentFiles[key].Add(file);
      }

      return assignmentFiles;
    }

    private Assignment ParseHeader(Stream file)
    {
      Assignment homework = new Assignment();
      List<string> header = new List<string>();

      using (StreamReader reader = new StreamReader(file))
      {
        for (int i = 0; i < 5 && !reader.EndOfStream; ++i)
        {
          header.Add(reader.ReadLine().ToLower());
        }
      }

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
      if (string.IsNullOrEmpty(homework.StudentId) || !Regex.IsMatch(homework.StudentId,@"^[Aa]\d{8}$"))
      {
        homework.ValidHeader = false;
        homework.ErrorMessage = "Header is missing a student ID, or the student ID is invalid. Format should be :'A12345678' (A followed by 8 numbers)";
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

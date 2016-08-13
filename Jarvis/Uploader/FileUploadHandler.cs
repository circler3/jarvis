using Nancy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis
{
  public class FileUploadHandler
  {    
    public Assignment HandleUpload(HttpFile file)
    {
      // Check file header
      Assignment homework = ParseHeader(file);        
      
      if (homework.ValidHeader) 
      {
        string baseDir = Jarvis.Config.AppSettings.Settings["workingDir"].Value;
        string sectionDir = baseDir + "/courses/" + homework.Course.ToLower() + "/hw" + homework.HomeworkId + "/section" + homework.Section;

        // Check that directories exist
        if (Directory.Exists(sectionDir))
        {
          // Upload to correct directory
          homework.Path = sectionDir + homework + "/" + homework.StudentId + "/";
          
          using (FileStream destinationStream = File.Create (homework.Path + "/" + homework.Filename)) 
          {
            file.Value.Position = 0;
            file.Value.CopyTo (destinationStream);
          }          
        }
        else
        {
          homework.ValidHeader = false;
        }
      }

      return homework;
    }

    private Assignment ParseHeader (HttpFile file)
    {
      Assignment homework = new Assignment ();
      List<string> header = new List<string>();
      StreamReader reader = new StreamReader(file.Value);

      for (int i = 0; i < 5 && !reader.EndOfStream; ++i)
      {
        header.Add(reader.ReadLine().ToLower());
      }

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
          homework.Course = s.Split(':')[1].Trim();
        }
        else if (s.Contains("hw#:"))
        {
          homework.Course = s.Split(':')[1].Trim();
        }
      }
        
      if (homework.StudentId != "" && homework.Course != "" && homework.Section != "" && homework.HomeworkId != "")
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

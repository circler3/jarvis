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

        // Check that directories exist
        if (Directory.Exists(baseDir + "/courses/" + homework.Course.ToLower()) && 
            Directory.Exists(baseDir + "/courses/" + homework.Course.ToLower() + "/hw" + homework.HomeworkId))
        {
          // Upload to correct directory
          homework.Path = baseDir + "/courses/" + homework.Course.ToLower() + "/hw" + homework.HomeworkId + "/";
          
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
      for(int i = 0; i < 5; ++i)
      {
        header.Add (reader.ReadLine ());
      }
        
      if (header[1].Contains("A#:") && header[2].Contains("Course:") && header[3].Contains("HW#:"))
      {
        homework.StudentId = header [1].Split (':') [1].Trim();
        homework.Course = header [2].Split (':') [1].Trim();
        homework.HomeworkId = header [3].Split (':') [1].Trim();
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

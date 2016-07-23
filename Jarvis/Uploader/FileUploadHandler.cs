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
    public FileUploadResult HandleUpload(HttpFile file)
    {
      string targetDirectory = CreateTempDirectory();

      using (FileStream destinationStream = File.Create(targetDirectory + "/" + file.Name))
      {
        file.Value.CopyTo(destinationStream);
      }

      // TODO Add some verification 

      return new FileUploadResult()
      {
        FileName = file.Name,
        Path = targetDirectory,
        IsValid = true
      };
    }

    private string CreateTempDirectory()
    {
      var uploadDirectory = Path.GetTempPath() + "jarvisUploads/" + Guid.NewGuid().ToString(); // TODO Make this part of the app.config, probably want to change it per class and assignment?

      if (!Directory.Exists(uploadDirectory))
      {
        Directory.CreateDirectory(uploadDirectory);
      }

      return uploadDirectory;
    }
  }
}

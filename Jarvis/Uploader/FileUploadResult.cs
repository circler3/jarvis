using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis
{
  public class FileUploadResult
  {
    public string FileName { get; set; }
    public string Path { get; set; }
    public bool IsValid { get; set; }

    public string FullName 
    {
      get
      {
        return Path + "/" + FileName;
      }
    }
  }
}

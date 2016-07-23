using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis
{
  public class FileUploadRequest
  {
    public string Title { get; set; }
    public HttpFile File { get; set; }
  }
}

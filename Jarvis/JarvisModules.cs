using Nancy.ModelBinding;

namespace Jarvis
{
  using Nancy;
  using System;

  public class JarvisModules : NancyModule
  {
    private FileUploadHandler uploadHandler = new FileUploadHandler();
    private Grader grader = new Grader();

    public JarvisModules()
    {
      Get["/"] = parameters =>
      {
        return View["index"];
      };

      Get["/results"] = x => // TODO Make this return all of a students past results
      {
        //var model = new Index() { Name = "Boss Hawg" };

        //var file = Request.Files.GetEnumerator().Current;
        //string fileDetails = "None";

        //if (file != null)
        //{
//          fileDetails = string.Format("{0} ({1}) {2}bytes", file.Name, file.ContentType, file.Value.Length);
        //}

        //model.Posted = fileDetails;

        return View["upload"];
      };

      
      Post["/results"] = _ =>
      {
        GradingResult result = null;

        var request = this.Bind<FileUploadRequest>();
        var assignment = uploadHandler.HandleUpload(request.File);

        if (assignment.ValidHeader)
        {
         // Run grader
          result = grader.Grade(assignment);
          result.ValidHeader = true;
        }
        else
        {
          result = new GradingResult();
          result.ValidHeader = false;
        }

        //var response = new FileUploadResponse() { Identifier = uploadResult.Identifier };
        if (result.ValidHeader)
        {
         return View["results", result];
        }
        else
        {
          return View["error"];
        }
      };

      // Need to provide a way to close an assignment and get MOSS report
      // MOSS - To be written to a file
    }    
  }
}
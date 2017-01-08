using System;
using System.Collections.Generic;
using System.Text;

namespace Jarvis
{
  public class TestCase
  {
    private bool passed = true;
    public enum Type
    {
      Text,
      PPM
    }

    public TimeSpan Duration { get; set; }
    public int Id { get; set; }
    public string StdInputFile { get; set; }
    public string StdOutputFile { get; set; }
    public string StdInText { get; set; }
    public string StdOutText { get; set; }

    public List<InputFile> FileInputFiles { get; set; }
    public List<OutputFile> FileOutputFiles { get; set; }
    public List<string> ProvidedSourceFiles { get; set; }

    public bool Passed
    { 
      get
      {
        return passed;
      }

      set
      {
        if (passed) // Once we've failed, don't let this be updated
        {
          passed = value;
        }
      }
    }

    public List<IViewer> Viewers { get; set; }

    public string TestsPath { get; private set; }
    public string HomeworkPath { get; private set; }

    public string GetResults(string outputPath, string testsPath)
    { 
      TestsPath = testsPath;
      HomeworkPath = outputPath;

      // Run output first so test.Passed is set properly
      StringBuilder output = new StringBuilder();
      foreach (IViewer viewer in Viewers)
      {
        output.Append(viewer.ToHtml(this) + "<br />");
      }

      var passedText = Passed ? "<span style=\"color:#00ff00\">Passed</span>" : "<span style=\"color:#ff0000\">Failed</span>";
      StringBuilder result = new StringBuilder();

      result.Append("<p style='display: inline;'>------------------------------------------------------------------</p>");
      result.AppendFormat("<h3 style='margin-top: 0px; margin-bottom: 0px;'>Test case {0}: {1} ({2} seconds)</h3>",  Id, passedText, Duration.TotalSeconds);
      result.Append("<p style='display: inline;'>------------------------------------------------------------------</p>");
      result.Append("<br />");
      result.Append(output.ToString());

      return result.ToString();
    }

    public TestCase(int id)
    {
      Id = id;

      Passed = true;
      FileInputFiles = new List<InputFile>();
      FileOutputFiles = new List<OutputFile>();
      ProvidedSourceFiles = new List<string>();
      Viewers = new List<IViewer>();
    }
  }
}

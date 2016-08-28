using NUnit.Framework;
using System;
using Jarvis;
using System.Collections.Generic;

namespace JarvisTests
{
  [TestFixture()]
  public class CanvasFormatterTests
  {
    [Test()]
    public void ReadCsv()
    {
      CanvasFormatter canvas = new CanvasFormatter();
      string path = "data/canvas.csv";

      Assignment assignment = new Assignment();
      assignment.HomeworkId = "2";
      assignment.Course = "cs1400";
      assignment.ValidHeader = true;
      assignment.Section = "1";
      assignment.StudentId = "A02233814";

      GradingResult result = new GradingResult(assignment);      

      List<GradingResult> results = new List<GradingResult>();
      results.Add(result);

      canvas.GenerateCanvasCsv(path, "5", results);
    }
  }
}


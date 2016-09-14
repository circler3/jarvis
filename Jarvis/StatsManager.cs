using System;
using System.Collections.Generic;
using CsvHelper;
using System.IO;
using System.Xml;

namespace Jarvis
{
  public class AssignmentStats
  {
    public string Name { get; set; }
    public int TotalSubmissions { get; set; }
    public int TotalNonCompile { get; set; }
    public int TotalBadStyle { get; set; }
    public Dictionary<string, string> TotalUniqueStudentsSubmissions { get; set; }

    public float AverageScore 
    {
      get
      {
        float total = 0;

        foreach (string grade in TotalUniqueStudentsSubmissions.Values)
        {
          total += float.Parse(grade);
        }

        return total / (float) TotalUniqueStudentsSubmissions.Count;
      }
    }

    public int TotalFullCredit 
    { 
      get
      {
        int total = 0;

        foreach (string grade in TotalUniqueStudentsSubmissions.Values)
        {
          if (grade == "10")
          {
            total++;
          }
        }

        return total;
      }
    }

    public int GetUniqueSubmissionCount 
    {
      get
      {
        return TotalUniqueStudentsSubmissions.Count;
      }
    }

    public AssignmentStats()
    {
      TotalUniqueStudentsSubmissions = new Dictionary<string, string>();
    }      
  }

  public class StatsManager
  {
    public int TotalBadHeaders { get; set; }
    public int TotalFilesProcessed { get; set; }    
    public Dictionary<string, AssignmentStats> AssignmentData { get; set; }

    public List<AssignmentStats> HomeworkStats
    {
      get
      {
        List<AssignmentStats> stats = new List<AssignmentStats>();

        foreach(AssignmentStats stat in AssignmentData.Values)
        {
          stats.Add(stat);
        }

        return stats;
      }
    }

    public StatsManager()
    {
      AssignmentData = new Dictionary<string, AssignmentStats>();
    }

    public void ReadStats()
    {        
      string statsPath = string.Format("{0}/stats.xml", Jarvis.Config.AppSettings.Settings["workingDir"].Value);

      if (File.Exists(statsPath))
      {
        string currentAssignmentKey = string.Empty;
        FileStream stream = File.OpenRead(statsPath);

        using (XmlReader reader = XmlReader.Create(stream))
        {
          while (reader.Read())
          {
            switch (reader.NodeType)
            {
              case XmlNodeType.Element:                
                switch (reader.Name)
                {
                  case "JarvisStats":
                    TotalBadHeaders = int.Parse(reader.GetAttribute("TotalBadHeaders"));
                    TotalFilesProcessed = int.Parse(reader.GetAttribute("TotalFilesProcessed"));
                    break;  

                  case "Assignment":
                    AssignmentStats stats = new AssignmentStats();
                    stats.Name = reader.GetAttribute("Name");
                    stats.TotalSubmissions = int.Parse(reader.GetAttribute("TotalSubmissions"));

                    if (!string.IsNullOrEmpty(reader.GetAttribute("TotalNonCompile")))
                    {
                      stats.TotalNonCompile = int.Parse(reader.GetAttribute("TotalNonCompile"));
                      stats.TotalBadStyle = int.Parse(reader.GetAttribute("TotalBadStyle"));
                    }
                      
                    AssignmentData.Add(stats.Name, stats);

                    currentAssignmentKey = stats.Name;
                    break;

                  case "Submission":
                    string studentId = reader.GetAttribute("Student");
                    string grade = reader.GetAttribute("Grade");
                    if (AssignmentData.ContainsKey(currentAssignmentKey))
                    {
                      AssignmentData[currentAssignmentKey].TotalUniqueStudentsSubmissions.Add(studentId, grade);
                    }
                    break;
                }
                break;
            }
          }
        }

        stream.Close();
        stream.Dispose();
      }
    }

    public void WriteStats()
    { 
      string statsPath = string.Format("{0}/stats.xml", Jarvis.Config.AppSettings.Settings["workingDir"].Value);

      FileStream stream = File.Create(statsPath);

      using (XmlWriter writer = XmlWriter.Create(stream))
      {
        writer.WriteStartDocument();
        writer.WriteStartElement("JarvisStats");
        writer.WriteAttributeString("TotalBadHeaders", TotalBadHeaders.ToString());
        writer.WriteAttributeString("TotalFilesProcessed", TotalFilesProcessed.ToString());

        foreach (string key in AssignmentData.Keys)
        {        
          writer.WriteStartElement("Assignment");

          writer.WriteAttributeString("Name", AssignmentData[key].Name);
          writer.WriteAttributeString("TotalSubmissions", AssignmentData[key].TotalSubmissions.ToString());
          writer.WriteAttributeString("TotalNonCompile", AssignmentData[key].TotalNonCompile.ToString());
          writer.WriteAttributeString("TotalBadStyle", AssignmentData[key].TotalBadStyle.ToString());

          foreach (string student in AssignmentData[key].TotalUniqueStudentsSubmissions.Keys)
          {
            writer.WriteStartElement("Submission");
            writer.WriteAttributeString("Student", student);
            writer.WriteAttributeString("Grade", AssignmentData[key].TotalUniqueStudentsSubmissions[student]);

            writer.WriteEndElement();
          }

          writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.Flush();
        writer.Close();
        writer.Dispose();
      }
      stream.Flush();
      stream.Close();
      stream.Dispose();
    }
  }
}


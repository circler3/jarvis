using System;
using System.Collections.Generic;
using CsvHelper;
using System.IO;
using System.Xml;

namespace Jarvis
{
  public class AssignmentStats
  {
    public int TotalSubmissions { get; set; }
    public int TotalFullCredit { get; set; }
    public Dictionary<string, string> TotalUniqueStudentsSubmissions { get; set; }

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

    public StatsManager()
    {
      AssignmentData = new Dictionary<string, AssignmentStats>();
    }

    public void ReadStats()
    {
      string statsPath = string.Format("{0}/stats.xml", Jarvis.Config.AppSettings.Settings["workingDir"].Value);
      string currentAssignmentKey = string.Empty;
      FileStream stream = File.OpenRead(statsPath);

      using (XmlReader reader = XmlReader.Create(stream))
      {
        while (reader.Read())
        {
          switch (reader.NodeType)
          {
            case XmlNodeType.Element:                
              switch(reader.Name)
              {
                case "JarvisStats":
                  TotalBadHeaders = int.Parse(reader.GetAttribute("TotalBadHeaders"));
                  TotalFilesProcessed = int.Parse(reader.GetAttribute("TotalFilesProcessed"));
                  break;  

                case "Assignment":
                  AssignmentStats stats = new AssignmentStats();
                  currentAssignmentKey = reader.GetAttribute("Key");
                  stats.TotalSubmissions = int.Parse(reader.GetAttribute("TotalSubmissions"));
                  stats.TotalFullCredit = int.Parse(reader.GetAttribute("TotalFullCredit"));

                  AssignmentData.Add(currentAssignmentKey, stats);
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

    public void WriteStats()
    { 
      string statsPath = string.Format("{0}/stats.xml", Jarvis.Config.AppSettings.Settings["workingDir"].Value);

      FileStream stream = File.OpenWrite(statsPath);

      using (XmlWriter writer = XmlWriter.Create(stream))
      {
        writer.WriteStartDocument();
        writer.WriteStartElement("JarvisStats");
        writer.WriteAttributeString("TotalBadHeaders", TotalBadHeaders.ToString());
        writer.WriteAttributeString("TotalFilesProcessed", TotalFilesProcessed.ToString());

        foreach (string key in AssignmentData.Keys)
        {        
          writer.WriteStartElement("Assignment");

          writer.WriteAttributeString("Key", key);
          writer.WriteAttributeString("TotalSubmissions", AssignmentData[key].TotalSubmissions.ToString());
          writer.WriteAttributeString("TotalFullCredit", AssignmentData[key].TotalFullCredit.ToString());

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


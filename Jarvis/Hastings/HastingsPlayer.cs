using System;
using System.Collections.Generic;
using System.IO;

namespace Jarvis
{
  public class HastingsPlayer
  {
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
    public int Uploads { get; set; }
    public string Name { get; set; }
    public string PlayerDirectory { get; set; }

    public string DisplayName
    {
      get
      {
        return string.Format("{0} [{1}/{2}/{3}] (v{4})", Name, Wins, Losses, Draws, Uploads);
      }
    }

    public HastingsPlayer()
    {
      // empty
    }

    public HastingsPlayer(string header)
    {      
      PlayerDirectory = Path.GetDirectoryName(header);
      // read Name from header file
      Name = GetPlayerNameFromHeader(header);
    }

    private string GetPlayerNameFromHeader(string header)
    {
      string name = string.Empty;

      IEnumerable<string> lines = File.ReadLines(header);

      foreach (string line in lines)
      {
        if (line.Contains(": public Unit"))
        {
          string[] parts = line.Split(' ');

          for (int i = 0; i < parts.Length; ++i)
          {
            if (parts[i] == "class")
            {
              name = parts[i + 1];
              break;
            }
          }

          break;
        }
      }

      return name;
    }
  }
}


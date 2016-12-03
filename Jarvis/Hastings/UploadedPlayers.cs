using System;
using System.Collections.Generic;

namespace Jarvis
{
  public class UploadedPlayers
  {
    public List<string> Players { get; set; }

    public UploadedPlayers()
    {
      
      Players = new List<string>();
      Players.Add("Bob");
      Players.Add("Tom");
      Players.Add("Jay");
      Players.Add("Jak");
      Players.Add("Bryan");
    }

  }
}


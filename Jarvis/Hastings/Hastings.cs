using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Jarvis
{
  public static class Hastings
  {
    private static Dictionary<string, HastingsPlayer> players = new Dictionary<string, HastingsPlayer>();
    private static List<string> hastingsFiles = new List<string>();
    private static List<string> playerFiles = new List<string>();
    private static string hastingsDir = Jarvis.Config.AppSettings.Settings["workingDir"].Value + "/courses/cs1400/hastings/";

    static Hastings()
    {
      Directory.CreateDirectory(hastingsDir + "code/players"); // Create all the directories

      LoadPlayers();

      string[] files = Directory.GetFiles(hastingsDir + "code");
      foreach (string file in files)
      {
        hastingsFiles.Add(file);
      }
    }

    public static List<HastingsPlayer> GetPlayers()
    {
      List<HastingsPlayer> playerList = new List<HastingsPlayer>();

      foreach (HastingsPlayer player in players.Values)
      {
        playerList.Add(player);
      }

      return playerList;
    }

    public static string BuildHastings(Assignment assignment)
    {
      string result = string.Empty;

      lock (players)
      {
        // find player that uploaded new code
        HastingsPlayer newPlayer = FindPlayer(assignment);

        if (newPlayer.Name.ToLower() == "ultron")
        {
          result = "<p>Ultron was defeated by Vision. You must change your class name!</p>";
        }
        else if (newPlayer != null)
        {
          // Copy all code to tmp directory
          List<string> sourceFiles = new List<string>();
          List<string> includeDirs = new List<string>();

          includeDirs.Add(assignment.Path);
          includeDirs.Add(hastingsDir + "code/");
          includeDirs.Add(hastingsDir + "code/players/");

          foreach (string file in hastingsFiles)
          {
            if (file.Contains(".cpp"))
            {
              sourceFiles.Add(file);
            }
          }

          foreach (string file in playerFiles)
          {
            if (file.Contains(".cpp"))
            {
              sourceFiles.Add(file);
            }
          }

          foreach (string file in assignment.FileNames)
          {
            if (file.Contains(".cpp"))
            {
              sourceFiles.Add(assignment.Path + file);
            }
          }

          // Compile all *.cpp in tmp directory
          string compileResult = Compiler.CompileCpp("hastings", assignment.Path, includeDirs, sourceFiles);
          if (compileResult == "Success!!")
          {
            // delete win loss record for player
            newPlayer.Wins = 0;
            newPlayer.Losses = 0;
            newPlayer.Draws = 0;
            newPlayer.Uploads++;

            SavePlayers();

            // if successful, copy code to hastings directory
            foreach (string file in assignment.FileNames)
            {
              if (File.Exists(hastingsDir + "code/players/" + file))
              {
                File.Delete(hastingsDir + "code/players/" + file);
              }

              File.Copy(assignment.Path + file, hastingsDir + "code/players/" + file);
            }

            LoadPlayers();

            // rebuild hastings binary
            sourceFiles.Clear();
            includeDirs.Clear();
            includeDirs.Add(hastingsDir + "code/");

            foreach (string file in hastingsFiles)
            {
              if (file.Contains(".cpp"))
              {
                sourceFiles.Add(file);
              }
            }

            foreach (string file in playerFiles)
            {
              if (file.Contains(".cpp"))
              {
                sourceFiles.Add(file);
              }
            }

            string compileOutput = Compiler.CompileCpp(hastingsDir + "hastings", hastingsDir, includeDirs, sourceFiles);

            Logger.Trace("Rebuild hastings: {0}", compileOutput);
          }
          else
          {
            result = compileResult;
          }
        }
        else
        {
          result = "Unknown player";
        }
      }

      return result;
    }

    public static string DoBattle(string player1Name, string player2Name)
    {
      string battleOutput = string.Empty;

      lock (players)
      {        
        using (Process executionProcess = new Process())
        {
          executionProcess.StartInfo.WorkingDirectory = hastingsDir;
          executionProcess.StartInfo.UseShellExecute = false;
          executionProcess.StartInfo.RedirectStandardOutput = true;
          executionProcess.StartInfo.RedirectStandardError = true;
          executionProcess.StartInfo.RedirectStandardInput = true;

          if (!File.Exists(hastingsDir + "hastings"))
          {
            Logger.Fatal("Executable " + hastingsDir + "hastings did not exist!!");
            battleOutput = "[Jarvis could not find the executable!]";
          }
          else
          {
            DateTime startTime = DateTime.Now;
            DateTime finishTime;

            executionProcess.StartInfo.FileName = hastingsDir + "hastings";
            executionProcess.Start();

            try
            {
              executionProcess.StandardInput.AutoFlush = true;
             
              executionProcess.StandardInput.Write(player1Name + "\n");
              executionProcess.StandardInput.Write(player2Name + "\n");
             
              Jarvis.StudentProcesses.Add(executionProcess.Id);

              battleOutput = executionProcess.StandardOutput.ReadToEnd();
              executionProcess.WaitForExit(10000);

              finishTime = DateTime.Now;

              //homework.Duration = finishTime - startTime;

              if (!executionProcess.HasExited)
              {
                executionProcess.Kill();
                battleOutput += "\n[Unresponsive program terminated by Jarvis]\n";
              }
            }
            catch (Exception e)
            {
              Logger.Fatal("Fatal exception while running program");
              Logger.Fatal(e.ToString());
            }

            executionProcess.Close();
          }
        }

        // Find winner
        // something something the one true crown
        Regex regex = new Regex("The one true crown! (.*) wins.");

        Match match = regex.Match(battleOutput);

        if (match.Groups.Count >= 1)
        {
          string winner = match.Groups[1].Value;
          string loser = string.Empty;
          if (player1Name != winner)
          {
            loser = player1Name;
          }
          else
          {
            loser = player2Name;
          }

          try
          {
          // update player stats
          players[winner].Wins++;
          players[loser].Losses++;
          }
          catch (Exception e)
          {
            Console.WriteLine(e.ToString());
          }
        }
        else
        {
          players[player1Name].Draws++;
          players[player2Name].Draws++;
        }

        // save player data
        SavePlayers();
      }

      HastingsViewer viewer = new HastingsViewer();

      return viewer.ToHtml(player1Name, player2Name, battleOutput);
    }

    private static void LoadPlayers()
    {
      lock (players)
      {
        if (players.Count > 0)
        {
          players.Clear();
        }

        string playersCodePath = hastingsDir + "code/players";

        string[] records = Directory.GetFiles(playersCodePath, "*.record", SearchOption.AllDirectories);

        foreach (string record in records)
        {
          HastingsPlayer player = new HastingsPlayer();

          using(StreamReader reader = new StreamReader(record))
          {
            player.Name = reader.ReadLine();
            player.Wins = int.Parse(reader.ReadLine());
            player.Losses = int.Parse(reader.ReadLine());
            player.Draws = int.Parse(reader.ReadLine());
            player.Uploads = int.Parse(reader.ReadLine());

            reader.Close();
          }

          if (!players.ContainsKey(player.Name))
          {
            players.Add(player.Name, player);
          }
        }

        // Load player code
        playerFiles.Clear();

        string[] files = Directory.GetFiles(hastingsDir + "code/players");
        foreach (string file in files)
        {
          playerFiles.Add(file);
        }
      }
    }

    private static void SavePlayers()
    {
      lock (players)
      {
        foreach (HastingsPlayer player in players.Values)
        {
          using (StreamWriter writer = new StreamWriter(hastingsDir + "code/players/" + player.Name + ".record", false))
          {
            writer.WriteLine(player.Name);
            writer.WriteLine(player.Wins);
            writer.WriteLine(player.Losses);
            writer.WriteLine(player.Draws);
            writer.WriteLine(player.Uploads);
            writer.WriteLine(player.PlayerDirectory);

            writer.Close();
          }
        }
      }
    }

    private static HastingsPlayer FindPlayer(Assignment assignment)
    {
      HastingsPlayer player = null;

      foreach (string file in assignment.FileNames)
      {
        if (file.Contains(".h"))
        {
          player = new HastingsPlayer(assignment.Path + file);

          if (players.ContainsKey(player.Name))
          {
            player = players[player.Name];
          }
        }
      }

      return player;
    }
  }
}


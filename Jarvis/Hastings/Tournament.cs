using System;
using System.Collections.Generic;

namespace Jarvis
{
	public class Tournament
	{
		public List<HastingsPlayer> Contestants { get; set; }
		public List<Table> Tables { get; set; }
		public int Rounds { get; set; }
		public int currentRound { get; set; }
		private static Random rng = new Random();

		public Tournament(List<HastingsPlayer> HastingsPlayers)
		{
			currentRound = 0;
			Contestants = HastingsPlayers;
			if (HastingsPlayers.Count <= 8)
			{
				Rounds = 3;
			}
			else if (HastingsPlayers.Count > 8 && HastingsPlayers.Count <= 16)
			{
				Rounds = 4;
			}
			else if (HastingsPlayers.Count >= 17 && HastingsPlayers.Count <= 32)
			{
				Rounds = 5;
			}
			else if (HastingsPlayers.Count >= 33 && HastingsPlayers.Count <= 64)
			{
				Rounds = 6;
			}
			else if (HastingsPlayers.Count >= 65 && HastingsPlayers.Count <= 128)
			{
				Rounds = 7;
			}
			else if (HastingsPlayers.Count >= 129 && HastingsPlayers.Count <= 226)
			{
				Rounds = 8;
			}
			else if (HastingsPlayers.Count >= 227 && HastingsPlayers.Count <= 409)
			{
				Rounds = 9;
			}
			else
			{
				Rounds = 10;
			}
			Tables = new List<Table>();
		}

		public void groupTables()
		{
			currentRound++;
			if (currentRound > Rounds)
			{
				return;
			}
			List<HastingsPlayer> addToTables = new List<HastingsPlayer>();
			if (currentRound == 1)
			{
				foreach (HastingsPlayer HastingsPlayer in Contestants)
				{
					HastingsPlayer.hasTable = false;
					HastingsPlayer.GamesPlayed = 0;
					HastingsPlayer.Byes = 0;
					HastingsPlayer.GamePoints = 0;
					HastingsPlayer.GamesPlayed = 0;
					HastingsPlayer.GameWinPercentage = 0.0f;
					HastingsPlayer.MatchPoints = 0;
					HastingsPlayer.MatchWinPercentage = 0.0f;
					HastingsPlayer.Opponents.Clear();
					HastingsPlayer.OpponentsGameWinPercentage = 0.0f;
					HastingsPlayer.OpponentsMatchWinPercentage = 0.0f;
				}
				Tables.Clear();
				Shuffle(Contestants);
				for (int i = 0; i < Contestants.Count; i += 2)
				{
					if (Contestants.Count % 2 != 0 && i == Contestants.Count - 1 && !Contestants[i].hasTable)
					{
						Tables.Add(new Table(Contestants[i], new HastingsPlayer()));
						Contestants[i].hasTable = true;
						Contestants[i].Byes += 1;
					}
					else
					{
						Tables.Add(new Table(Contestants[i], Contestants[i + 1]));
						Contestants[i].Opponents.Add(Contestants[i + 1]);
						Contestants[i].hasTable = true;
						Contestants[i + 1].Opponents.Add(Contestants[i]);
						Contestants[i + 1].hasTable = true;
					}
				}
			}
			else
			{
				// This is for every round past the first. At this point, you need
				// to organize everyone based off of match points.
				// and make sure that no one plays the same person
				// that they've played before.
				List<int> possiblePoints = new List<int>();
				List<List<HastingsPlayer>> groups = new List<List<HastingsPlayer>>();

				foreach (var person in Contestants)
				{
					if (!possiblePoints.Contains(person.MatchPoints))
					{
						possiblePoints.Add(person.MatchPoints);
					}
				}

				possiblePoints.Sort();

				foreach (int possible in possiblePoints)
				{
					groups.Add(new List<HastingsPlayer>());
				}

				int i = 0;
				foreach (int possible in possiblePoints)
				{
					foreach (var people in Contestants)
					{
						if (people.MatchPoints == possible)
						{
							groups[i].Add(people);
						}
					}
					i++;
				}

				foreach (List<HastingsPlayer> HastingsPlayers in groups)
				{
					Shuffle(HastingsPlayers);
				}

				List<HastingsPlayer> allHastingsPlayers = new List<HastingsPlayer>();
				foreach (List<HastingsPlayer> HastingsPlayers in groups)
				{
					foreach (HastingsPlayer playa in HastingsPlayers)
					{
						allHastingsPlayers.Add(playa);
					}
				}

				if (Tables.Count != 0)
				{
					Tables.Clear();
				}



				for (i = allHastingsPlayers.Count - 1; i >= 0; i--)
				{
					if (addToTables.Count != allHastingsPlayers.Count - 1)
					{
						for (int j = allHastingsPlayers.Count - 1; j >= 0; j--)
						{
							if (allHastingsPlayers[i] == allHastingsPlayers[j])
							{
								continue;
							}
							else if (!allHastingsPlayers[i].Opponents.Contains(allHastingsPlayers[j])
								&& !allHastingsPlayers[j].Opponents.Contains(allHastingsPlayers[i])
								&& !allHastingsPlayers[i].hasTable
								&& !allHastingsPlayers[j].hasTable)
							{
								addToTables.Add(allHastingsPlayers[i]);
								addToTables.Add(allHastingsPlayers[j]);
								allHastingsPlayers[i].hasTable = true;
								allHastingsPlayers[j].hasTable = true;

								if (Contestants.Contains(allHastingsPlayers[i]))
								{
									Contestants[Contestants.IndexOf(allHastingsPlayers[i])].Opponents.Add(allHastingsPlayers[j]);
								}
								if (Contestants.Contains(allHastingsPlayers[j]))
								{
									Contestants[Contestants.IndexOf(allHastingsPlayers[j])].Opponents.Add(allHastingsPlayers[i]);
								}
								break;
							}
						}
					}
					else if (addToTables.Count == Contestants.Count - 1 && !allHastingsPlayers[i].hasTable)
					{
						addToTables.Add(allHastingsPlayers[i]);
						addToTables.Add(new HastingsPlayer());
						Contestants[Contestants.IndexOf(allHastingsPlayers[i])].Byes += 1;
						break;
					}
				}
			}

			for (int j = 0; j < addToTables.Count; j += 2)
			{
				Tables.Add(new Table(addToTables[j], addToTables[j + 1]));
			}

			int tableNumber = 1;
			foreach (Table table in Tables)
			{
				table.tableNumber = tableNumber++;
			}
		}

		public void AssignWinsToHastingsPlayer(Tournament myTournament, int winnerIndex, int loserIndex, int matchPoints, int winnerGamePoints, int loserGamePoints)
		{
			if (winnerIndex != -1)
			{
				myTournament.Contestants[winnerIndex].GamePoints += winnerGamePoints;
				myTournament.Contestants[winnerIndex].MatchPoints += matchPoints;
				myTournament.Contestants[winnerIndex].GamesPlayed += 2;
				if (loserGamePoints == 3 || loserGamePoints == 4)
				{
					myTournament.Contestants[winnerIndex].GamesPlayed += 1;
				}
				myTournament.Contestants[winnerIndex].hasTable = false;
			}
			if (loserIndex != -1)
			{
				if (matchPoints == 1)
				{
					myTournament.Contestants[loserIndex].MatchPoints += matchPoints;
				}
				myTournament.Contestants[loserIndex].GamesPlayed += 2;
				if (loserGamePoints == 3 || loserGamePoints == 4)
				{
					myTournament.Contestants[loserIndex].GamesPlayed += 1;
				}
				myTournament.Contestants[loserIndex].GamePoints += loserGamePoints;
				myTournament.Contestants[loserIndex].hasTable = false;
			}
		}

		private static void Shuffle<T>(IList<T> list)
		{
			int n = list.Count;

			while (n > 1)
			{
				n--;
				int k = rng.Next(n + 1);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}

		public List<HastingsPlayer> CalculateTieBreaks()
		{
			// Start all calculations
			CalculateTieBreaks tieBreakers = new CalculateTieBreaks();
			tieBreakers.CalcMatchPointAverage(this);
			tieBreakers.CalcOppMatchPointAverage(this);
			tieBreakers.CalcGamePointAverage(this);
			tieBreakers.CalcOppGamePointAverage(this);
			// Then start forming groups based off match points
			List<HastingsPlayer> standings = tieBreakers.ComputeStandings(this);

			for (int i = 1; i <= standings.Count; i++)
			{
				standings[i - 1].Rank = i;
			}

			return standings;
		}
	}
}

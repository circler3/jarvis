using System.Collections.Generic;

namespace Jarvis
{
	public class CalculateTieBreaks
	{
		public void CalcMatchPointAverage(Tournament myTournament)
		{
			foreach (HastingsPlayer player in myTournament.Contestants)
			{
				int totalRoundsPlayed = 0;

				totalRoundsPlayed = player.Byes + player.Opponents.Count;

				player.MatchWinPercentage = player.MatchPoints / (float)(totalRoundsPlayed * 3);
			}
		}

		public void CalcOppMatchPointAverage(Tournament myTournament)
		{
			float runningTotal = 0.0f;
			foreach (HastingsPlayer player in myTournament.Contestants)
			{
				foreach (HastingsPlayer opp in player.Opponents)
				{
					if (opp.MatchWinPercentage >= .33f)
					{
						runningTotal += opp.MatchWinPercentage;
					}
					else
					{
						runningTotal += .33f;
					}
				}
				player.OpponentsMatchWinPercentage = runningTotal / player.Opponents.Count;
				runningTotal = 0.0f;
			}
		}

		public void CalcGamePointAverage(Tournament myTournament)
		{
			foreach (HastingsPlayer player in myTournament.Contestants)
			{
				player.GameWinPercentage = (float)player.GamePoints / (player.GamesPlayed * 3);
			}
		}

		public void CalcOppGamePointAverage(Tournament myTournament)
		{
			float runningTotal = 0.0f;
			foreach (HastingsPlayer player in myTournament.Contestants)
			{
				foreach (HastingsPlayer opp in player.Opponents)
				{
					if (opp.GameWinPercentage >= .33f)
					{
						runningTotal += opp.GameWinPercentage;
					}
					else
					{
						runningTotal += .33f;
					}
				}
				player.OpponentsGameWinPercentage = runningTotal / player.Opponents.Count;
				runningTotal = 0.0f;
			}
		}

		public List<HastingsPlayer> ComputeStandings(Tournament myTournament)
		{
			List<int> possibleMatchPoints = new List<int>();

			foreach (HastingsPlayer player in myTournament.Contestants)
			{
				if (!possibleMatchPoints.Contains(player.MatchPoints))
				{
					possibleMatchPoints.Add(player.MatchPoints);
				}
			}

			List<List<HastingsPlayer>> groups = new List<List<HastingsPlayer>>();

			possibleMatchPoints.Sort();
			possibleMatchPoints.Reverse();

			foreach (int possible in possibleMatchPoints)
			{
				groups.Add(new List<HastingsPlayer>());
			}

			int i = 0;
			foreach (int possible in possibleMatchPoints)
			{
				foreach (var people in myTournament.Contestants)
				{
					if (people.MatchPoints == possible)
					{
						groups[i].Add(people);
					}
				}
				i++;
			}

			List<HastingsPlayer> standings = new List<HastingsPlayer>();

			foreach (List<HastingsPlayer> myGroup in groups)
			{
				if (myGroup.Count == 1)
				{
					// only one person in the group.
					// add them to the standings.
					standings.Add(myGroup[0]);
				}
				else
				{
					// this means that there are people who have the same number
					// of match points and must be compared based off of their
					// opponents Match Win Percentage
					myGroup.Sort((x, y) => x.OpponentsMatchWinPercentage.CompareTo(y.OpponentsMatchWinPercentage));
					myGroup.Reverse();

					List<float> possibleMWP = new List<float>();

					foreach (HastingsPlayer player in myGroup)
					{
						if (!possibleMWP.Contains(player.OpponentsMatchWinPercentage))
						{
							possibleMWP.Add(player.OpponentsMatchWinPercentage);
						}
					}

					if (possibleMWP.Count == myGroup.Count)
					{
						// this means that everyone in the group is sorted based on 
						// Opponents Match Win Percentage

						foreach (HastingsPlayer player in myGroup)
						{
							standings.Add(player);
						}
					}
					else
					{
						// this means that we have to compare based off of
						// Game Win Percentages

						List<List<HastingsPlayer>> groupsOMWP = new List<List<HastingsPlayer>>();

						List<float> possibleOMWP = new List<float>();

						foreach (HastingsPlayer player in myGroup)
						{
							if (!possibleOMWP.Contains(player.OpponentsMatchWinPercentage))
							{
								possibleOMWP.Add(player.OpponentsMatchWinPercentage);
							}
						}

						foreach (float impossible in possibleOMWP)
						{
							groupsOMWP.Add(new List<HastingsPlayer>());
						}

						int j = 0;
						foreach (float impossible in possibleOMWP)
						{
							foreach (HastingsPlayer person in myGroup)
							{
								if (person.OpponentsMatchWinPercentage == impossible)
								{
									groupsOMWP[j].Add(person);
								}
							}
							j++;
						}

						foreach (List<HastingsPlayer> subgroup in groupsOMWP)
						{
							if (subgroup.Count == 1)
							{
								standings.Add(subgroup[0]);
							}
							else
							{
								subgroup.Sort((x, y) => x.GameWinPercentage.CompareTo(y.GameWinPercentage));
								subgroup.Reverse();

								List<float> possibleGWP = new List<float>();

								foreach (HastingsPlayer playerGWP in subgroup)
								{
									if (!possibleGWP.Contains(playerGWP.GameWinPercentage))
									{
										possibleGWP.Add(playerGWP.GameWinPercentage);
									}
								}

								if (possibleGWP.Count == subgroup.Count)
								{
									// add these people to the standings

									foreach (HastingsPlayer person in subgroup)
									{
										standings.Add(person);
									}

								}
								else
								{
									// organize based off of 
									// Opponents Game Win Percentage

									List<List<HastingsPlayer>> groupsGWP = new List<List<HastingsPlayer>>();

									List<float> possibleOGWP = new List<float>();

									foreach (HastingsPlayer player in subgroup)
									{
										if (!possibleOGWP.Contains(player.GameWinPercentage))
										{
											possibleOGWP.Add(player.GameWinPercentage);
										}
									}

									foreach (float ohWell in possibleOGWP)
									{
										groupsGWP.Add(new List<HastingsPlayer>());
									}

									int k = 0;
									foreach (float ohWell in possibleOGWP)
									{
										foreach (HastingsPlayer person in subgroup)
										{
											if (person.GameWinPercentage == ohWell)
											{
												groupsGWP[k].Add(person);
											}
										}
										k++;
									}

									// this could be where people are getting thrown out of the standings
									foreach (List<HastingsPlayer> subSubGroup in groupsGWP)
									{
										if (subSubGroup.Count == 1)
										{
											standings.Add(subSubGroup[0]);
										}
										else
										{
											subSubGroup.Sort((x, y) => x.OpponentsGameWinPercentage.CompareTo(y.OpponentsGameWinPercentage));
											subSubGroup.Reverse();

											foreach (HastingsPlayer player in subSubGroup)
											{
												standings.Add(player);
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return standings;
		}
	}
}

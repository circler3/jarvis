namespace Jarvis
{
	public class Table
	{
		public HastingsPlayer firstPlayer { get; set; }
		public HastingsPlayer secondPlayer { get; set; }
		public int tableNumber { get; set; }

		public Table(HastingsPlayer PlayerOne, HastingsPlayer PlayerTwo)
		{
			firstPlayer = PlayerOne;
			secondPlayer = PlayerTwo;
		}
	}
}

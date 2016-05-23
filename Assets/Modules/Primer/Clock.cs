using System;
using System.Diagnostics;

namespace Primer
{
	public static class Clock
	{
		public static readonly DateTime UTC = new DateTime(1970, 1, 1, 0, 0, 0, 0);
		private static readonly Stopwatch sinceStartup = new Stopwatch();
		private static long timeStartup;

		public static long Now
		{
			get { return timeStartup + sinceStartup.ElapsedMilliseconds * 1000; }
		}

		public static long Elapsed
		{
			get { return sinceStartup.ElapsedMilliseconds * 1000; }
		}

		public static void Initialize()
		{
			if (!sinceStartup.IsRunning)
			{
				sinceStartup.Start();
				TimeSpan ts = DateTime.UtcNow - UTC;
				timeStartup = (long)(ts.TotalMilliseconds * 1000);
			}
		}
	}
}
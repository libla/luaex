using System;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Primer
{
	public static class Clock
	{
		public static readonly DateTime UTC = new DateTime(1970, 1, 1, 0, 0, 0, 0);
		private static readonly Stopwatch sinceStartup = new Stopwatch();
		private static long timeStartup;

		public static void Initialize()
		{
			sinceStartup.Start();
			TimeSpan ts = DateTime.UtcNow - UTC;
			timeStartup = (long)(ts.TotalMilliseconds * 1000);
		}

		public static long Now
		{
			get { return timeStartup + sinceStartup.ElapsedMilliseconds * 1000; }
		}

		public static long Elapse
		{
			get { return sinceStartup.ElapsedMilliseconds * 1000; }
		}
	}
}
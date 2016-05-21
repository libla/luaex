using System;

namespace Primer
{
	public static class Plugin
	{
		public static void Init()
		{
			Loop.Initialize();
			Clock.Initialize();
		}

		public static void Exit()
		{
			NetManager.ExitAll();
		}
	}
}
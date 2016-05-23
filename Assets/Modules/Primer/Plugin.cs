using System;
using UnityEngine;

namespace Primer
{
	public static class Plugin
	{
		public static void Init()
		{
			Clock.Initialize();
			Loop.Initialize();
		}

		public static void Exit()
		{
			NetManager.ExitAll();
		}
	}
}
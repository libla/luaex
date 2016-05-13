using System;
using System.Collections.Generic;
using System.Diagnostics;
using Utility;

public class Finalizes : Module
{
	private static readonly List<Action> finalizes;

	static Finalizes()
	{
		finalizes = new List<Action>();
		Loop.Update += delegate()
		{
			if (finalizes.Count > 0)
			{
				Action[] actions;
				lock (finalizes)
				{
					actions = finalizes.ToArray();
					finalizes.Clear();
				}
				for (int i = 0, j = actions.Length; i < j; ++i)
				{
					actions[i]();
				}
			}
		};
	}

	public static void Add(Action action)
	{
		lock (finalizes)
		{
			finalizes.Add(action);
		}
	}
}
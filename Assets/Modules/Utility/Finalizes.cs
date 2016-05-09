using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;

[InitializeOnLoad]
class Finalizes
{
	private List<Finalize> lstfinalize;
	public delegate void Finalize();
	
	public static Finalizes Instance;

	static Finalizes()
	{
		UnityEngine.Debug.Log(123);
	}

	public Finalizes()
	{
		lstfinalize = new List<Finalize>();
	}
	public void Add(Finalize finalize)
	{
		lock (lstfinalize)
		{
			lstfinalize.Add(finalize);
		}
	}
	public bool MustUpdate()
	{
		return true;
	}
	public void Update()
	{
		if (lstfinalize.Count > 0)
		{
			Finalize[] finalizes;
			lock (lstfinalize)
			{
				finalizes = lstfinalize.ToArray();
				lstfinalize.Clear();
			}
			for (int i = 0, j = finalizes.Length; i < j; ++i)
			{
				lstfinalize[i]();
			}
		}
	}
}
using System;
using System.Collections.Generic;
using UnityEngine;
using Primer;

namespace Lua
{
	public enum TestEnum
	{
		Test1,
		Test2,
		Test3,
	}

	public abstract class TTT
	{
		public abstract void Add();
	}
	public class Test : TTT
	{
		public override void Add() { }
		public static bool operator ^(Test a, int b)
		{
			return true;
		}

		public void TestTest<T>() { }
	}
}

public struct Data
{
	public long elapsed;
	public int serial;
}

public class DataCompare : IComparer<Data>
{
	public int Compare(Data x, Data y)
	{
		if (x.elapsed != y.elapsed)
			return x.elapsed < y.elapsed ? -1 : 1;
		return x.serial - y.serial;
	}

	public static readonly DataCompare Default = new DataCompare();
}

public class Test : MonoBehaviour
{
	private readonly SortedDictionary<Data, bool> datas = new SortedDictionary<Data, bool>(DataCompare.Default);
	void Start()
	{
		datas.Add(new Data { serial = 1, elapsed = 1 }, true);
		datas.Add(new Data { serial = 2, elapsed = 2 }, true);
		datas.Add(new Data { serial = 3, elapsed = 0 }, true);
		datas.Add(new Data { serial = 2, elapsed = 2 }, true);
		Plugin.Init();
		Clock.Initialize();
		Log.Error("123");
		try
		{
			int i = (int) (new object());
		}
		catch (Exception e)
		{
			Log.Exception(e);
		}
	}

	void Update()
	{
		Log.Error("1234");
	}
}
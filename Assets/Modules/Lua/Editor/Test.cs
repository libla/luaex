using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

		public void TestTest<T>() {}
	}
}

using System;
using UnityDebug = UnityEngine.Debug;

public static class Log
{
	public static bool Debugging = true;
	public static void Error(string fmt, object arg0)
	{
		Error(string.Format(fmt, arg0));
	}
	public static void Error(string fmt, object arg0, object arg1)
	{
		Error(string.Format(fmt, arg0, arg1));
	}
	public static void Error(string fmt, object arg0, object arg1, object arg2)
	{
		Error(string.Format(fmt, arg0, arg1, arg2));
	}
	public static void Error(string fmt, params object[] args)
	{
		Error(string.Format(fmt, args));
	}
	public static void Error(string str)
	{
		UnityDebug.LogError(str);
	}
	public static void Error(Exception e)
	{
		UnityDebug.LogException(e);
	}
	public static void Warning(string fmt, object arg0)
	{
		Warning(string.Format(fmt, arg0));
	}
	public static void Warning(string fmt, object arg0, object arg1)
	{
		Warning(string.Format(fmt, arg0, arg1));
	}
	public static void Warning(string fmt, object arg0, object arg1, object arg2)
	{
		Warning(string.Format(fmt, arg0, arg1, arg2));
	}
	public static void Warning(string fmt, params object[] args)
	{
		Warning(string.Format(fmt, args));
	}
	public static void Warning(string str)
	{
		UnityDebug.LogWarning(str);
	}
	public static void Record(string fmt, object arg0)
	{
		Record(string.Format(fmt, arg0));
	}
	public static void Record(string fmt, object arg0, object arg1)
	{
		Record(string.Format(fmt, arg0, arg1));
	}
	public static void Record(string fmt, object arg0, object arg1, object arg2)
	{
		Record(string.Format(fmt, arg0, arg1, arg2));
	}
	public static void Record(string fmt, params object[] args)
	{
		Record(string.Format(fmt, args));
	}
	public static void Record(string str)
	{
		UnityDebug.Log(str);
	}
	public static void Debug(string fmt, object arg0)
	{
		if (Debugging)
			DebugImpl(string.Format(fmt, arg0));
	}
	public static void Debug(string fmt, object arg0, object arg1)
	{
		if (Debugging)
			DebugImpl(string.Format(fmt, arg0, arg1));
	}
	public static void Debug(string fmt, object arg0, object arg1, object arg2)
	{
		if (Debugging)
			DebugImpl(string.Format(fmt, arg0, arg1, arg2));
	}
	public static void Debug(string fmt, params object[] args)
	{
		if (Debugging)
			DebugImpl(string.Format(fmt, args));
	}
	public static void Debug(string str)
	{
		if (Debugging)
			DebugImpl(str);
	}

	public static void DebugImpl(string str)
	{
		UnityDebug.Log(str);
	}
}
using System;

public sealed class Log
{
	public static void Error(string fmt, params object[] args)
	{
		Error(string.Format(fmt, args));
	}
	public static void Error(string str)
	{
		
	}
	public static void Warning(string fmt, params object[] args)
	{
		Warning(string.Format(fmt, args));
	}
	public static void Warning(string str)
	{

	}
	public static void Record(string fmt, params object[] args)
	{
		Record(string.Format(fmt, args));
	}
	public static void Record(string str)
	{

	}
	public static void Debug(string fmt, params object[] args)
	{
		Debug(string.Format(fmt, args));
	}
	public static void Debug(string str)
	{

	}
}
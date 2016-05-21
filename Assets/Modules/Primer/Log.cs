using System;
using System.Collections.Generic;
using UnityDebug = UnityEngine.Debug;

namespace Primer
{
	public static class Log
	{
		public enum Level
		{
			Normal,
			Warning,
			Error
		}

		public static bool IsDebug = true;
		public static event Action<Level, string> Print;

		public static void Exception(Exception e)
		{
			HandlerAction action = HandlerAction.Acquire();
			action.datetime = DateTime.Now;
			action.exception = e;
			Loop.Run(action.emit);
		}
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
			Write(Level.Error, str);
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
			Write(Level.Warning, str);
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
			Write(Level.Normal, str);
		}
		public static void Debug(string fmt, object arg0)
		{
			if (IsDebug)
				Write(Level.Normal, string.Format(fmt, arg0));
		}
		public static void Debug(string fmt, object arg0, object arg1)
		{
			if (IsDebug)
				Write(Level.Normal, string.Format(fmt, arg0, arg1));
		}
		public static void Debug(string fmt, object arg0, object arg1, object arg2)
		{
			if (IsDebug)
				Write(Level.Normal, string.Format(fmt, arg0, arg1, arg2));
		}
		public static void Debug(string fmt, params object[] args)
		{
			if (IsDebug)
				Write(Level.Normal, string.Format(fmt, args));
		}
		public static void Debug(string str)
		{
			if (IsDebug)
				Write(Level.Normal, str);
		}

		private static void Write(Level level, string text)
		{
			HandlerAction action = HandlerAction.Acquire();
			action.datetime = DateTime.Now;
			action.level = level;
			action.text = text;
			Loop.Run(action.emit);
		}

		private static string GetLogFormat(string str, DateTime datetime)
		{
			return string.Format("[{0}-{1}-{2} {3}:{4}:{5}.{6}]{7}", datetime.Year, datetime.Month, datetime.Day, datetime.Hour, datetime.Minute, datetime.Second, datetime.Millisecond, str);
		}

		private static void ExecWrite(HandlerAction action)
		{
			if (Print != null)
			{
				try
				{
					if (action.exception != null)
					{
						Print(Level.Error, GetLogFormat(action.exception.ToString(), action.datetime));
					}
					else
					{
						Print(action.level, GetLogFormat(action.text, action.datetime));
					}
				}
				catch
				{
				}
			}
			else
			{
				if (action.exception != null)
				{
					UnityDebug.LogException(action.exception);
				}
				else
				{
					switch (action.level)
					{
					case Level.Normal:
						UnityDebug.Log(action.text);
						break;
					case Level.Warning:
						UnityDebug.LogWarning(action.text);
						break;
					case Level.Error:
						UnityDebug.LogError(action.text);
						break;
					}
				}
			}
			action.exception = null;
			action.text = null;
			action.Release();
		}

		private class HandlerAction
		{
			public Action emit
			{
				get;
				private set;
			}
			public Level level;
			public Exception exception;
			public string text;
			public DateTime datetime;

			private HandlerAction()
			{
				emit = () =>
				{
					ExecWrite(this);
				};
			}

			private static readonly Stack<HandlerAction> pool = new Stack<HandlerAction>();
			public static HandlerAction Acquire()
			{
				HandlerAction action = null;
				if (pool.Count > 0)
				{
					lock (pool)
					{
						if (pool.Count > 0)
							action = pool.Pop();
					}
				}
				return action ?? new HandlerAction();
			}
			public void Release()
			{
				lock (pool)
				{
					pool.Push(this);
				}
			}
		}
	}
}
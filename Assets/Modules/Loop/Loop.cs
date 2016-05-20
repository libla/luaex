using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;

namespace Primer
{
	public static class Loop
	{
		private static List<Action> _actions = new List<Action>();
		private static List<Action> _actions_ = new List<Action>();
		private static readonly Queue<Action> _asyncs = new Queue<Action>();
		private static readonly List<Exception> _exceptions = new List<Exception>();
		public static int MaxThreads = 8;
		private static int numThreads = 0;
		private static int currentThread = 0;
		public static event Action<Exception> OnException;

		static bool initialized = false;
		public static void Initialize()
		{
			if (!initialized)
			{
				if (!Application.isPlaying)
					return;
				initialized = true;
				currentThread = Thread.CurrentThread.ManagedThreadId;
				GameObject go = new GameObject("Loop");
				go.AddComponent<Updater>();
			}
		}

		public static void QueueToMainThread(Action action)
		{
			if (initialized && currentThread == Thread.CurrentThread.ManagedThreadId)
			{
				action();
			}
			else
			{
				lock (_actions)
				{
					_actions.Add(action);
				}
			}
		}

		public static void RunAsync(Action action)
		{
			Initialize();
			lock (_asyncs)
			{
				_asyncs.Enqueue(action);
			}
			while (true)
			{
				if (numThreads >= MaxThreads)
					return;

				int oldnumThreads = numThreads;
				if (Interlocked.CompareExchange(ref numThreads, oldnumThreads + 1, oldnumThreads) == oldnumThreads)
					break;
			}
			ThreadPool.QueueUserWorkItem(RunAsyncAction);
		}

		private static void RunAsyncAction(object o)
		{
			while (true)
			{
				Action action = null;
				lock (_asyncs)
				{
					if (_actions.Count > 0)
					{
						action = _asyncs.Dequeue();
					}
				}
				if (action == null)
					break;
				try
				{
					action();
				}
				catch (Exception e)
				{
					lock (_exceptions)
					{
						_exceptions.Add(e);
					}
				}
			}
			Interlocked.Decrement(ref numThreads);
		}

		private class Updater : MonoBehaviour
		{
			void Update()
			{
				lock (_actions)
				{
					var tmp = _actions_;
					_actions_ = _actions;
					_actions = tmp;
				}
				for (int i = 0, j = _actions_.Count; i < j; ++i)
				{
					try
					{
						_actions_[i]();
					}
					catch (Exception e)
					{
						try
						{
							if (OnException != null)
								OnException(e);
						}
						catch
						{
						}
					}
				}
				_actions_.Clear();
				if (OnException != null)
				{
					lock (_exceptions)
					{
						for (int i = 0, j = _exceptions.Count; i < j; ++i)
						{
							try
							{
								OnException(_exceptions[i]);
							}
							catch
							{
							}
						}
					}
				}
			}
		}
	}
}
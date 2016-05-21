using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Primer
{
	public static class Loop
	{
		private static List<Action> actions = new List<Action>();
		private static List<Action> actions_tmp = new List<Action>();
		private static readonly SortedDictionary<string, Action> always_actions = new SortedDictionary<string, Action>();
		private static readonly Dictionary<string, Action> always_actions_async = new Dictionary<string, Action>();
		private static readonly List<Action> always_actions_order = new List<Action>();
		private static bool always_actions_invaild = false;
		private static readonly Queue<Action> async_actions = new Queue<Action>();
		private static readonly List<Exception> exceptions = new List<Exception>();
		private static int now_threads = 0;
		private static bool initialized = false;
		private static bool current_thread_init = false;
		private static int current_thread = 0;

		public static int MaxThreads = 8;
		public static event Action<Exception> OnException;

		public static void Initialize()
		{
			if (!initialized)
			{
				if (!Application.isPlaying)
					return;
				initialized = true;
				GameObject go = new GameObject("Loop");
				UnityObject.DontDestroyOnLoad(go);
				go.hideFlags |= HideFlags.HideInHierarchy;
				go.AddComponent<Updater>();
			}
		}

		public static void Run(Action action)
		{
			if (current_thread_init && current_thread == Thread.CurrentThread.ManagedThreadId)
			{
				action();
			}
			else
			{
				lock (actions)
				{
					actions.Add(action);
				}
			}
		}

		public static void RunAlways(string name, Action action)
		{
			if (current_thread_init && current_thread == Thread.CurrentThread.ManagedThreadId && always_actions_async.Count == 0)
			{
				if (action == null)
					always_actions.Remove(name);
				else
					always_actions[name] = action;
				always_actions_invaild = true;
			}
			else
			{
				lock (always_actions_async)
				{
					always_actions_async[name] = action;
				}
			}
		}

		public static void RemoveRunAlways(string name)
		{
			RunAlways(name, null);
		}

		public static void RunAsync(Action action)
		{
			Initialize();
			lock (async_actions)
			{
				async_actions.Enqueue(action);
			}
			while (true)
			{
				if (now_threads >= MaxThreads)
					return;

				int old_threads = now_threads;
				if (Interlocked.CompareExchange(ref now_threads, old_threads + 1, old_threads) == old_threads)
					break;
			}
			ThreadPool.QueueUserWorkItem(RunAsyncAction);
		}

		private static void RunAsyncAction(object o)
		{
			while (true)
			{
				Action action = null;
				lock (async_actions)
				{
					if (actions.Count > 0)
					{
						action = async_actions.Dequeue();
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
					lock (exceptions)
					{
						exceptions.Add(e);
					}
				}
			}
			Interlocked.Decrement(ref now_threads);
		}

		private class Updater : MonoBehaviour
		{
			void Start()
			{
				current_thread = Thread.CurrentThread.ManagedThreadId;
				current_thread_init = true;
			}
			void Update()
			{
				if (actions.Count > 0)
				{
					lock (actions)
					{
						var tmp = actions_tmp;
						actions_tmp = actions;
						actions = tmp;
					}
				}
				for (int i = 0, j = actions_tmp.Count; i < j; ++i)
				{
					try
					{
						actions_tmp[i]();
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
				actions_tmp.Clear();
				if (always_actions_async.Count > 0)
				{
					lock (always_actions_async)
					{
						foreach (var action in always_actions_async)
						{
							always_actions_invaild = true;
							if (action.Value == null)
								always_actions.Remove(action.Key);
							else
								always_actions[action.Key] = action.Value;
						}
						always_actions_async.Clear();
					}
				}
				if (always_actions_invaild)
				{
					always_actions_invaild = false;
					always_actions_order.Clear();
					foreach (var action in always_actions)
					{
						always_actions_order.Add(action.Value);
					}
				}
				for (int i = 0, j = always_actions_order.Count; i < j; ++i)
				{
					try
					{
						always_actions_order[i]();
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
				if (OnException != null && exceptions.Count > 0)
				{
					lock (exceptions)
					{
						for (int i = 0, j = exceptions.Count; i < j; ++i)
						{
							try
							{
								OnException(exceptions[i]);
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
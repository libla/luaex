using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Primer
{
	public struct Schedule
	{
		public long elapsed { get; private set; }
		public int serial { get; private set; }

		private static int index = 0;

		public Schedule(uint countdown)
			: this()
		{
			this.elapsed = countdown * 1000 + Clock.Elapsed;
			this.serial = Interlocked.Increment(ref index);
		}

		public Action Action { get; set; }
	}

	public static class Loop
	{
		private class DataCompare : IComparer<Schedule>
		{
			public int Compare(Schedule x, Schedule y)
			{
				if (x.elapsed != y.elapsed)
					return x.elapsed < y.elapsed ? -1 : 1;
				return x.serial - y.serial;
			}

			public static readonly DataCompare Default = new DataCompare();
		}

		private class DataEqualityCompare : IEqualityComparer<Schedule>
		{
			public bool Equals(Schedule x, Schedule y)
			{
				return x.serial == y.serial;
			}

			public int GetHashCode(Schedule obj)
			{
				return obj.serial;
			}

			public static readonly DataEqualityCompare Default = new DataEqualityCompare();
		}

		private static List<Action> actions = new List<Action>();
		private static List<Action> actions_tmp = new List<Action>();
		private static readonly SortedDictionary<Schedule, Action> delay_actions = new SortedDictionary<Schedule, Action>(DataCompare.Default);
		private static readonly Dictionary<Schedule, Action> delay_actions_async = new Dictionary<Schedule, Action>(DataEqualityCompare.Default);
		private static readonly SortedDictionary<string, Action> always_actions = new SortedDictionary<string, Action>();
		private static readonly Dictionary<string, Action> always_actions_async = new Dictionary<string, Action>();
		private static readonly List<Action> always_actions_order = new List<Action>();
		private static bool always_actions_invaild = false;
		private static readonly Queue<Action> async_actions = new Queue<Action>();
		private static readonly List<Exception> exceptions = new List<Exception>();
		private static bool initialized = false;
		private static int now_threads = 0;
		private static bool main_thread_init = false;
		private static int main_thread = 0;

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
			if (main_thread_init && main_thread == Thread.CurrentThread.ManagedThreadId)
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

		public static Schedule RunDelay(uint delay, Action action)
		{
			if (action == null)
				throw new ArgumentNullException("action");
			Schedule s = new Schedule(delay);
			if (main_thread_init && main_thread == Thread.CurrentThread.ManagedThreadId && delay_actions_async.Count == 0)
			{
				delay_actions.Add(s, action);
			}
			else
			{
				lock (delay_actions_async)
				{
					delay_actions_async.Add(s, action);
				}
			}
			return s;
		}

		public static void RemoveRunDelay(Schedule s)
		{
			if (main_thread_init && main_thread == Thread.CurrentThread.ManagedThreadId && delay_actions_async.Count == 0)
			{
				delay_actions.Remove(s);
			}
			else
			{
				lock (delay_actions_async)
				{
					delay_actions_async[s] = null;
				}
			}
		}

		public static void RunAlways(string name, Action action)
		{
			if (main_thread_init && main_thread == Thread.CurrentThread.ManagedThreadId && always_actions_async.Count == 0)
			{
				always_actions_invaild = true;
				if (action == null)
					always_actions.Remove(name);
				else
					always_actions[name] = action;
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
					if (OnException != null)
					{
						lock (exceptions)
						{
							exceptions.Add(e);
						}
					}
				}
			}
			Interlocked.Decrement(ref now_threads);
		}

		private class Updater : MonoBehaviour
		{
			void Start()
			{
				main_thread = Thread.CurrentThread.ManagedThreadId;
				main_thread_init = true;
			}

			void Update()
			{
				lock (actions)
				{
					var tmp = actions_tmp;
					actions_tmp = actions;
					actions = tmp;
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
				if (delay_actions_async.Count > 0)
				{
					lock (delay_actions_async)
					{
						foreach (var action in delay_actions_async)
						{
							if (action.Value == null)
								delay_actions.Remove(action.Key);
							else
								delay_actions[action.Key] = action.Value;
						}
						delay_actions_async.Clear();
					}
				}
				long now = Clock.Elapsed;
				while (true)
				{
					var iterator = delay_actions.GetEnumerator();
					if (!iterator.MoveNext())
						break;
					var action = iterator.Current;
					if (action.Key.elapsed >= now)
						break;
					delay_actions.Remove(action.Key);
					action.Value();
				}
				if (exceptions.Count > 0)
				{
					lock (exceptions)
					{
						for (int i = 0, j = exceptions.Count; i < j; ++i)
						{
							if (OnException == null)
								break;
							try
							{
								OnException(exceptions[i]);
							}
							catch
							{
							}
						}
						exceptions.Clear();
					}
				}
			}
		}
	}
}
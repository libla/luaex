using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Utility
{
	public class Manager
	{
		private static readonly Dictionary<Type, bool> pendings;
		public static event Action Update;
		static Manager()
		{
			GameObject go = new GameObject("Manager");
			UnityObject.DontDestroyOnLoad(go);
			go.AddComponent<Updater>();
			pendings = new Dictionary<Type, bool>();

			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for (int i = 0; i < assemblies.Length; ++i)
			{
				Type[] types = assemblies[i].GetTypes();
				for (int j = 0; j < types.Length; ++j)
				{
					Type type = types[j];
					Type[] interfaces = type.GetInterfaces();
					for (int k = 0; k < interfaces.Length; ++k)
					{
						if (interfaces[k] == typeof(Module))
						{
							Initialize(type);
							break;
						}
					}
				}
			}
		}

		public static void Initialize()
		{
		}

		internal static void Initialize(Type type)
		{
			if (pendings.ContainsKey(type))
			{
				Log.Error(new Exception("Cycle Init."));
				Application.Quit();
			}
			pendings.Add(type, true);
			RuntimeHelpers.RunClassConstructor(type.TypeHandle);
			pendings.Remove(type);
		}

		private class Updater : MonoBehaviour
		{
			void Update()
			{
				try
				{
					Manager.Update();
				}
				catch (Exception e)
				{
					Log.Error(e);
				}
			}
		}
	}

	public abstract class Module
	{
		protected Module()
		{
			update = null;
		}

		private Action update;
		protected Action Update
		{
			set
			{
				if (update != null)
					Manager.Update -= update;
				update = value;
				if (update != null)
					Manager.Update += update;
			}
		}

		protected void Request<T>()
		{
			Manager.Initialize(typeof(T));
		}
	}
}
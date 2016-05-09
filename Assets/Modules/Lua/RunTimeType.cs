using System;
using System.Reflection;
using System.Collections.Generic;

class RunTimeType
{
	static Dictionary<string, Type> nametypes = new Dictionary<string, Type>();
	static Dictionary<Type, string> typenames = new Dictionary<Type, string>();
	static LinkedList<string> namelist = new LinkedList<string>();
	public struct TypeName
	{
		public string this[Type type]
		{
			get {
				for (Type parent = type; parent != null; parent = parent.DeclaringType)
				{
					namelist.AddFirst(parent.Name);
				}
				namelist.AddFirst(type.Namespace);
				string[] names = new string[namelist.Count];
				namelist.CopyTo(names, 0);
				namelist.Clear();
				string result = string.Join(".", names);
				typenames.Add(type, result);
				return result;
			}
		}
	}
	public static TypeName Name;
}
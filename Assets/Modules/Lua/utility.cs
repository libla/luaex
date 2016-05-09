using System;
using System.Reflection;
using System.Collections.Generic;

namespace Lua
{
    public static class Utils
    {
        private static Dictionary<string, Type> nametypes = new Dictionary<string, Type>();
        private static Assembly[] assemblys = null;
        private static LinkedList<string> namelist = new LinkedList<string>();
        public static string NameOf(Type type)
        {
            for (Type parent = type; parent != null; parent = parent.DeclaringType)
            {
                namelist.AddFirst(parent.Name);
            }
            namelist.AddFirst(type.Namespace);
            string[] names = new string[namelist.Count];
            namelist.CopyTo(names, 0);
            namelist.Clear();
            return string.Join(".", names);
        }
        public static Type FindType(string name)
        {
            if (name == null || name == "")
                return null;
            Type type;
            if (!nametypes.TryGetValue(name, out type))
            {
                if (assemblys == null)
                    assemblys = AppDomain.CurrentDomain.GetAssemblies();
                string lastname = name.Substring(name.LastIndexOf('.') + 1);
                foreach (Assembly assembly in assemblys)
                {
                    foreach (Type stype in assembly.GetExportedTypes())
                    {
                        if (stype.IsGenericType)
                            continue;
                        if (stype.Name == lastname)
                        {
                            if (NameOf(stype) == name)
                            {
                                type = stype;
                                break;
                            }
                        }
                    }
                    if (type != default(Type))
                    {
                        break;
                    }
                }
                nametypes.Add(name, type);
            }
            return type;
        }

    }

}
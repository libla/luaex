using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.Method)]
public sealed class MonoPInvokeCallbackAttribute : Attribute
{
	public MonoPInvokeCallbackAttribute(Type t) {}
}

public static class MonoPInvokeCallbackAddress
{
	private static readonly Dictionary<Delegate, IntPtr> address = new Dictionary<Delegate, IntPtr>();
	public static IntPtr From(Delegate fn)
	{
		IntPtr result;
		if (!address.TryGetValue(fn, out result))
		{
			if (fn.Target == null)
			{
				foreach (CustomAttributeData attr in CustomAttributeData.GetCustomAttributes(fn.Method))
				{
					if (attr.Constructor.DeclaringType.Name == "MonoPInvokeCallbackAttribute")
					{
						result = Marshal.GetFunctionPointerForDelegate(fn);
						address.Add(fn, result);
						return result;
					}
				}
			}
			throw new ArgumentException();
		}
		return result;
	}
}
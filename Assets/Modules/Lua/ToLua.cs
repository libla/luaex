using System;
using System.Text;
using System.Runtime.InteropServices;

namespace Lua
{
	[AttributeUsage(AttributeTargets.All)]
	public class ExportToLua : Attribute
	{
		public ExportToLua() { }
		public ExportToLua(string name) { }
	}

	[AttributeUsage(AttributeTargets.All)]
	public class NotToLuaAttribute : Attribute { }

	public static partial class API
	{
		/*
        ** tolua helper functions
        */
		[DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaEX_pushcfunction(IntPtr L, lua_CFunction fn);
		[DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaEX_pushcclosure(IntPtr L, lua_CFunction fn, int n);
		[DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern bool luaEX_pushuserdata(IntPtr L, IntPtr ptr);
		[DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern bool luaEX_getmetatable(IntPtr L, IntPtr ptr);
		[DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaEX_error(IntPtr L, byte[] str, int len);

		[DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaEX_newtype(IntPtr L, byte[] str, IntPtr type);
		[DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaEX_basetype(IntPtr L, IntPtr type);
		[DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaEX_nexttype(IntPtr L);
		[DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaEX_construct(IntPtr L, lua_CFunction fn);
		[DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaEX_method(IntPtr L, byte[] str, uint len, lua_CFunction fn);
		[DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaEX_index(IntPtr L, byte[] str, uint len, lua_CFunction fn);
		[DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaEX_newindex(IntPtr L, byte[] str, uint len, lua_CFunction fn);
		[DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaEX_setter(IntPtr L, byte[] str, uint len, lua_CFunction fn);
		[DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaEX_getter(IntPtr L, byte[] str, uint len, lua_CFunction fn);
		[DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaEX_value(IntPtr L, byte[] str, uint len, lua_CFunction fn);
		[DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaEX_newvalue(IntPtr L, byte[] str, uint len, lua_CFunction fn);
		[DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaEX_opt(IntPtr L, byte[] str, uint len, lua_CFunction fn);

		/* adapt functions */
		public static void luaEX_newtype(IntPtr L, string str, Type type)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(str);
			byte[] strbytes = new byte[bytes.Length + 1];
			Array.Copy(bytes, strbytes, bytes.Length);
			luaEX_newtype(L, strbytes, Tools.Type2IntPtr(type));
		}
		public static void luaEX_newtype(IntPtr L, Type type)
		{
			byte[] bytes = { 0 };
			luaEX_newtype(L, bytes, Tools.Type2IntPtr(type));
		}

		public static void luaEX_basetype(IntPtr L, Type type)
		{
			luaEX_basetype(L, Tools.Type2IntPtr(type));
		}

		public static int luaEX_error(IntPtr L, Exception e)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(e.Message);
			int result = luaEX_error(L, bytes, bytes.Length);
			GC.KeepAlive(bytes);
			return result;
		}
	}
}
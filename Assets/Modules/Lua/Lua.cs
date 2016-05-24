using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Lua
{
    public static class Consts
    {
        /* option for multiple returns in `lua_pcall' and `lua_call' */
        public const int LUA_MULTRET = -1;

        /* pseudo-indices */
        public const int LUA_REGISTRYINDEX = -10000;
        public const int LUA_ENVIRONINDEX = -10001;
        public const int LUA_GLOBALSINDEX = -10002;

        /* thread status; 0 is OK */
        public const int LUA_YIELD = 1;
        public const int LUA_ERRRUN = 2;
        public const int LUA_ERRSYNTAX = 3;
        public const int LUA_ERRMEM = 4;
        public const int LUA_ERRERR = 5;

        /* basic types */
        public const int LUA_TNONE = -1;
        public const int LUA_TNIL = 0;
        public const int LUA_TBOOLEAN = 1;
        public const int LUA_TLIGHTUSERDATA = 2;
        public const int LUA_TNUMBER = 3;
        public const int LUA_TSTRING = 4;
        public const int LUA_TTABLE = 5;
        public const int LUA_TFUNCTION = 6;
        public const int LUA_TUSERDATA = 7;
        public const int LUA_TTHREAD = 8;

        /* garbage-collection options */
        public const int LUA_GCSTOP = 0;
        public const int LUA_GCRESTART = 1;
        public const int LUA_GCCOLLECT = 2;
        public const int LUA_GCCOUNT = 3;
        public const int LUA_GCCOUNTB = 4;
        public const int LUA_GCSTEP = 5;
        public const int LUA_GCSETPAUSE = 6;
        public const int LUA_GCSETSTEPMUL = 7;

        /* pre-defined references */
        public const int LUA_NOREF = -2;
        public const int LUA_REFNIL = -1;
    }

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate int lua_CFunction(IntPtr L);

	public delegate int lua_CSFunction(IntPtr L);
	public delegate int lua_Writer(byte[] bytes);
	public delegate byte[] lua_Reader();

	public struct luaL_Reg
	{
		public string name;
		public lua_CFunction fn;
		public luaL_Reg(string name, lua_CFunction fn)
		{
			this.name = name;
			this.fn = fn;
		}
	}

	public sealed class luaL_Key
	{
		private IntPtr key;
		public luaL_Key()
		{
			key = Marshal.AllocHGlobal(1);
		}
		~luaL_Key()
		{
			Marshal.FreeHGlobal(key);
		}
		public static implicit operator IntPtr(luaL_Key k)
		{
			return k.key;
		}
	}

	public sealed class luaL_Keys
	{
		private IntPtr key;
		private int len;
		public luaL_Keys(int length)
		{
			len = length;
			key = Marshal.AllocHGlobal(length);
		}
		~luaL_Keys()
		{
			Marshal.FreeHGlobal(key);
		}
		public IntPtr this[int index]
		{
			get
			{
				if (index < 0 || index >= len)
					throw new ArgumentOutOfRangeException();
				return (IntPtr)((long)key + index);
			}
		}
	}

	public class ObjectReference
	{
		public object Target;
		private int index;

		private static readonly ObjectReference head = new ObjectReference { index = -1 };
		private static readonly List<ObjectReference> list = new List<ObjectReference>();

		private ObjectReference() { }

		public static ObjectReference Alloc(object obj)
		{
			ObjectReference refobj;
			int i = head.index;
			if (i == -1)
			{
				i = list.Count;
				refobj = new ObjectReference();
				list.Add(refobj);
			}
			else
			{
				refobj = list[i];
				head.index = list[i].index;
			}
			refobj.index = i;
			refobj.Target = obj;
			return list[i];
		}

		public static ObjectReference Alloc()
		{
			return Alloc(null);
		}

		public void Free()
		{
			int i = index;
			index = head.index;
			Target = null;
			head.index = i;
		}

		public static ObjectReference FromIntPtr(IntPtr intptr)
		{
			return list[intptr.ToInt32()];
		}

		public IntPtr ToIntPtr()
		{
			return (IntPtr)index;
		}

		public static explicit operator IntPtr(ObjectReference refobj)
		{
			return refobj.ToIntPtr();
		}

		public static explicit operator ObjectReference(IntPtr intptr)
		{
			return FromIntPtr(intptr);
		}
	}

	public class EnumObject
	{
		protected static readonly Dictionary<Type, Func<Enum, EnumObject>> creates = new Dictionary<Type, Func<Enum, EnumObject>>();

		public static EnumObject Add(Type type, Enum e)
		{
			Func<Enum, EnumObject> fn;
			if (creates.TryGetValue(type, out fn))
				return fn(e);
			throw new NotImplementedException(RunTimeType.Name[type]);
		}
	}

	public class EnumObject<T> : EnumObject
	{
		private static readonly Dictionary<T, EnumObject<T>> dict = new Dictionary<T, EnumObject<T>>();

		public T Target;

		static EnumObject()
		{
			creates.Add(typeof(T), delegate(Enum o)
			{
				return (EnumObject<T>)(T)(object)o;
			});
		}

		public static explicit operator EnumObject<T>(T t)
		{
			EnumObject<T> result;
			if (dict.TryGetValue(t, out result))
				return result;
			result = new EnumObject<T> { Target = t };
			dict.Add(t, result);
			return result;
		}
	}

	public abstract class ValueObject
	{
		protected static readonly Dictionary<Type, Func<object, ValueObject>> creates = new Dictionary<Type, Func<object, ValueObject>>();
		public abstract void Release();

		public static ValueObject Add(Type type, object o)
		{
			Func<object, ValueObject> fn;
			if (creates.TryGetValue(type, out fn))
				return fn(o);
			throw new NotImplementedException(RunTimeType.Name[type]);
		}

		public static void Release(ValueObject obj)
		{
			if (obj != null)
				obj.Release();
		}
	}

	public class ValueObject<T> : ValueObject
	{
		private static readonly Stack<ValueObject<T>> list = new Stack<ValueObject<T>>();

		public T Target;

		static ValueObject()
		{
			creates.Add(typeof(T), delegate(object o)
			{
				return Add((T)o);
			});
		}

		public static ValueObject<T> Add(T t)
		{
			ValueObject<T> result = Add();
			result.Target = t;
			return result;
		}

		public new static ValueObject<T> Add()
		{
			ValueObject<T> result = list.Count == 0 ? new ValueObject<T>() : list.Pop();
			return result;
		}

		public override void Release()
		{
			list.Push(this);
		}
	}

	public static partial class Tools
	{
		private static readonly Dictionary<Type, ObjectReference> types = new Dictionary<Type, ObjectReference>();
		public static IntPtr Type2IntPtr(Type type)
		{
			ObjectReference handle;
			if (!types.TryGetValue(type, out handle))
			{
				handle = ObjectReference.Alloc(type);
				types.Add(type, handle);
			}
			return (IntPtr)handle;
		}
	}

    public static partial class API
    {
#if UNITY_IPHONE || UNITY_XBOX360
        const string LIB = "__Internal";
#else
        const string LIB = "luaex";
#endif
        /*
        ** functions that read/write blocks when loading/dumping Lua chunks
        */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate byte[] Reader(IntPtr L, IntPtr ud, IntPtr sz);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int Writer(IntPtr L, IntPtr data, IntPtr sz, IntPtr ud);

        /*
        ** state manipulation
        */
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_host(IntPtr L);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_getrefers(IntPtr L);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_setrefers(IntPtr L, IntPtr custom);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_close(IntPtr L);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_newthread(IntPtr L);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern lua_CFunction lua_atpanic(IntPtr L, lua_CFunction panicf);

        /*
        ** basic stack manipulation
        */
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_gettop(IntPtr L);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_settop(IntPtr L, int idx);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushvalue(IntPtr L, int idx);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_remove(IntPtr L, int idx);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_insert(IntPtr L, int idx);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_replace(IntPtr L, int idx);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_checkstack(IntPtr L, int sz);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_xmove(IntPtr from, IntPtr to, int n);

        /*
        ** access functions (stack -> C)
        */
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_isnumber(IntPtr L, int idx);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern bool lua_isstring(IntPtr L, int idx);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern bool lua_iscfunction(IntPtr L, int idx);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern bool lua_isuserdata(IntPtr L, int idx);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_type(IntPtr L, int idx);
		[DllImport(LIB, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern string lua_typename(IntPtr L, int tp);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_equal(IntPtr L, int idx1, int idx2);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_rawequal(IntPtr L, int idx1, int idx2);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_lessthan(IntPtr L, int idx1, int idx2);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern double lua_tonumber(IntPtr L, int idx);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_tointeger(IntPtr L, int idx);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_toboolean(IntPtr L, int idx);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_tolstring(IntPtr L, int idx, out IntPtr len);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_objlen(IntPtr L, int idx);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern lua_CFunction lua_tocfunction(IntPtr L, int idx);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_touserdata(IntPtr L, int idx);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_tothread(IntPtr L, int idx);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_topointer(IntPtr L, int idx);

        /*
        ** push functions (C -> stack)
        */
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushnil(IntPtr L);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushnumber(IntPtr L, double n);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushinteger(IntPtr L, int n);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
		private static extern void lua_pushlstring(IntPtr L, byte[] s, IntPtr l);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushstring(IntPtr L, byte[] s);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushcclosure(IntPtr L, lua_CFunction fn, int n);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushboolean(IntPtr L, int b);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushlightuserdata(IntPtr L, IntPtr p);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_pushthread(IntPtr L);

        /*
        ** get functions (Lua -> stack)
        */
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_gettable(IntPtr L, int idx);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_getfield(IntPtr L, int idx, byte[] k);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_rawget(IntPtr L, int idx);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_rawgeti(IntPtr L, int idx, int n);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_createtable(IntPtr L, int narr, int nrec);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_newuserdata(IntPtr L, IntPtr sz);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_getmetatable(IntPtr L, int objindex);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_getfenv(IntPtr L, int idx);

        /*
        ** set functions (stack -> Lua)
        */
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_settable(IntPtr L, int idx);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_setfield(IntPtr L, int idx, byte[] k);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_rawset(IntPtr L, int idx);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_rawseti(IntPtr L, int idx, int n);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_setmetatable(IntPtr L, int objindex);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_setfenv(IntPtr L, int idx);

        /*
        ** `load' and `call' functions (load and run Lua code)
        */
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_call(IntPtr L, int nargs, int nresults);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_pcall(IntPtr L, int nargs, int nresults, int errfunc);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        private static extern int lua_load(IntPtr L, Reader reader, IntPtr ud, byte[] chunkname);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        private static extern int lua_dump(IntPtr L, Writer writer, IntPtr ud);

        /*
        ** coroutine functions
        */
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_yield(IntPtr L, int nresults);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_resume(IntPtr L, int narg);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_status(IntPtr L);

        /*
        ** garbage-collection function
        */
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_gc(IntPtr L, int what, int data);

        /*
        ** miscellaneous functions
        */
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_next(IntPtr L, int idx);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_concat(IntPtr L, int n);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool luaL_getmetafield(IntPtr L, int obj, string e);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool luaL_callmeta(IntPtr L, int obj, string e);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaL_typerror(IntPtr L, int narg, byte[] tname);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaL_argerror(IntPtr L, int numarg, byte[] extramsg);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr luaL_checklstring(IntPtr L, int numArg, out IntPtr l);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr luaL_optlstring(IntPtr L, int numArg, byte[] def, out IntPtr l);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern double luaL_checknumber(IntPtr L, int numArg);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern double luaL_optnumber(IntPtr L, int nArg, double def);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaL_checkinteger(IntPtr L, int numArg);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaL_optinteger(IntPtr L, int nArg, int def);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaL_checkstack(IntPtr L, int sz, byte[] msg);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaL_checktype(IntPtr L, int narg, int t);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaL_checkany(IntPtr L, int narg);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool luaL_newmetatable(IntPtr L, string tname);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr luaL_checkudata(IntPtr L, int ud, string tname);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaL_where(IntPtr L, int lvl);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaL_ref(IntPtr L, int t);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaL_unref(IntPtr L, int t, int id);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        private static extern int luaL_loadbuffer(IntPtr L, byte[] buff, int sz, byte[] name);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        private static extern int luaL_loadstring(IntPtr L, byte[] s);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr luaL_newstate();
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr luaL_findtable(IntPtr L, int idx, byte[] fname, int szhint);

        /* open all previous libraries */
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaL_initlibs(IntPtr L);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaL_openlibs(IntPtr L);

        /* adapt functions */
	    public static int lua_error(IntPtr L)
	    {
			return luaEX_error(L, null, -1);
	    }
        public static IntPtr lua_tolstring(IntPtr L, int idx, out int len)
        {
        	IntPtr ptr;
        	IntPtr result = lua_tolstring(L, idx, out ptr);
        	len = ptr.ToInt32();
        	return result;
        }
        public static void lua_pushlstring(IntPtr L, byte[] s)
        {
			lua_pushlstring(L, s, new IntPtr(s.Length));
        }
        public static void lua_pushstring(IntPtr L, string s)
        {
			byte[] bytes = Encoding.UTF8.GetBytes(s);
			lua_pushlstring(L, bytes);
        }
        public static string lua_pushfstring(IntPtr L, string fmt, params object[] argp)
        {
            string s = string.Format(fmt, argp);
            lua_pushstring(L, s);
            return s;
        }
        public static IntPtr lua_newuserdata(IntPtr L, int sz)
        {
        	return lua_newuserdata(L, new IntPtr(sz));
        }
        public static void lua_getfield(IntPtr L, int idx, string k)
        {
			byte[] bytes = Encoding.UTF8.GetBytes(k);
            byte[] strbytes = new byte[bytes.Length + 1];
            Array.Copy(bytes, strbytes, bytes.Length);
            lua_getfield(L, idx, strbytes);
            GC.KeepAlive(strbytes);
        }
        public static void lua_setfield(IntPtr L, int idx, string k)
        {
			byte[] bytes = Encoding.UTF8.GetBytes(k);
            byte[] strbytes = new byte[bytes.Length + 1];
            Array.Copy(bytes, strbytes, bytes.Length);
            lua_setfield(L, idx, strbytes);
            GC.KeepAlive(strbytes);
        }
        [MonoPInvokeCallbackAttribute(typeof(Reader))]
        private static byte[] load_reader(IntPtr L, IntPtr ud, IntPtr sz)
        {
            ObjectReference handle = (ObjectReference)ud;
            lua_Reader reader = (lua_Reader)handle.Target;
            byte[] result = reader();
            Marshal.WriteIntPtr(sz, new IntPtr(result.Length));
            return result;
        }
        public static int lua_load(IntPtr L, lua_Reader reader, string chunkname)
        {
			byte[] bytes = Encoding.UTF8.GetBytes(chunkname);
            byte[] strbytes = new byte[bytes.Length + 1];
            Array.Copy(bytes, strbytes, bytes.Length);
            ObjectReference handle = ObjectReference.Alloc(reader);
            int result = lua_load(L, load_reader, (IntPtr)handle, strbytes);
            handle.Free();
            return result;
        }
        [MonoPInvokeCallbackAttribute(typeof(Writer))]
        private static int dump_writer(IntPtr L, IntPtr data, IntPtr sz, IntPtr ud)
        {
            ObjectReference handle = (ObjectReference)ud;
            lua_Writer writer = (lua_Writer)handle.Target;
            byte[] bytes = new byte[sz.ToInt32()];
            Marshal.Copy(data, bytes, 0, (int)sz);
            return writer(bytes);
        }
        public static int lua_dump(IntPtr L, lua_Writer writer)
        {
            ObjectReference handle = ObjectReference.Alloc(writer);
            int result = lua_dump(L, dump_writer, (IntPtr)handle);
            handle.Free();
            return result;
        }
        public static int luaL_typerror(IntPtr L, int narg, string tname)
        {
			byte[] bytes = Encoding.UTF8.GetBytes(tname);
            byte[] strbytes = new byte[bytes.Length + 1];
            Array.Copy(bytes, strbytes, bytes.Length);
            int result = luaL_typerror(L, narg, strbytes);
            GC.KeepAlive(strbytes);
            return result;
        }
        public static int luaL_argerror(IntPtr L, int numarg, string extramsg)
        {
			byte[] bytes = Encoding.UTF8.GetBytes(extramsg);
            byte[] strbytes = new byte[bytes.Length + 1];
            Array.Copy(bytes, strbytes, bytes.Length);
            int result = luaL_argerror(L, numarg, strbytes);
            GC.KeepAlive(strbytes);
            return result;
        }
		public static IntPtr luaL_checklstring(IntPtr L, int numArg, out int l)
		{
			IntPtr ptr;
			IntPtr result = luaL_checklstring(L, numArg, out ptr);
			l = ptr.ToInt32();
			return result;
		}
        public static IntPtr luaL_optlstring(IntPtr L, int numArg, string def, out int l)
        {
			byte[] bytes = Encoding.UTF8.GetBytes(def);
            byte[] strbytes = new byte[bytes.Length + 1];
            Array.Copy(bytes, strbytes, bytes.Length);
			IntPtr ptr;
			IntPtr result = luaL_optlstring(L, numArg, strbytes, out ptr);
			l = ptr.ToInt32();
            GC.KeepAlive(strbytes);
            return result;
        }
        public static void luaL_checkstack(IntPtr L, int sz, string msg)
        {
			byte[] bytes = Encoding.UTF8.GetBytes(msg);
            byte[] strbytes = new byte[bytes.Length + 1];
            Array.Copy(bytes, strbytes, bytes.Length);
            luaL_checkstack(L, sz, strbytes);
            GC.KeepAlive(strbytes);
        }
        public static int luaL_error(IntPtr L, string fmt, params object[] argp)
        {
			string msg = string.Format(fmt, argp);
			byte[] bytes = Encoding.UTF8.GetBytes(msg);
			return luaEX_error(L, bytes, bytes.Length);
        }
        public static int luaL_loadbuffer(IntPtr L, byte[] buff, string name)
        {
			byte[] bytes = Encoding.UTF8.GetBytes(name);
            byte[] strbytes = new byte[bytes.Length + 1];
            Array.Copy(bytes, strbytes, bytes.Length);
            int result = luaL_loadbuffer(L, buff, buff.Length, strbytes);
            GC.KeepAlive(strbytes);
            return result;
        }
        public static int luaL_loadstring(IntPtr L, string s)
        {
			byte[] bytes = Encoding.UTF8.GetBytes(s);
            byte[] strbytes = new byte[bytes.Length + 1];
            Array.Copy(bytes, strbytes, bytes.Length);
            int result = luaL_loadstring(L, strbytes);
            GC.KeepAlive(strbytes);
            return result;
        }
        public static string luaL_gsub(IntPtr L, string s, string p, string r)
        {
			string result = s.Replace(p, r);
			lua_pushstring(L, result);
            return result;
        }
		public static bool luaL_findtable(IntPtr L, int idx, string fname, int szhint)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(fname);
			byte[] strbytes = new byte[bytes.Length + 1];
			Array.Copy(bytes, strbytes, bytes.Length);
			IntPtr result = luaL_findtable(L, idx, strbytes, szhint);
			return result == IntPtr.Zero;
		}

        /* 
        ** some useful macros
        */
        public static int lua_upvalueindex(int i)
        {
            return Consts.LUA_GLOBALSINDEX - i;
        }
        public static void lua_pop(IntPtr L, int n)
        {
            lua_settop(L, -n - 1);
        }
        public static void lua_newtable(IntPtr L)
        {
            lua_createtable(L, 0, 0);
        }
        public static void lua_register(IntPtr L, string s, lua_CFunction f)
        {
            lua_pushcfunction(L, f);
            lua_setglobal(L, s);
        }
        public static void lua_pushcfunction(IntPtr L, lua_CFunction f)
        {
            lua_pushcclosure(L, f, 0);
        }
        [MonoPInvokeCallbackAttribute(typeof(lua_CFunction))]
        private static int c2csfunction(IntPtr L)
        {
            IntPtr ptr = lua_touserdata(L, lua_upvalueindex(1));
            ObjectReference handle = (ObjectReference)Marshal.ReadIntPtr(ptr);
            lua_CSFunction f = (lua_CSFunction)handle.Target;
            return f(L);
        }
        [MonoPInvokeCallbackAttribute(typeof(lua_CFunction))]
        private static int __c2csgc(IntPtr L)
        {
            IntPtr ptr = lua_touserdata(L, 1);
            ObjectReference handle = (ObjectReference)Marshal.ReadIntPtr(ptr);
            handle.Free();
            return 0;
        }
        private static luaL_Key csmetakey = null;
        public static void lua_pushcsfunction(IntPtr L, lua_CSFunction f)
        {
            ObjectReference handle = ObjectReference.Alloc(f);
            IntPtr ptr = lua_newuserdata(L, Marshal.SizeOf(typeof(IntPtr)));
            Marshal.WriteIntPtr(ptr, (IntPtr)handle);
            if (csmetakey == null)
            {
                lua_newtable(L);
                csmetakey = new luaL_Key();
                lua_pushlightuserdata(L, csmetakey);
                lua_pushvalue(L, -2);
                lua_settable(L, Consts.LUA_REGISTRYINDEX);
                lua_pushliteral(L, "__gc");
                lua_pushcfunction(L, __c2csgc);
                lua_settable(L, -3);
            }
            else
            {
                lua_pushlightuserdata(L, csmetakey);
                lua_gettable(L, Consts.LUA_REGISTRYINDEX);
            }
            lua_setmetatable(L, -2);
            lua_pushcclosure(L, c2csfunction, 1);
        }
        public static int lua_strlen(IntPtr L, int i)
        {
            return lua_objlen(L, i).ToInt32();
        }
        public static bool lua_isfunction(IntPtr L, int n)
        {
            return lua_type(L, n) == Consts.LUA_TFUNCTION;
        }
        public static bool lua_istable(IntPtr L, int n)
        {
            return lua_type(L, n) == Consts.LUA_TTABLE;
        }
        public static bool lua_islightuserdata(IntPtr L, int n)
        {
            return lua_type(L, n) == Consts.LUA_TLIGHTUSERDATA;
        }
        public static bool lua_isnil(IntPtr L, int n)
        {
            return lua_type(L, n) == Consts.LUA_TNIL;
        }
        public static bool lua_isboolean(IntPtr L, int n)
        {
            return lua_type(L, n) == Consts.LUA_TBOOLEAN;
        }
        public static bool lua_isthread(IntPtr L, int n)
        {
            return lua_type(L, n) == Consts.LUA_TTHREAD;
        }
        public static bool lua_isnone(IntPtr L, int n)
        {
            return lua_type(L, n) == Consts.LUA_TNONE;
        }
        public static bool lua_isnoneornil(IntPtr L, int n)
        {
            return lua_type(L, n) <= 0;
        }
        public static void lua_pushliteral(IntPtr L, string s)
        {
			lua_pushstring(L, s);
        }
        public static void lua_setglobal(IntPtr L, string s)
        {
            lua_setfield(L, Consts.LUA_GLOBALSINDEX, s);
        }
        public static void lua_getglobal(IntPtr L, string s)
        {
            lua_getfield(L, Consts.LUA_GLOBALSINDEX, s);
        }
        public static string lua_tostring(IntPtr L, int n)
        {
            int len;
            IntPtr ptr = lua_tolstring(L, n, out len);
            if (ptr == IntPtr.Zero)
                return null;
            byte[] bytes = new byte[len];
            Marshal.Copy(ptr, bytes, 0, len);
			return Encoding.UTF8.GetString(bytes);
        }
        public static void luaL_argcheck(IntPtr L, bool cond, int narg, string extramsg)
        {
            if (!cond)
                luaL_argerror(L, narg, extramsg);
        }
        public static string luaL_checkstring(IntPtr L, int n)
        {
			int len;
			IntPtr ptr = luaL_checklstring(L, n, out len);
			if (ptr == IntPtr.Zero)
				return null;
			byte[] bytes = new byte[len];
			Marshal.Copy(ptr, bytes, 0, len);
			return Encoding.UTF8.GetString(bytes);
        }
        public static string luaL_optstring(IntPtr L, int n, string def)
        {
            int len;
            IntPtr ptr = luaL_optlstring(L, n, def, out len);
            if (ptr == IntPtr.Zero)
                return null;
            byte[] bytes = new byte[len];
            Marshal.Copy(ptr, bytes, 0, len);
			return Encoding.UTF8.GetString(bytes);
        }
        public static int luaL_checkint(IntPtr L, int n)
        {
            return luaL_checkinteger(L, n);
        }
        public static int luaL_optint(IntPtr L, int n, int def)
        {
            return luaL_optinteger(L, n, def);
        }
        public static int luaL_checklong(IntPtr L, int n)
        {
            return luaL_checkinteger(L, n);
        }
        public static int luaL_optlong(IntPtr L, int n, int def)
        {
            return luaL_optinteger(L, n, def);
        }
        public delegate Value CheckFunc<Value>(IntPtr L, int n);
        public static Value luaL_opt<Value>(IntPtr L, CheckFunc<Value> f, int n, Value def)
        {
            return lua_isnoneornil(L, n) ? def : f(L, n);
        }
        public static string luaL_typename(IntPtr L, int n)
        {
            return lua_typename(L, lua_type(L, n));
        }
        public static int luaL_dostring(IntPtr L, string s)
        {
            int result = luaL_loadstring(L, s);
            if (result != 0)
            {
                return result;
            }
            return lua_pcall(L, 0, Consts.LUA_MULTRET, 0);
        }
        public static void luaL_getmetatable(IntPtr L, string s)
        {
            lua_getfield(L, Consts.LUA_REGISTRYINDEX, s);
        }
        public static void luaL_register(IntPtr L, string libname, luaL_Reg[] regs)
        {
            luaL_register(L, libname, regs, 0);
        }
        public static void luaL_register(IntPtr L, string libname, luaL_Reg[] regs, int nup)
        {
            if (libname != null && libname != "")
            {
                /* check whether lib already exists */
                luaL_findtable(L, Consts.LUA_REGISTRYINDEX, "_LOADED", 1);
                lua_getfield(L, -1, libname);  /* get _LOADED[libname] */
                if (!lua_istable(L, -1))
                {  /* not found? */
                    lua_pop(L, 1);  /* remove previous result */
                    /* try global variable (and create one if it does not exist) */
                    if (!luaL_findtable(L, Consts.LUA_GLOBALSINDEX, libname, regs.Length))
                        luaL_error(L, "name conflict for module '{0}'", libname);
                    lua_pushvalue(L, -1);
                    lua_setfield(L, -3, libname);  /* _LOADED[libname] = new table */
                }
                lua_remove(L, -2);  /* remove _LOADED table */
                lua_insert(L, -(nup + 1));  /* move library table to below upvalues */
            }
            foreach (luaL_Reg reg in regs)
            {
                for (int i = 0; i < nup; i++)  /* copy upvalues to the top */
                    lua_pushvalue(L, -nup);
                lua_pushcclosure(L, reg.fn, nup);
                lua_setfield(L, -(nup + 2), reg.name);
            }
            lua_pop(L, nup);  /* remove upvalues */
        }

	    public static int luaL_absindex(IntPtr L, int i)
	    {
			return (i > 0 || i <= Consts.LUA_REGISTRYINDEX) ? i : lua_gettop(L) + i + 1;
	    }

        /*
        ** compatibility macros
        */
        public static IntPtr lua_open()
        {
            return luaL_newstate();
        }
        public static void lua_getregistry(IntPtr L)
        {
            lua_pushvalue(L, Consts.LUA_REGISTRYINDEX);
        }
        public static int lua_ref(IntPtr L)
        {
            return luaL_ref(L, Consts.LUA_REGISTRYINDEX);
        }
        public static void lua_unref(IntPtr L, int id)
        {
            luaL_unref(L, Consts.LUA_REGISTRYINDEX, id);
        }
        public static void lua_getref(IntPtr L, int id)
        {
            lua_rawgeti(L, Consts.LUA_REGISTRYINDEX, id);
        }
    }
}
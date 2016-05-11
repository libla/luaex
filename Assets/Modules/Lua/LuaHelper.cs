using System;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;
/*
namespace Lua
{
	public class Error : Exception
	{
		public Error(string message)
			: base(message)
		{
		}
		public Error(string fmt, params object[] argp)
			: this(string.Format(fmt, argp)) { }
		private static string Where(IntPtr L)
		{
			API.luaL_where(L, 1);
			string result = API.lua_tostring(L, -1);
			API.lua_pop(L, 1);
			return result;
		}
	}

	public class String
	{
		private readonly string str;
		public String(string s)
		{
			str = s;
		}
		public static implicit operator string(String s)
		{
			return s.str;
		}
	}

	public class Function
	{
		protected delegate Function Func();
		protected static readonly Dictionary<Type, Func> creates = new Dictionary<Type, Func>();

		public static Function Create(Type type)
		{
			Func fn;
			if (!creates.TryGetValue(type, out fn))
				return null;
			return fn();
		}

		protected int refidx;
		protected State refstate;

		protected Function()
		{
			refidx = Consts.LUA_NOREF;
			refstate = null;
		}
		public bool Load(IntPtr L, int idx)
		{
			if (!API.lua_isfunction(L, idx))
				return false;
			if (refstate != null)
				API.lua_unref(L, refidx);
			refstate = L;
			API.lua_pushvalue(L, idx);
			refidx = API.lua_ref(L);
			return true;
		}

		public bool Push(IntPtr L)
		{
			if (refstate != L)
				return false;
			API.lua_getref(L, refidx);
			if (!API.lua_isnil(L, -1))
				return true;
			API.lua_pop(L, 1);
			return false;
		}
	}

	public sealed class State : IDisposable
	{
		private IntPtr L;
		private readonly Dictionary<object, ObjectReference> objects;
		private readonly Dictionary<Enum, ObjectReference> enums;
		private static readonly List<Hold> holds = new List<Hold>();

		public State()
		{
			L = API.lua_open();
			GCHandle handle = GCHandle.Alloc(this, GCHandleType.Weak);
			API.lua_setrefers(L, (IntPtr)handle);
			API.luaL_initlibs(L);
			API.luaL_openlibs(L);
			objects = new Dictionary<object, ObjectReference>();
			enums = new Dictionary<Enum, ObjectReference>();
		}

		~State()
		{
			Finalizes.Add(delegate()
			{
				Dispose(false);
			});
		}
		private void Dispose(bool force)
		{
			if (L == IntPtr.Zero)
			{
				return;
			}
			ObjectReference handle = (ObjectReference)API.lua_getrefers(L);
			handle.Free();
			foreach (var pair in objects)
			{
				pair.Value.Free();
			}
			foreach (var pair in enums)
			{
				pair.Value.Free();
			}
			objects.Clear();
			enums.Clear();
			API.lua_close(L);
			L = IntPtr.Zero;
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		public static implicit operator IntPtr(State state)
		{
			return state.L;
		}
		public static implicit operator State(IntPtr L)
		{
			ObjectReference handle = (ObjectReference)API.lua_getrefers(L);
			return (State)handle.Target;
		}

		public Hold Backup(IntPtr L)
		{
			if (holds.Count == 0)
			{
				return new Hold(L);
			}
			int index = holds.Count - 1;
			Hold hold = holds[index];
			holds.RemoveAt(index);
			return hold;
		}

		public sealed class Hold : IDisposable
		{
			private readonly IntPtr l;
			private readonly int top;
			public Hold(IntPtr L)
			{
				l = L;
				top = API.lua_gettop(L);
			}
			public void Dispose()
			{
				API.lua_settop(l, top);
				holds.Add(this);
			}
		}

		public class Value : IDisposable
		{
			protected int refidx;
			protected State refstate;

			protected Value(IntPtr L, int idx)
			{
				refstate = L;
				API.lua_pushvalue(L, idx);
				refidx = API.lua_ref(L);
			}
			~Value()
			{
				Finalizes.Add(delegate()
				{
					Dispose(false);
				});
			}
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}
			public void Push(IntPtr L)
			{
				if (API.lua_host(L) == refstate.L)
				{
					API.lua_getref(L, refidx);
				}
				else
				{
					API.lua_pushnil(L);
				}
			}
			private void Dispose(bool force)
			{
				IntPtr L = refstate.L;
				if (L == IntPtr.Zero)
				{
					return;
				}
				API.lua_unref(L, refidx);
				refidx = Consts.LUA_NOREF;
			}
		}

		public bool PushType(int n)
		{
			API.lua_pushnumber(L, n);
			return true;
		}

		public bool PushType(uint n)
		{
			API.lua_pushnumber(L, n);
			return true;
		}

		public bool PushType(short n)
		{
			API.lua_pushnumber(L, n);
			return true;
		}

		public bool PushType(ushort n)
		{
			API.lua_pushnumber(L, n);
			return true;
		}

		public bool PushType(byte n)
		{
			API.lua_pushnumber(L, n);
			return true;
		}

		public bool PushType(sbyte n)
		{
			API.lua_pushnumber(L, n);
			return true;
		}

		public bool PushType(char n)
		{
			API.lua_pushnumber(L, n);
			return true;
		}

		public bool PushType(double d)
		{
			API.lua_pushnumber(L, d);
			return true;
		}

		public bool PushType(float f)
		{
			API.lua_pushnumber(L, f);
			return true;
		}

		public bool PushType(bool b)
		{
			API.lua_pushboolean(L, b ? 1 : 0);
			return true;
		}

		public bool PushType(string s)
		{
			if (s == null)
				API.lua_pushnil(L);
			else
				API.lua_pushstring(L, s);
			return true;
		}

		public bool PushType(ulong l)
		{
			byte[] bytes = new byte[9];
			bytes[0] = 76;
			for (int i = 1; i < 9; ++i)
			{
				bytes[9 - i] = (byte)(l & 0xff);
				l >>= 8;
			}
			API.lua_pushlstring(L, bytes);
			return true;
		}

		public bool PushType(long l)
		{
			return PushType((ulong)l);
		}

		public bool PushType(Function f)
		{
			if (f == null)
			{
				API.lua_pushnil(L);
				return true;
			}
			return f.Push(L);
		}

		public bool PushType(object o)
		{
			if (o == null)
			{
				API.lua_pushnil(L);
				return true;
			}
			return PushType(o, o.GetType());
		}

		public bool PushType<T>(T o)
		{
			Type type = typeof(T);
			if (type.IsValueType)
			{
				if (type.IsEnum)
				{
					ObjectReference refobj;
					Enum e = o as Enum;
					if (!enums.TryGetValue(e, out refobj))
					{
						refobj = ObjectReference.Alloc(e);
						enums.Add(e, refobj);
					}
					if (API.tolua_pushuserdata(L, refobj.ToIntPtr()))
					{
						if (API.tolua_getmetatable(L, Tools.Type2IntPtr(type)))
						{
							API.lua_setmetatable(L, -2);
						}
						else
						{
							API.lua_pop(L, 1);
							switch (Type.GetTypeCode(Enum.GetUnderlyingType(type)))
							{
								case TypeCode.SByte:
								case TypeCode.Int16:
								case TypeCode.Int32:
									return PushType((int)((object)e));
								case TypeCode.Byte:
								case TypeCode.UInt16:
								case TypeCode.UInt32:
									return PushType((uint)((object)e));
								case TypeCode.UInt64:
									return PushType((ulong)((object)e));
								case TypeCode.Int64:
									return PushType((long)((object)e));
							}
							return false;
						}
					}
					return true;
				}
				return PushType(ValueObject<T>.Add(o), typeof(T));
			}
			if (o == null)
			{
				API.lua_pushnil(L);
				return true;
			}
			return PushType(o, o.GetType());
		}

		public bool PushType<T>(T[] t)
		{
			if (t == null)
			{
				API.lua_pushnil(L);
				return true;
			}
			Type type = typeof(T);
			int top = API.lua_gettop(L);
			API.lua_createtable(L, t.Length, 0);
			for (int i = 0, j = t.Length; i < j; ++i)
			{
				if (!PushType(t[i]))
				{
					API.lua_settop(L, top);
					return false;
				}
				API.lua_rawseti(L, -2, i + 1);
			}
			return true;
		}

		public bool PushType<T>(IList<T> t)
		{
			if (t == null)
			{
				API.lua_pushnil(L);
				return true;
			}
			Type type = typeof(T);
			int top = API.lua_gettop(L);
			API.lua_createtable(L, t.Count, 0);
			for (int i = 0, j = t.Count; i < j; ++i)
			{
				if (!PushType(t[i]))
				{
					API.lua_settop(L, top);
					return false;
				}
				API.lua_rawseti(L, -2, i + 1);
			}
			return true;
		}

		public bool PushType<TKey, TValue>(IDictionary<TKey, TValue> t)
		{
			if (t == null)
			{
				API.lua_pushnil(L);
				return true;
			}
			int top = API.lua_gettop(L);
			API.lua_createtable(L, 0, t.Count);
			foreach (var kv in t)
			{
				if (!PushType(kv.Key))
				{
					API.lua_settop(L, top);
					return false;
				}
				if (!PushType(kv.Value))
				{
					API.lua_settop(L, top);
					return false;
				}
				API.lua_rawset(L, -3);
			}
			return true;
		}

		private bool PushType(Enum e, Type type)
		{
			ObjectReference refobj;
			if (!enums.TryGetValue(e, out refobj))
			{
				refobj = ObjectReference.Alloc(e);
				enums.Add(e, refobj);
			}
			if (API.tolua_pushuserdata(L, refobj.ToIntPtr()))
			{
				if (API.tolua_getmetatable(L, Tools.Type2IntPtr(type)))
				{
					API.lua_setmetatable(L, -2);
				}
				else
				{
					API.lua_pop(L, 1);
					switch (Type.GetTypeCode(Enum.GetUnderlyingType(type)))
					{
						case TypeCode.SByte:
						case TypeCode.Int16:
						case TypeCode.Int32:
							return PushType((int)((object)e));
						case TypeCode.Byte:
						case TypeCode.UInt16:
						case TypeCode.UInt32:
							return PushType((uint)((object)e));
						case TypeCode.UInt64:
							return PushType((ulong)((object)e));
						case TypeCode.Int64:
							return PushType((long)((object)e));
					}
					return false;
				}
			}
			return true;
		}

		private bool PushType(object o, Type type)
		{
			if (type.IsArray)
			{
				if (type.GetArrayRank() != 1)
					return false;
				type = type.GetElementType();
				if (type.IsEnum)
					type = Enum.GetUnderlyingType(type);
				Array array = o as Array;
				API.lua_createtable(L, array.Length, 0);
				for (int i = 0; i < array.Length; ++i)
				{
					if (!PushType(array.GetValue(i), type))
					{
						API.lua_pop(L, 1);
						return false;
					}
					API.lua_rawseti(L, -2, i + 1);
				}
				return true;
			}
			else if (type.IsPrimitive)
			{
				if (type == typeof(bool))
					return PushType((bool)o);
				else if (type == typeof(ulong))
					return PushType((ulong)o);
				else if (type == typeof(long))
					return PushType((long)o);
				else
					return PushType(Convert.ToDouble(o));
			}
			else if (type == typeof(string))
			{
				return PushType((string)o);
			}
			else if (type.IsEnum)
			{
				return PushType(o, Enum.GetUnderlyingType(type));
			}
			else if (type.IsSubclassOf(typeof(Function)))
			{
				return PushType((Function)o);
			}
			else if (type.IsSubclassOf(typeof(Table)))
			{
				return PushType((Table)o);
			}
			else if (type.IsSubclassOf(typeof(Thread)))
			{
				return PushType((Thread)o);
			}
			if (type.IsValueType)
			{
				ObjectReference gch = ObjectReference.Alloc(o);
				IntPtr ptr = API.lua_newuserdata(L, Marshal.SizeOf(typeof(IntPtr)));
				Marshal.WriteIntPtr(ptr, (IntPtr)gch);
			}
			else
			{
				ObjectReference gch;
				if (objects.TryGetValue(o, out gch))
				{
					getweaktable();
					API.lua_pushlightuserdata(L, (IntPtr)gch);
					API.lua_gettable(L, -2);
					API.lua_replace(L, -2);
					return true;
				}
				gch = ObjectReference.Alloc(o);
				objects.Add(o, gch);
				IntPtr ptr = API.lua_newuserdata(L, Marshal.SizeOf(typeof(IntPtr)));
				Marshal.WriteIntPtr(ptr, (IntPtr)gch);
				getweaktable();
				API.lua_pushlightuserdata(L, (IntPtr)gch);
				API.lua_pushvalue(L, -3);
				API.lua_settable(L, -3);
				API.lua_pop(L, 1);
			}
			TypeInfo info = gettypeof(type);
			API.lua_pushlightuserdata(L, (IntPtr)info.gch);
			API.lua_gettable(L, Consts.LUA_REGISTRYINDEX);
			API.lua_setmetatable(L, -2);
			return true;
		}

		public bool ToType(int idx, ref int n)
		{
			if (!API.lua_isnumber(L, idx))
				return false;
			n = API.lua_tointeger(L, idx);
			return true;
		}

		public bool ToType(int idx, ref uint n)
		{
			if (!API.lua_isnumber(L, idx))
				return false;
			n = (uint)(API.lua_tonumber(L, idx));
			return true;
		}

		public bool ToType(int idx, ref short n)
		{
			if (!API.lua_isnumber(L, idx))
				return false;
			n = (short)API.lua_tointeger(L, idx);
			return true;
		}

		public bool ToType(int idx, ref ushort n)
		{
			if (!API.lua_isnumber(L, idx))
				return false;
			n = (ushort)API.lua_tointeger(L, idx);
			return true;
		}

		public bool ToType(int idx, ref byte n)
		{
			if (!API.lua_isnumber(L, idx))
				return false;
			n = (byte)API.lua_tointeger(L, idx);
			return true;
		}

		public bool ToType(int idx, ref sbyte n)
		{
			if (!API.lua_isnumber(L, idx))
				return false;
			n = (sbyte)API.lua_tointeger(L, idx);
			return true;
		}

		public bool ToType(int idx, ref char n)
		{
			if (!API.lua_isnumber(L, idx))
				return false;
			n = (char)API.lua_tointeger(L, idx);
			return true;
		}

		public bool ToType(int idx, ref double d)
		{
			if (!API.lua_isnumber(L, idx))
				return false;
			d = API.lua_tonumber(L, idx);
			return true;
		}

		public bool ToType(int idx, ref float f)
		{
			if (!API.lua_isnumber(L, idx))
				return false;
			f = (float)API.lua_tonumber(L, idx);
			return true;
		}

		public bool ToType(int idx, ref bool b)
		{
			b = API.lua_toboolean(L, idx);
			return true;
		}

		public bool ToType(int idx, ref string s)
		{
			if (API.lua_isstring(L, idx))
			{
				s = API.lua_tostring(L, idx);
				return true;
			}
			if (API.lua_isuserdata(L, idx))
			{
				String str = null;
				if (!ToType(idx, ref str))
					return false;
				s = str;
				return true;
			}
			return false;
		}

		public bool ToType(int idx, ref ulong l)
		{
			if (!API.lua_isstring(L, idx))
				return false;
			int len;
			IntPtr ptr = API.lua_tolstring(L, idx, out len);
			if (ptr == IntPtr.Zero || len != 9)
				return false;
			byte[] bytes = new byte[9];
			Marshal.Copy(ptr, bytes, 0, 9);
			if (bytes[0] != 76)
				return false;
			l = 0;
			for (int i = 1; i < 9; ++i)
			{
				l = (l << 8) + bytes[i];
			}
			return true;
		}

		public bool ToType(int idx, ref long l)
		{
			ulong u = 0;
			if (!ToType(idx, ref u))
				return false;
			l = (long)u;
			return true;
		}

		public bool ToType<T>(int idx, ref T[] lst)
		{
			if (!API.lua_istable(L, idx))
				return false;
			int len = API.lua_objlen(L, idx).ToInt32();
			if (lst == null)
			{
				lst = new T[len];
			}
			else
			{
				int nlen = lst.Length;
				if (len > nlen)
					len = nlen;
			}
			T t = default(T);
			idx = API.luaL_absindex(L, idx);
			for (int i = 0; i < len; ++i)
			{
				API.lua_pushinteger(L, i + 1);
				API.lua_gettable(L, idx);
				if (!ToType(-1, ref t))
				{
					API.lua_pop(L, 1);
					return false;
				}
				API.lua_pop(L, 1);
				lst[i] = t;
			}
			return true;
		}

		public bool ToType<T>(int idx, ref IList<T> lst)
		{
			if (!API.lua_istable(L, idx))
				return false;
			int len = API.lua_objlen(L, idx).ToInt32();
			if (lst == null)
				lst = new List<T>();
			else
				lst.Clear();
			T t = default(T);
			idx = API.luaL_absindex(L, idx);
			for (int i = 0; i < len; ++i)
			{
				API.lua_pushinteger(L, i + 1);
				API.lua_gettable(L, idx);
				if (!ToType(-1, ref t))
				{
					API.lua_pop(L, 1);
					return false;
				}
				API.lua_pop(L, 1);
				lst.Add(t);
			}
			return true;
		}

		public bool ToType<TKey, TValue>(int idx, ref IDictionary<TKey, TValue> lst)
		{
			if (!API.lua_istable(L, idx))
				return false;
			int len = API.lua_objlen(L, idx).ToInt32();
			if (lst == null)
				lst = new Dictionary<TKey, TValue>();
			TKey key = default(TKey);
			TValue value = default(TValue);
			idx = API.luaL_absindex(L, idx);
			API.lua_pushnil(L);
			while (API.lua_next(L, idx))
			{
				if (!ToType(-2, ref key))
				{
					API.lua_pop(L, 2);
					return false;
				}
				if (!ToType(-1, ref value))
				{
					API.lua_pop(L, 2);
					return false;
				}
				lst[key] = value;
				API.lua_pop(L, 1);
			}
			return true;
		}

		public bool ToType<T>(int idx, ref T t)
		{
			object o = null;
			if (!ToType(idx, ref o, typeof(T)))
				return false;
			if (o == null)
			{
				t = default(T);
				return true;
			}
			if (!(o is T))
				return false;
			t = (T)o;
			return true;
		}

		public bool ToType(int idx, ref object o)
		{
			switch (API.lua_type(L, idx))
			{
				case Consts.LUA_TNIL:
					o = null;
					return true;
				case Consts.LUA_TBOOLEAN:
					o = API.lua_toboolean(L, idx);
					return true;
				case Consts.LUA_TNUMBER:
					o = API.lua_tonumber(L, idx);
					return true;
				case Consts.LUA_TSTRING:
					o = API.lua_tostring(L, idx);
					return true;
				case Consts.LUA_TLIGHTUSERDATA:
				case Consts.LUA_TUSERDATA:
					ObjectReference gch = (ObjectReference)API.lua_touserdata(L, idx);
					o = gch.Target;
					return true;
			}
			return false;
		}

		private bool ToType(int idx, ref object o, Type type)
		{
			if (type.IsArray)
			{
				if (type.GetArrayRank() != 1 || !API.lua_istable(L, idx))
					return false;

				type = type.GetElementType();
				int len = API.lua_objlen(L, idx).ToInt32();
				Array array = Array.CreateInstance(type, len);
				object item = null;
				for (int i = 0; i < len; ++i)
				{
					API.lua_rawgeti(L, idx, i + 1);
					if (!ToType(-1, ref item, type))
					{
						API.lua_pop(L, 1);
						return false;
					}
					try
					{
						array.SetValue(item, i);
					}
					catch
					{
						API.lua_pop(L, 1);
						return false;
					}
					API.lua_pop(L, 1);
				}
				o = array;
				return true;
			}
			if (type.IsPrimitive)
			{
				if (type == typeof(bool))
				{
					bool b = default(bool);
					if (!ToType(idx, ref b))
						return false;
					o = b;
					return true;
				}
				if (type == typeof(int))
				{
					int d = default(int);
					if (!ToType(idx, ref d))
						return false;
					o = d;
					return true;
				}
				if (type == typeof(uint))
				{
					uint d = default(uint);
					if (!ToType(idx, ref d))
						return false;
					o = d;
					return true;
				}
				if (type == typeof(float))
				{
					float d = default(float);
					if (!ToType(idx, ref d))
						return false;
					o = d;
					return true;
				}
				if (type == typeof(double))
				{
					double d = default(double);
					if (!ToType(idx, ref d))
						return false;
					o = d;
					return true;
				}
				if (type == typeof(short))
				{
					short d = default(short);
					if (!ToType(idx, ref d))
						return false;
					o = d;
					return true;
				}
				if (type == typeof(ushort))
				{
					ushort d = default(ushort);
					if (!ToType(idx, ref d))
						return false;
					o = d;
					return true;
				}
				if (type == typeof(byte))
				{
					byte d = default(byte);
					if (!ToType(idx, ref d))
						return false;
					o = d;
					return true;
				}
				if (type == typeof(sbyte))
				{
					sbyte d = default(sbyte);
					if (!ToType(idx, ref d))
						return false;
					o = d;
					return true;
				}
				if (type == typeof(char))
				{
					char d = default(char);
					if (!ToType(idx, ref d))
						return false;
					o = d;
					return true;
				}
				if (type == typeof(ulong))
				{
					ulong d = default(ulong);
					if (!ToType(idx, ref d))
						return false;
					o = d;
					return true;
				}
				if (type == typeof(long))
				{
					long d = default(long);
					if (!ToType(idx, ref d))
						return false;
					o = d;
					return true;
				}
				return false;
			}
			if (type == typeof(string))
			{
				string s = default(string);
				if (!ToType(idx, ref s))
					return false;
				o = s;
				return true;
			}
			if (type.IsEnum)
			{
				return ToType(idx, ref o, Enum.GetUnderlyingType(type));
			}
			if (type.BaseType == typeof(Function))
			{
				Function f = Function.Create(type);
				if (f == null)
					return false;
				if (!f.Load(L, idx))
					return false;
				o = f;
				return true;
			}
			if (API.lua_isnoneornil(L, idx))
			{
				o = null;
				return true;
			}
			switch (API.lua_type(L, idx))
			{
				case Consts.LUA_TBOOLEAN:
					o = API.lua_toboolean(L, idx);
					return true;
				case Consts.LUA_TNUMBER:
					o = API.lua_tonumber(L, idx);
					return true;
				case Consts.LUA_TSTRING:
					o = API.lua_tostring(L, idx);
					return true;
				case Consts.LUA_TLIGHTUSERDATA:
					o = API.lua_touserdata(L, idx);
					return true;
			}
			IntPtr ptr = API.lua_touserdata(L, idx);
			if (ptr == IntPtr.Zero)
				return false;
			ObjectReference gch = (ObjectReference)Marshal.ReadIntPtr(ptr);
			if (!type.IsInstanceOfType(gch.Target))
				return false;
			o = gch.Target;
			return true;
		}

		public T ToType<T>(int idx)
		{
			T result = default(T);
			if (!ToType(idx, ref result))
				throw new InvalidCastException(string.Format("bad argument #{0}", idx));
			return result;
		}
	}

	public static class Helper
	{
		private static class ValueTypeBuffer<T>
		{
			const int MAX_SIZE = 16 * 1024;
			static int COUNT = (MAX_SIZE - 1) / Marshal.SizeOf(typeof(T)) + 1;
			static List<T[]> buffers = new List<T[]>();
			static List<int> frees = new List<int>(2 * COUNT);

			public static int New()
			{
				if (frees.Count == 0)
				{
					T[] array = new T[COUNT];
					int start = (buffers.Count) * COUNT;
					buffers.Add(array);
					for (int i = 0; i < COUNT; ++i)
					{
						frees.Add(i + start);
					}
				}
				int index = frees.Count - 1;
				int result = frees[index];
				frees.RemoveAt(index);
				buffers[result / COUNT][result % COUNT] = default(T);
				return result;
			}
			public static void Delete(int i)
			{
				frees.Add(i);
			}
			public static T Get(int i)
			{
				return buffers[i / COUNT][i % COUNT];
			}
			public static void Set(int i, ref T t)
			{
				buffers[i / COUNT][i % COUNT] = t;
			}
		}
		public class ValueTypeInstance<T>
		{
			private int bufferIndex;
			public ValueTypeInstance()
			{
				bufferIndex = ValueTypeBuffer<T>.New();
			}
			~ValueTypeInstance()
			{
				Finalizes.Add(delegate()
				{
					ValueTypeBuffer<T>.Delete(bufferIndex);
				});
			}
			public void Set(T t)
			{
				ValueTypeBuffer<T>.Set(bufferIndex, ref t);
			}
			public void Set(ref T t)
			{
				ValueTypeBuffer<T>.Set(bufferIndex, ref t);
			}
			public T Get()
			{
				return ValueTypeBuffer<T>.Get(bufferIndex);
			}
			public static implicit operator T(ValueTypeInstance<T> inst)
			{
				return ValueTypeBuffer<T>.Get(inst.bufferIndex);
			}
		}
	}

	public static partial class API
	{
		private static Dictionary<object, ObjectReference> objects = new Dictionary<object, ObjectReference>();
		private static Dictionary<Enum, ObjectReference> enums = new Dictionary<Enum, ObjectReference>();
		public static void lua_pushtype(IntPtr L, Enum e)
		{
		}

		public static void lua_pushtype(IntPtr L, bool b)
		{
			lua_pushboolean(L, b ? 1 : 0);
		}
		public static void lua_pushtype(IntPtr L, sbyte s)
		{
			lua_pushinteger(L, s);
		}
		public static void lua_pushtype(IntPtr L, byte b)
		{
			lua_pushinteger(L, b);
		}
		public static void lua_pushtype(IntPtr L, short s)
		{
			lua_pushinteger(L, s);
		}
		public static void lua_pushtype(IntPtr L, ushort u)
		{
			lua_pushinteger(L, u);
		}
		public static void lua_pushtype(IntPtr L, int i)
		{
			lua_pushinteger(L, i);
		}
		public static void lua_pushtype(IntPtr L, uint u)
		{
			lua_pushnumber(L, u);
		}
		public static void lua_pushtype(IntPtr L, float f)
		{
			lua_pushnumber(L, f);
		}
		public static void lua_pushtype(IntPtr L, double d)
		{
			lua_pushnumber(L, d);
		}
		public static void lua_pushtype(IntPtr L, decimal d)
		{
			lua_pushnumber(L, (double)d);
		}
		[ThreadStatic] static char[] chars;
		public static void lua_pushtype(IntPtr L, char c)
		{
			if (chars == null)
				chars = new char[1];
			chars[0] = c;
			byte[] bytes = Encoding.UTF8.GetBytes(chars);
			lua_pushlstring(L, bytes);
		}
		public static void lua_pushtype(IntPtr L, string s)
		{
			if (s == null)
				lua_pushnil(L);
			else
				lua_pushstring(L, s);
		}
		public static void lua_pushtype(IntPtr L, byte[] s)
		{
			if (s == null)
				lua_pushnil(L);
			else
				lua_pushlstring(L, s);
		}
		[ThreadStatic] static byte[] longbytes;
		public static void lua_pushtype(IntPtr L, ulong l)
		{
			if (longbytes == null)
				longbytes = new byte[9];
			longbytes[0] = (byte)'L';
			for (int i = 1; i < 9; ++i)
			{
				longbytes[9 - i] = (byte)(l & 0xff);
				l >>= 8;
			}
			lua_pushlstring(L, longbytes);
		}
		public static void lua_pushtype(IntPtr L, long l)
		{
			lua_pushtype(L, (ulong)l);
		}
		public static void lua_pushtype(IntPtr L, object o)
		{
			if (o == null)
			{
				lua_pushnil(L);
			}
			else
			{
				switch (Type.GetTypeCode(o.GetType()))
				{
				case TypeCode.Empty:
				case TypeCode.DBNull:
					lua_pushnil(L);
					break;
				case TypeCode.Boolean:
					lua_pushtype(L, (bool)o);
					break;
				case TypeCode.Char:
					lua_pushtype(L, (char)o);
					break;
				case TypeCode.SByte:
					lua_pushtype(L, (sbyte)o);
					break;
				case TypeCode.Byte:
					lua_pushtype(L, (byte)o);
					break;
				case TypeCode.Int16:
					lua_pushtype(L, (short)o);
					break;
				case TypeCode.UInt16:
					lua_pushtype(L, (ushort)o);
					break;
				case TypeCode.Int32:
					lua_pushtype(L, (int)o);
					break;
				case TypeCode.UInt32:
					lua_pushtype(L, (uint)o);
					break;
				case TypeCode.Int64:
					lua_pushtype(L, (long)o);
					break;
				case TypeCode.UInt64:
					lua_pushtype(L, (ulong)o);
					break;
				case TypeCode.Single:
					lua_pushtype(L, (float)o);
					break;
				case TypeCode.Double:
					lua_pushtype(L, (double)o);
					break;
				case TypeCode.Decimal:
					lua_pushtype(L, (decimal)o);
					break;
				case TypeCode.String:
					lua_pushtype(L, (string)o);
					break;
				default:
					lua_pushtype(L, o, typeof(object));
					break;
				}
			}
		}
		public static void lua_pushtype<T>(IntPtr L, T t)
		{
			lua_pushtype(L, ref t);
		}
		public static void lua_pushtype<T>(IntPtr L, ref T t)
		{
			Type type = typeof(T);
			if (type.IsValueType)
			{
				Helper.ValueTypeInstance<T> inst = new Helper.ValueTypeInstance<T>();
				inst.Set(ref t);
				lua_pushtype(L, inst, type);
			}
			else
			{
				if (t == null)
					lua_pushnil(L);
				else
					lua_pushtype(L, t, type);
			}
		}
		public static void lua_pushtype(IntPtr L, object o, Type type)
		{

		}

		public static bool lua_totype(IntPtr L, int idx, ref bool b)
		{
			b = lua_toboolean(L, idx);
			return true;
		}
		public static bool lua_totype(IntPtr L, int idx, ref sbyte s)
		{
			if (!lua_isnumber(L, idx))
				return false;
			double d = lua_tonumber(L, idx);
			double nd = Math.Floor(d + 0.5);
			if (Math.Abs(d - nd) >= double.Epsilon)
				return false;
			if (nd > sbyte.MaxValue || nd < sbyte.MinValue)
				return false;
			s = (sbyte)nd;
			return true;
		}
		public static bool lua_totype(IntPtr L, int idx, ref byte b)
		{
			if (!lua_isnumber(L, idx))
				return false;
			double d = lua_tonumber(L, idx);
			double nd = Math.Floor(d + 0.5);
			if (Math.Abs(d - nd) >= double.Epsilon)
				return false;
			if (nd > byte.MaxValue || nd < byte.MinValue)
				return false;
			b = (byte)nd;
			return true;
		}
		public static bool lua_totype(IntPtr L, int idx, ref short s)
		{
			if (!lua_isnumber(L, idx))
				return false;
			double d = lua_tonumber(L, idx);
			double nd = Math.Floor(d + 0.5);
			if (Math.Abs(d - nd) >= double.Epsilon)
				return false;
			if (nd > short.MaxValue || nd < short.MinValue)
				return false;
			s = (short)nd;
			return true;
		}
		public static bool lua_totype(IntPtr L, int idx, ref ushort u)
		{
			if (!lua_isnumber(L, idx))
				return false;
			double d = lua_tonumber(L, idx);
			double nd = Math.Floor(d + 0.5);
			if (Math.Abs(d - nd) >= double.Epsilon)
				return false;
			if (nd > ushort.MaxValue || nd < ushort.MinValue)
				return false;
			u = (ushort)nd;
			return true;
		}
		public static bool lua_totype(IntPtr L, int idx, ref int i)
		{
			if (!lua_isnumber(L, idx))
				return false;
			double d = lua_tonumber(L, idx);
			double nd = Math.Floor(d + 0.5);
			if (Math.Abs(d - nd) >= double.Epsilon)
				return false;
			if (nd > int.MaxValue || nd < int.MinValue)
				return false;
			i = (int)nd;
			return true;
		}
		public static bool lua_totype(IntPtr L, int idx, ref uint u)
		{
			if (!lua_isnumber(L, idx))
				return false;
			double d = lua_tonumber(L, idx);
			double nd = Math.Floor(d + 0.5);
			if (Math.Abs(d - nd) >= double.Epsilon)
				return false;
			if (nd > uint.MaxValue || nd < uint.MinValue)
				return false;
			u = (uint)nd;
			return true;
		}
		public static bool lua_totype(IntPtr L, int idx, ref float f)
		{
			if (!lua_isnumber(L, idx))
				return false;
			double d = lua_tonumber(L, idx);
			if (d > float.MaxValue || d < float.MinValue)
				return false;
			f = (float)d;
			return true;
		}
		public static bool lua_totype(IntPtr L, int idx, ref double d)
		{
			if (!lua_isnumber(L, idx))
				return false;
			d = lua_tonumber(L, idx);
			return true;
		}
		public static bool lua_totype(IntPtr L, int idx, ref decimal d)
		{
			if (!lua_isnumber(L, idx))
				return false;
			double nd = lua_tonumber(L, idx);
			if (nd > decimal.ToDouble(decimal.MaxValue) || nd < decimal.ToDouble(decimal.MinValue))
				return false;
			d = (decimal)nd;
			return true;
		}
		public static bool lua_totype(IntPtr L, int idx, ref char c)
		{
			string s = null;
			if (!lua_totype(L, idx, ref s))
				return false;
			if (s.Length != 1)
				return false;
			c = s[0];
			return true;
		}
		public static bool lua_totype(IntPtr L, int idx, ref string s)
		{
			if (!lua_isstring(L, idx))
				return false;
			s = lua_tostring(L, idx);
			return true;
		}
		public static bool lua_totype(IntPtr L, int idx, ref byte[] s)
		{
			if (!lua_isstring(L, idx))
				return false;
			int len;
			IntPtr ptr = lua_tolstring(L, idx, out len);
			s = new byte[len];
			Marshal.Copy(ptr, s, 0, len);
			return true;
		}
		public static bool lua_totype(IntPtr L, int idx, ref ulong u)
		{
			if (longbytes == null)
				longbytes = new byte[9];
			if (!lua_isstring(L, idx))
				return false;
			int len;
			IntPtr ptr = lua_tolstring(L, idx, out len);
			if (len != 9)
				return false;
			Marshal.Copy(ptr, longbytes, 0, 9);
			if (longbytes[0] != (byte)'L')
				return false;
			u = 0;
			for (int i = 1; i < 9; ++i)
			{
				u = (u << 8) + longbytes[i];
			}
			return true;
		}
		public static bool lua_totype(IntPtr L, int idx, ref long l)
		{
			ulong u = 0;
			if (!lua_totype(L, idx, ref u))
				return false;
			l = (long)u;
			return true;
		}
		public static bool lua_totype(IntPtr L, int idx, ref object o)
		{
			switch (lua_type(L, idx))
			{
			case Consts.LUA_TNIL:
				o = null;
				return true;
			case Consts.LUA_TBOOLEAN:
				o = API.lua_toboolean(L, idx);
				return true;
			case Consts.LUA_TNUMBER:
				o = API.lua_tonumber(L, idx);
				return true;
			case Consts.LUA_TSTRING:
				o = API.lua_tostring(L, idx);
				return true;
			case Consts.LUA_TUSERDATA:
				o = ((ObjectReference)lua_touserdata(L, idx)).Target;
				return true;
			}
			return false;
		}
		public static bool lua_totype<T>(IntPtr L, int idx, ref T t)
		{
			Type type = typeof(T);
			if (type.IsValueType)
			{
				if (!lua_isuserdata(L, idx))
					return false;
				ObjectReference handle = (ObjectReference)lua_touserdata(L, idx);
				Helper.ValueTypeInstance<T> inst = handle.Target as Helper.ValueTypeInstance<T>;
				if (inst == null)
					return false;
				t = inst.Get();
				return true;
			}
			else
			{
				if (lua_isnil(L, idx))
				{
					t = default(T);
					return true;
				}
				if (!lua_isuserdata(L, idx))
					return false;
				ObjectReference handle = (ObjectReference)lua_touserdata(L, idx);
				if (!(handle.Target is T))
					return false;
				t = (T)handle.Target;
				return true;
			}
		}
		public static bool lua_totype<T>(IntPtr L, int idx, ref T[] t)
		{
			T[] array;
			if (lua_istable(L, idx))
			{
				int len = lua_objlen(L, idx).ToInt32();
				array = new T[len];
				T tt = default(T);
				for (int i = 0; i < len; ++i)
				{
					lua_rawgeti(L, idx, i + 1);
					if (!lua_totype(L, -1, ref tt))
						return false;
					lua_pop(L, 1);
					array[i] = tt;
				}
				t = array;
				return true;
			}
			if (!lua_isuserdata(L, idx))
				return false;
			ObjectReference handle = (ObjectReference)lua_touserdata(L, idx);
			array = handle.Target as T[];
			if (array == null)
				return false;
			t = array;
			return true;
		}
		public static T luaL_checktype<T>(IntPtr L, int idx)
		{
			T result = default(T);
			if (!lua_totype(L, idx, ref result))
				luaL_typerror(L, idx, "");
			return result;
		}
	}
}*/
using System;
using System.Collections;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Primer;

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

	public sealed class State : IDisposable
	{
		private IntPtr L;
		private readonly Dictionary<object, ObjectReference> objects;
		private readonly Dictionary<Enum, ObjectReference> enums;
		private static readonly Stack<Hold> holds = new Stack<Hold>();

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
			Loop.Run(delegate()
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
			return holds.Count == 0 ? new Hold(L) : holds.Pop();
		}

		public Hold Backup()
		{
			return Backup(L);
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
				holds.Push(this);
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
			string s = new string(new char[] {n});
			API.lua_pushstring(L, s);
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

		public bool PushType(object o)
		{
			if (o == null)
			{
				API.lua_pushnil(L);
				return true;
			}
			return PushType(o, o.GetType());
		}

		public bool PushType(byte[] bytes)
		{
			if (bytes == null)
			{
				API.lua_pushnil(L);
				return true;
			}
			API.lua_pushlstring(L, bytes);
			return true;
		}

		public bool PushType(Array array)
		{// todo
			return false;
		}

		public bool PushType<T>(T o)
		{
			Type type = typeof(T);
			if (type.IsValueType)
			{
				if (type.IsEnum)
					return PushEnum(o);
				return PushValue(o);
			}
			if (o == null)
			{
				API.lua_pushnil(L);
				return true;
			}
			return PushType(o, o.GetType());
		}

		private bool PushEnum<T>(T t)
		{
			PushUserData((EnumObject<T>)t, typeof(T));
			return true;
		}

		private bool PushValue<T>(T t)
		{
			PushUserData(ValueObject<T>.Add(t), typeof(T));
			return true;
		}

		private bool PushType(object o, Type type)
		{
			if (type.IsArray)
			{
				if (type.GetArrayRank() == 1 && type.GetElementType() == typeof(byte))
					return PushType(o as byte[]);
				return PushType(o as Array);
			}
			if (typeof(IList).IsAssignableFrom(type))
			{
				IList list = o as IList;
				API.lua_createtable(L, list.Count, 0);
				for (int i = 0; i < list.Count; ++i)
				{
					if (!PushType(list[i]))
					{
						API.lua_pop(L, 1);
						return false;
					}
					API.lua_rawseti(L, -2, i + 1);
				}
				return true;
			}
			if (typeof(IDictionary).IsAssignableFrom(type))
			{
				IDictionary dict = o as IDictionary;
				API.lua_createtable(L, 0, dict.Count);
				foreach (DictionaryEntry kv in dict)
				{
					if (!PushType(kv.Key))
					{
						API.lua_pop(L, 1);
						return false;
					}
					if (!PushType(kv.Value))
					{
						API.lua_pop(L, 2);
						return false;
					}
					API.lua_rawset(L, -3);
				}
				return true;
			}
			if (type.IsPrimitive)
			{
				if (type == typeof(bool))
					return PushType((bool)o);
				if (type == typeof(ulong))
					return PushType((ulong)o);
				if (type == typeof(long))
					return PushType((long)o);
				return PushType(Convert.ToDouble(o));
			}
			if (type == typeof(string))
				return PushType((string)o);
			if (type.IsValueType)
			{
				if (type.IsEnum)
				{
					try
					{
						PushUserData(EnumObject.Add(type, (Enum)o), type);
						return true;
					}
					catch (NotImplementedException)
					{
						switch (Type.GetTypeCode(Enum.GetUnderlyingType(type)))
						{
							case TypeCode.SByte:
							case TypeCode.Int16:
							case TypeCode.Int32:
								return PushType((int)o);
							case TypeCode.Byte:
							case TypeCode.UInt16:
							case TypeCode.UInt32:
								return PushType((uint)o);
							case TypeCode.UInt64:
								return PushType((ulong)o);
							case TypeCode.Int64:
								return PushType((long)o);
						}
					}
					return true;
				}
				PushUserData(ValueObject.Add(type, o), type);
				return true;
			}
			PushUserData(o, type);
			return true;
		}

		private void PushUserData(object o, Type t)
		{
			ObjectReference gch;
			if (!objects.TryGetValue(o, out gch))
			{
				gch = ObjectReference.Alloc(o);
				objects.Add(o, gch);
			}
			if (API.luaEX_pushuserdata(L, (IntPtr)gch))
			{
				if (API.luaEX_getmetatable(L, Tools.Type2IntPtr(t)))
					API.lua_setmetatable(L, -2);
			}
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
			string s = null;
			if (!ToType(idx, ref s))
				return false;
			if (s == null || s.Length != 1)
				return false;
			n = s[0];
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
			if (API.lua_isnil(L, idx))
			{
				s = null;
				return true;
			}
			return false;
		}

		public bool ToType(int idx, ref ulong l)
		{
			int type = API.lua_type(L, idx);
			switch (type)
			{
			case Consts.LUA_TNUMBER:
				double d = 0;
				if (!ToType(idx, ref d) || d < ulong.MinValue || d > ulong.MaxValue)
					return false;
				l = (ulong)d;
				return true;
			case Consts.LUA_TSTRING:
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
			return false;
		}

		public bool ToType(int idx, ref long l)
		{
			int type = API.lua_type(L, idx);
			switch (type)
			{
			case Consts.LUA_TNUMBER:
				double d = 0;
				if (!ToType(idx, ref d) || d < long.MinValue || d > long.MaxValue)
					return false;
				l = (long)d;
				return true;
			case Consts.LUA_TSTRING:
				ulong u = 0;
				if (!ToType(idx, ref u))
					return false;
				l = (long)u;
				return true;
			}
			return false;
		}

		public bool ToType(int idx, ref byte[] bytes)
		{
			if (API.lua_isstring(L, idx))
			{
				int len;
				IntPtr ptr = API.lua_tolstring(L, idx, out len);
				bytes = new byte[len];
				Marshal.Copy(ptr, bytes, 0, len);
				return true;
			}
			return false;
		}

		public bool ToType<T>(int idx, ref T[] t)
		{
			Array array = null;
			if (!ToType(idx, ref array, typeof(T[])))
				return false;
			try
			{
				t = (T[])array;
				return true;
			}
			catch (InvalidCastException)
			{
				return false;
			}
		}

		private bool ToType(int idx, ref Array array, Type type)
		{// todo
			return false;
		}

		public bool ToType<T>(int idx, ref T t)
		{
			Type type = typeof(T);
			if (type.IsValueType)
			{
				object o = null;
				if (!ToType(idx, ref o, typeof(object)))
					return false;
				if (type.IsEnum)
				{
					EnumObject<T> e = o as EnumObject<T>;
					if (e == null)
						return false;
					t = e.Target;
					return true;
				}
				ValueObject<T> v = o as ValueObject<T>;
				if (v == null)
					return false;
				t = v.Target;
				return true;
			}
			else
			{
				object o = null;
				if (!ToType(idx, ref o, type))
					return false;
				t = (T)o;
				return true;
			}
		}

		public bool ToType<T>(int idx, ref ValueObject<T> t) where T : struct
		{
			object o = null;
			if (!ToType(idx, ref o, typeof(ValueObject<T>)))
				return false;
			t = (ValueObject<T>)o;
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
				case Consts.LUA_TTABLE:
					return true;
				case Consts.LUA_TLIGHTUSERDATA:
					{
						ObjectReference gch = (ObjectReference)API.lua_touserdata(L, idx);
						o = gch.Target;
					}
					break;
				case Consts.LUA_TUSERDATA:
					{
						IntPtr ptr = API.lua_touserdata(L, idx);
						ObjectReference gch = (ObjectReference)Marshal.ReadIntPtr(ptr);
						o = gch.Target;
					}
					break;
				default:
					return false;
			}
			EnumObject e = o as EnumObject;
			if (e != null)
			{
				o = e.GetTarget();
			}
			else
			{
				ValueObject v = o as ValueObject;
				if (v != null)
					o = v.GetTarget();
			}
			return true;
		}

		private bool ToType(int idx, ref object o, Type type)
		{
			if (type.IsArray)
			{
				if (type.GetArrayRank() == 1 && type.GetElementType() == typeof(byte))
				{
					byte[] bytes = null;
					if (!ToType(idx, ref bytes))
						return false;
					o = bytes;
					return true;
				}
				Array array = null;
				if (!ToType(idx, ref array, type))
					return false;
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
			if (type.IsValueType)
			{
				IntPtr ptr = API.lua_touserdata(L, idx);
				if (ptr == IntPtr.Zero)
					return false;
				ObjectReference gch = (ObjectReference)Marshal.ReadIntPtr(ptr);
				if (type.IsEnum)
				{
					EnumObject e = gch.Target as EnumObject;
					if (e == null)
						return false;
					o = e.GetTarget();
					return o.GetType() == type;
				}
				ValueObject v = gch.Target as ValueObject;
				if (v == null)
					return false;
				o = v.GetTarget();
				return o.GetType() == type;
			}
			else
			{
				if (API.lua_isnil(L, idx))
				{
					o = null;
					return true;
				}
				ObjectReference gch;
				if (API.lua_islightuserdata(L, idx))
				{
					gch = (ObjectReference)API.lua_touserdata(L, idx);
				}
				else if (API.lua_isuserdata(L, idx))
				{
					IntPtr ptr = API.lua_touserdata(L, idx);
					gch = (ObjectReference)Marshal.ReadIntPtr(ptr);
				}
				else
				{
					return false;
				}
				if (gch.Target == null)
				{
					o = null;
					return true;
				}
				if (!type.IsInstanceOfType(gch.Target))
					return false;
				o = gch.Target;
				return true;
			}
		}
	}

	public static partial class Tools
	{
		public static readonly Dictionary<Type, Func<Function, Delegate>> DelegateFactory = new Dictionary<Type, Func<Function, Delegate>>();

		public class Function
		{
			private readonly int refidx;
			private readonly State refstate;

			public Function(IntPtr L, int index)
			{
				if (!API.lua_isfunction(L, index))
					throw new InvalidCastException();
				refstate = L;
				API.lua_pushvalue(L, index);
				refidx = API.lua_ref(L);
			}

			~Function()
			{
				Loop.Run(delegate()
				{
					API.lua_unref(refstate, refidx);
				});
			}

			public bool Push(IntPtr L)
			{
				State s = L;
				if (refstate != s)
					return false;
				API.lua_getref(L, refidx);
				return true;
			}
		}

		public static Delegate CreateDelegate(Type type, IntPtr L, int index)
		{
			Func<Function, Delegate> f;
			if (!DelegateFactory.TryGetValue(type, out f))
				throw new NotImplementedException(RunTimeType.Name[type]);
			return f(new Function(L, index));
		}
	}
}
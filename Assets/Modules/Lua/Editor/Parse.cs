﻿using System;
using System.CodeDom;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CodeDom;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Lua
{
	public static class Parse
	{
		private static readonly Dictionary<Type, string> typeset = new Dictionary<Type, string>();
		private static readonly List<Type> enums = new List<Type>();
		private static readonly List<Type> delegates = new List<Type>();
		private static readonly List<Type> types = new List<Type>();
		private static readonly Dictionary<Type, Type> basetypes = new Dictionary<Type, Type>();

		private static readonly Type[] primitives =
		{
			typeof(Type), typeof(void), typeof(bool),
			typeof(sbyte), typeof(byte), typeof(short),
			typeof(ushort), typeof(int), typeof(uint),
			typeof(long), typeof(ulong), typeof(char),
			typeof(float), typeof(double), typeof(string),
			typeof(object), typeof(ValueType),
		};

		private static readonly Dictionary<string, string> operators = new Dictionary<string, string>()
		{
			{"op_Addition", "__add"},
			{"op_Subtraction", "__sub"},
			{"op_Multiply", "__mul"},
			{"op_Division", "__div"},
			{"op_Modulus", "__mod"},
			{"op_Equality", "__eq"},
			{"op_LessThan", "__lt"},
			{"op_LessThanOrEqual", "__le"},
			{"op_UnaryNegation", "__unm"},
		};

		private static readonly Type[] numbertypes =
		{
			typeof(byte),
			typeof(sbyte),
			typeof(short),
			typeof(ushort),
			typeof(int),
			typeof(uint),
			typeof(float),
			typeof(double),
		};

		private static readonly Dictionary<Type, Define.BuildType> Types = new Dictionary<Type, Define.BuildType>();

		private struct Function : IComparable<Function>
		{
			public int CompareTo(Function rhs)
			{
				bool lparams = args.Length > 0 && args[args.Length - 1].mode == ParamMode.PARAMS;
				bool rparams = rhs.args.Length > 0 && rhs.args[rhs.args.Length - 1].mode == ParamMode.PARAMS;
				if (lparams)
				{
					if (!rparams)
						return 1.CompareTo(-1);
				}
				if (rparams)
					return -1.CompareTo(1);
				int result = -args.Length.CompareTo(rhs.args.Length);
				if (result != 0)
					return result;
				for (int i = 0; i < args.Length; ++i)
				{
					int lidx = Array.IndexOf(numbertypes, args[i].type);
					int ridx = Array.IndexOf(numbertypes, rhs.args[i].type);
					if (lidx != -1)
					{
						if (ridx != -1)
						{
							result = lidx.CompareTo(ridx);
							if (result != 0)
								return result;
							continue;
						}
						return -1.CompareTo(1);
					}
					if (ridx == -1)
						continue;
					return 1.CompareTo(-1);
				}
				return 0;
			}
			public Type ret;
			public enum ParamMode
			{
				IN,
				REF,
				OUT,
				PARAMS,
			}
			public struct Param
			{
				public Type type;
				public string name;
				public ParamMode mode;
			}
			public Param[] args;
		}

		private struct ClassType
		{
			public Type type;
			public Type parent;
			public struct Property
			{
				public string name;
				public Type type;
			}
			public struct Method : IComparable<Method>
			{
				public int CompareTo(Method rhs)
				{
					int result = string.Compare(name, rhs.name, StringComparison.Ordinal);
					if (result != 0)
						return result;
					return func.CompareTo(rhs.func);
				}
				public string name;
				public bool instance;
				public Function func;
			}
			public struct Opt : IComparable<Opt>
			{
				public int CompareTo(Opt rhs)
				{
					int result = string.Compare(type, rhs.type, StringComparison.Ordinal);
					if (result != 0)
						return result;
					if (param == rhs.param)
						return 0;
					if (param == null)
						return -1;
					if (rhs.param == null)
						return 1;
					return 0;
				}
				public string type;
				public Type ret;
				public Type param;
			}

			public Function[] news;
			public Function[] indexs;
			public Function[] newindexs;
			public Property[] getters;
			public Property[] setters;
			public Property[] values;
			public Property[] newvalues;
			public Property[] consts;
			public Method[] methods;
			public Opt[] opts;
		}
		private struct FunctionType
		{
			public Type type;
			public Function func;
		}
		private struct EnumType
		{
			public Type type;
			public Type vtype;
			public string[] items;
		}

		private static void ParseFuncParam(ParameterInfo[] pinfos, ref Function.Param[] args)
		{
			args = new Function.Param[pinfos.Length];
			for (int i = 0; i < pinfos.Length; ++i)
			{
				int index = pinfos[i].Position;
				args[index].type = pinfos[i].ParameterType;
				args[index].name = pinfos[i].Name;
				if (pinfos[i].ParameterType.IsByRef)
				{
					args[index].type = pinfos[i].ParameterType.GetElementType();
					args[index].mode = pinfos[i].GetCustomAttributes(typeof(OutAttribute), true).Length > 0 ?
						Function.ParamMode.OUT :
						Function.ParamMode.REF;
				}
				else if (pinfos[i].ParameterType.IsArray && pinfos[i].GetCustomAttributes(typeof(ParamArrayAttribute), true).Length > 0)
				{
					args[index].mode = Function.ParamMode.PARAMS;
				}
				else
				{
					args[index].mode = Function.ParamMode.IN;
				}
			}
		}

		public static void Build()
		{
			regexfilters.Clear();
			matchfilters.Clear();
			for (int i = 0; i < Define.Filters.Length; ++i)
			{
				string filter = Define.Filters[i];
				if (filter.StartsWith("~"))
					regexfilters.Add(new Regex("^" + filter.Substring(1) + "$"));
				else
					matchfilters[filter] = true;
			}
			Types.Clear();
			for (int i = 0; i < Define.Buildtypes.Length; i++)
			{
				Define.BuildType buildtype = Define.Buildtypes[i];
				if (buildtype.type.IsGenericTypeDefinition)
					continue;
				Types[buildtype.type] = buildtype;
			}
			for (int i = 0; i < Define.BaseTypes.Length; i++)
			{
				Type type = Define.BaseTypes[i];
				if (!Types.ContainsKey(type))
				{
					Define.BuildType buildtype = Define.T(type, "");
					Types[buildtype.type] = buildtype;
				}
			}

			Dictionary<Type, bool> yieldtypes = new Dictionary<Type, bool>();
			List<Define.BuildType> ordertypes = new List<Define.BuildType>();
			LinkedList<Type> temptypes = new LinkedList<Type>();

			typeset.Clear();
			enums.Clear();
			delegates.Clear();
			types.Clear();
			basetypes.Clear();

			// 遍历类型，按父子类型排序
			Dictionary<Type, Define.BuildType> newTypes = new Dictionary<Type, Define.BuildType>();
			foreach (var type in Types)
			{
				Type t = type.Key;
				temptypes.Clear();
				for (Type tt = t; tt != null; tt = tt.BaseType)
				{
					if (yieldtypes.ContainsKey(tt))
						break;
					if (Array.IndexOf(Define.DropTypes, tt) >= 0)
						continue;
					if (!Types.ContainsKey(tt) && !newTypes.ContainsKey(tt))
					{
						newTypes[tt] = new Define.BuildType { type = tt, name = "", module = "" };
						yieldtypes[tt] = false;
					}
					else
					{
						yieldtypes[tt] = true;
					}
					temptypes.AddFirst(tt);
				}
				foreach (var tt in temptypes)
				{
					ordertypes.Add(newTypes.ContainsKey(tt) ? newTypes[tt] : Types[tt]);
				}
			}

			ordertypes = ordertypes.OrderBy(type =>
			{
				return IsStaticType(type.type) ? 1 : 0;
			}).ToList();

			// 把类型按枚举、委托、结构和类归类好
			foreach (var buildtype in ordertypes)
			{
				string name = buildtype.name;
				if (name.StartsWith("."))
					name = buildtype.module + name;
				Type type = buildtype.type;
				if (type.IsEnum)
				{
					if (Filter(type))
					{
						typeset[type] = name;
						enums.Add(type);
					}
				}
				else if (type.IsValueType && !type.IsPrimitive)
				{
					if (Filter(type))
					{
						typeset[type] = name;
						types.Add(type);
					}
				}
				else
				{
					if (type.BaseType == typeof(MulticastDelegate))
					{
						if (Filter(type))
						{
							typeset[type] = name;
							delegates.Add(type);
						}
					}
					else
					{
						if (Filter(type))
						{
							typeset[type] = name;
							types.Add(type);
							for (Type basetype = type.BaseType; basetype != null; basetype = basetype.BaseType)
							{
								if (yieldtypes.ContainsKey(basetype))
								{
									basetypes[type] = basetype;
									break;
								}
							}
						}
					}
				}
			}

			// 把primity类型加入类映射表
			foreach (Type type in primitives)
			{
				if (!typeset.ContainsKey(type))
				{
					typeset.Add(type, "");
				}
			}

			Func<Type, bool> verify = delegate(Type type)
			{
				while (type.IsByRef || type.IsArray)
				{
					type = type.GetElementType();
				}
				if (!typeset.ContainsKey(type))
				{
					if (Array.IndexOf(Define.DropTypes, type) < 0)
						return false;
					string name = "";
					for (Type parent = type; parent != null; parent = parent.DeclaringType)
					{
						name = "." + parent.Name + name;
					}
					name = Define.Module + name;
					if (type.IsEnum)
					{
						if (!Filter(type))
							return false;
						typeset[type] = name;
						enums.Add(type);
					}
					else if (!type.IsValueType && type.BaseType == typeof(MulticastDelegate))
					{
						if (!Filter(type))
							return false;
						typeset[type] = name;
						delegates.Add(type);
					}
				}
				return true;
			};

			List<ClassType> classtypes = new List<ClassType>();
			List<Function> news = new List<Function>();
			List<Function> indexs = new List<Function>();
			List<Function> newindexs = new List<Function>();
			List<ClassType.Property> getters = new List<ClassType.Property>();
			List<ClassType.Property> setters = new List<ClassType.Property>();
			List<ClassType.Property> values = new List<ClassType.Property>();
			List<ClassType.Property> newvalues = new List<ClassType.Property>();
			List<ClassType.Property> consts = new List<ClassType.Property>();
			List<ClassType.Method> methods = new List<ClassType.Method>();
			List<ClassType.Opt> opts = new List<ClassType.Opt>();

			// 对于类里面的每个成员进行归类
			for (int i = 0; i < types.Count; i++)
			{
				news.Clear();
				indexs.Clear();
				newindexs.Clear();
				getters.Clear();
				setters.Clear();
				values.Clear();
				newvalues.Clear();
				consts.Clear();
				methods.Clear();
				opts.Clear();
				Type type = types[i];
				foreach (PropertyInfo property in type.GetProperties(BindingFlags.Default | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Instance))
				{
					if (!Filter(type, property))
						continue;
					if (!verify(property.PropertyType))
						continue;
					ParameterInfo[] indexparams = property.GetIndexParameters();
					if (indexparams.Length > 0)
					{
						Function fn = new Function { ret = property.PropertyType };
						if (!verify(fn.ret))
							continue;
						ParseFuncParam(indexparams, ref fn.args);
						bool fail = false;
						for (int j = 0; j < fn.args.Length; j++)
						{
							if (!verify(fn.args[j].type))
							{
								fail = true;
								break;
							}
						}
						if (fail)
							continue;
						if (property.CanRead)
						{
							MethodInfo method = property.GetGetMethod();
							if (method != null)
							{
								indexs.Add(fn);
							}
						}
						if (property.CanWrite)
						{
							MethodInfo method = property.GetSetMethod();
							if (method != null)
							{
								newindexs.Add(fn);
							}
						}
					}
					else
					{
						ClassType.Property val = new ClassType.Property { type = property.PropertyType, name = property.Name };
						if (property.CanRead)
						{
							MethodInfo method = property.GetGetMethod();
							if (method != null)
							{
								if (method.IsStatic)
								{
									values.Add(val);
								}
								else
								{
									getters.Add(val);
								}
							}
						}
						if (property.CanWrite)
						{
							MethodInfo method = property.GetSetMethod();
							if (method != null)
							{
								if (method.IsStatic)
								{
									newvalues.Add(val);
								}
								else
								{
									setters.Add(val);
								}
							}
						}
					}
				}
				foreach (FieldInfo field in type.GetFields(BindingFlags.Default | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Instance))
				{
					if (!Filter(type, field))
						continue;
					if (!verify(field.FieldType))
						continue;
					ClassType.Property val = new ClassType.Property { type = field.FieldType, name = field.Name };
					if (field.IsLiteral)
					{
						consts.Add(val);
					}
					else if (field.IsStatic)
					{
						values.Add(val);
						newvalues.Add(val);
					}
					else
					{
						getters.Add(val);
						setters.Add(val);
					}
				}
				if (!type.IsAbstract && !type.IsInterface)
				{
					ConstructorInfo[] constructors =
						type.GetConstructors(BindingFlags.Default | BindingFlags.DeclaredOnly | BindingFlags.Public);
					if (constructors.Length == 0)
					{
						Function fn = new Function { ret = type, args = new Function.Param[0] };
						news.Add(fn);
					}
					else
					{
						foreach (ConstructorInfo constructor in constructors)
						{
							Function fn = new Function { ret = type };
							ParseFuncParam(constructor.GetParameters(), ref fn.args);
							bool fail = false;
							for (int j = 0; j < fn.args.Length; j++)
							{
								if (!verify(fn.args[j].type))
								{
									fail = true;
									break;
								}
							}
							if (fail)
								continue;
							news.Add(fn);
						}
					}
				}
				foreach (MethodInfo method in type.GetMethods(BindingFlags.Default | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Instance))
				{
					if (method.IsGenericMethod)
						continue;
					if (!Filter(type, method))
						continue;
					if (!verify(method.ReturnType))
						continue;
					Function fn = new Function { ret = method.ReturnType };
					ParseFuncParam(method.GetParameters(), ref fn.args);
					bool fail = false;
					for (int j = 0; j < fn.args.Length; j++)
					{
						if (!verify(fn.args[j].type))
						{
							fail = true;
							break;
						}
					}
					if (fail)
						continue;
					if (method.IsStatic && method.IsSpecialName)
					{
						if (operators.ContainsKey(method.Name))
						{
							if (fn.args.Length > 0 && fn.args[0].type == type)
							{
								ClassType.Opt val = new ClassType.Opt
								{
									type = operators[method.Name],
									ret = fn.ret,
									param = fn.args.Length > 1 ? fn.args[1].type : null,
								};
								opts.Add(val);
							}
						}
					}
					else
					{
						if (method.IsStatic)
						{
							if (operators.ContainsKey(method.Name))
							{
								if (fn.args.Length > 0 && fn.args[0].type == type)
								{
									ClassType.Opt val = new ClassType.Opt
									{
										type = operators[method.Name],
										ret = fn.ret,
										param = fn.args.Length > 1 ? fn.args[1].type : null,
									};
									opts.Add(val);
									continue;
								}
							}
						}
						else
						{
							if (operators.ContainsKey(method.Name))
							{
								ClassType.Opt val = new ClassType.Opt
								{
									type = operators[method.Name],
									ret = fn.ret,
									param = fn.args.Length > 0 ? fn.args[0].type : null,
								};
								opts.Add(val);
								continue;
							}
						}
						{
							ClassType.Method val = new ClassType.Method
							{
								name = method.Name,
								func = fn,
								instance = method.IsStatic,
							};
							methods.Add(val);
						}
					}
				}
				if (type.BaseType != null && !IsStaticType(type))
				{
					foreach (MethodInfo method in type.GetMethods(BindingFlags.Default | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
					{
						if (method.IsGenericMethod)
							continue;
						if (!Filter(type, method))
							continue;
						if (!verify(method.ReturnType))
							continue;
						if (method.DeclaringType != type)
						{
							Function fn = new Function { ret = method.ReturnType };
							ParseFuncParam(method.GetParameters(), ref fn.args);
							bool fail = false;
							for (int j = 0; j < fn.args.Length; j++)
							{
								if (!verify(fn.args[j].type))
								{
									fail = true;
									break;
								}
							}
							if (fail)
								continue;
							if (method.IsStatic && method.IsSpecialName)
							{
								if (operators.ContainsKey(method.Name))
								{
									if (fn.args.Length > 0 && fn.args[0].type == method.DeclaringType)
									{
										ClassType.Opt val = new ClassType.Opt
										{
											type = operators[method.Name],
											ret = fn.ret,
											param = fn.args.Length > 1 ? fn.args[1].type : null,
										};
										if (opts.FindIndex(m => { return m.type == val.type; }) >= 0)
										{
											opts.Add(val);
										}
									}
								}
							}
							else
							{
								if (method.IsStatic)
								{
									if (operators.ContainsKey(method.Name))
									{
										if (fn.args.Length > 0 && fn.args[0].type == method.DeclaringType)
										{
											ClassType.Opt val = new ClassType.Opt
											{
												type = operators[method.Name],
												ret = fn.ret,
												param = fn.args.Length > 1 ? fn.args[1].type : null,
											};
											if (opts.FindIndex(m => { return m.type == val.type; }) >= 0)
											{
												opts.Add(val);
											}
											continue;
										}
									}
								}
								else
								{
									if (operators.ContainsKey(method.Name))
									{
										ClassType.Opt val = new ClassType.Opt
										{
											type = operators[method.Name],
											ret = fn.ret,
											param = fn.args.Length > 0 ? fn.args[0].type : null,
										};
										if (opts.FindIndex(m => { return m.type == val.type; }) >= 0)
										{
											opts.Add(val);
										}
										continue;
									}
								}
								if (methods.FindIndex(m => { return m.name == method.Name; }) >= 0)
								{
									ClassType.Method val = new ClassType.Method
									{
										name = method.Name,
										func = fn,
										instance = method.IsStatic,
									};
									methods.Add(val);
								}
							}
						}
					}
				}
				Type basetype;
				if (!basetypes.TryGetValue(type, out basetype))
					basetype = null;
				news.Sort();
				indexs.Sort();
				newindexs.Sort();
				methods.Sort();
				opts.Sort();

				ClassType classtype = new ClassType
				{
					type = type,
					parent = basetype,
					news = news.ToArray(),
					indexs = indexs.ToArray(),
					newindexs = newindexs.ToArray(),
					getters = getters.ToArray(),
					setters = setters.ToArray(),
					values = values.ToArray(),
					newvalues = newvalues.ToArray(),
					consts = consts.ToArray(),
					methods = methods.ToArray(),
					opts = opts.ToArray(),
				};
				classtypes.Add(classtype);

				news.Clear();
				indexs.Clear();
				newindexs.Clear();
				getters.Clear();
				setters.Clear();
				values.Clear();
				newvalues.Clear();
				consts.Clear();
				methods.Clear();
				opts.Clear();
			}

			string path = Application.dataPath + Define.Path;
			if (!Directory.Exists(Path.GetDirectoryName(path)))
				Directory.CreateDirectory(Path.GetDirectoryName(path));
			FileStream stream = new FileStream(path + ".txt", FileMode.Create);
			stream.WriteByte(0xEF);
			stream.WriteByte(0xBB);
			stream.WriteByte(0xBF);
			StreamWriter file = new StreamWriter(stream, Encoding.UTF8);

			CodeUnit code = new CodeUnit();
			CodePackage package = new CodePackage("ToLua");
			CodeClassDefine classdef = new CodeClassDefine("Export");
			code.Import("System");
			code.Import("System.Collections.Generic");
			code.Import("Lua");
			code.Add(package);
			package.Add(classdef);
			CodeTypeMethod register = new CodeTypeMethod("Register");
			classdef.Add(register);
			register.modify = "public static";
			register.method.Return(new CodeTypeExp(typeof(int)));
			register.method.param.Add(new CodeParam(typeof(IntPtr), "L"));
			CodeBlockStat registers = register.method.block;

			if (delegates.Count > 0)
			{
				CodeTypeNew typenew = new CodeTypeNew();
				classdef.Add(typenew);
				typenew.modify = "static";
				CodeVariable dict = new CodeVariable(new CodeTypeExp(Tools.DelegateFactory.GetType()), "dict",
													new CodeMemberExp(new CodeTypeExp(typeof(Tools)), "DelegateFactory"));
				CodeVariableExp dictexp = new CodeVariableExp(dict);
				typenew.method.block.stats.Add(new CodeExpStat(new CodeVariableDefineExp(dict)));
				for (int i = 0; i < delegates.Count; i++)
				{
					Type type = delegates[i];
					string name = typeset[type];
					string prefix = name.Replace(".", "_");
					CodeTypeMethod tolua = new CodeTypeMethod("Create_" + prefix + "_FromLua");
					classdef.Add(tolua);
					tolua.modify = "private static";
					tolua.method.Return(new CodeTypeExp(typeof(Delegate)));
					tolua.method.param.Add(new CodeParam(typeof(Tools.Function), "f"));
					List<CodeStat> stats = tolua.method.block.stats;
					stats.Add(new CodeReturnStat(new CodeLiteralExp(null)));
					CodeMethodInvokeExp method = new CodeTypeMethodInvokeExp(dictexp, "Add");
					typenew.method.block.stats.Add(new CodeExpStat(method));
					method.param.Add(new CodeTypeOfExp(new CodeTypeExp(type)));
					method.param.Add(new CodeThisMemberExp(tolua.name));
				}
			}

			for (int i = 0; i < enums.Count; i++)
			{
				Type type = enums[i];
				string name = typeset[type];
				string prefix = name.Replace(".", "_");
				CodeMethodInvokeExp newtype = new CodeTypeMethodInvokeExp(new CodeTypeExp(typeof(API)), "luaEX_newtype");
				registers.stats.Add(new CodeExpStat(newtype));
				newtype.param.Add(new CodeParamExp("L"));
				newtype.param.Add(new CodeTypeOfExp(new CodeTypeExp(type)));
				newtype.param.Add(new CodeLiteralExp(name));
				string[] names = Enum.GetNames(type);
				for (int j = 0; j < names.Length; j++)
				{
					CodeMethodInvokeExp value = new CodeTypeMethodInvokeExp(new CodeTypeExp(typeof(API)), "luaEX_value");
					registers.stats.Add(new CodeExpStat(value));
					value.param.Add(new CodeParamExp("L"));
					value.param.Add(new CodeLiteralExp(names[j]));
					CodeTypeMethod tolua = new CodeTypeMethod(prefix + "_" + names[j] + "_ToLua");
					classdef.Add(tolua);
					value.param.Add(new CodeThisMemberExp(tolua.name));
					tolua.modify = "public static";
					tolua.attributes.Add("MonoPInvokeCallbackAttribute(typeof(lua_CFunction))");
					tolua.method.Return(new CodeTypeExp(typeof(int)));
					tolua.method.param.Add(new CodeParam(typeof(IntPtr), "L"));
					List<CodeStat> stats = tolua.method.block.stats;
					CodeVariable state = new CodeVariable(new CodeTypeExp(typeof(State)), "state",
												new CodeCastExp(typeof(State), new CodeParamExp("L")));
					stats.Add(new CodeExpStat(new CodeVariableDefineExp(state)));
					CodeTypeMethodInvokeExp pushtype = new CodeTypeMethodInvokeExp(new CodeVariableExp(state), "PushType");
					stats.Add(new CodeExpStat(pushtype));
					pushtype.param.Add(new CodeMemberExp(new CodeTypeExp(type), names[j]));
					CodeTypeMethodInvokeExp pushname = new CodeTypeMethodInvokeExp(new CodeTypeExp(typeof(API)), "lua_pushlstring");
					stats.Add(new CodeExpStat(pushname));
					pushname.param.Add(new CodeParamExp("L"));
					CodeArrayInitExp array = new CodeArrayInitExp(typeof(byte));
					pushname.param.Add(array);
					foreach (byte c in Encoding.UTF8.GetBytes(names[j]))
					{
						array.list.Add(new CodeLiteralExp(c));
					}
					CodeTypeMethodInvokeExp pushvalue = new CodeTypeMethodInvokeExp(new CodeTypeExp(typeof(API)), "lua_pushvalue");
					stats.Add(new CodeExpStat(pushvalue));
					pushvalue.param.Add(new CodeParamExp("L"));
					pushvalue.param.Add(new CodeLiteralExp(-2));
					CodeTypeMethodInvokeExp rawset = new CodeTypeMethodInvokeExp(new CodeTypeExp(typeof(API)), "lua_rawset");
					stats.Add(new CodeExpStat(rawset));
					rawset.param.Add(new CodeParamExp("L"));
					rawset.param.Add(new CodeLiteralExp(1));
					stats.Add(new CodeReturnStat(new CodeLiteralExp(1)));
				}

				CodeMethodInvokeExp nexttype = new CodeTypeMethodInvokeExp(new CodeTypeExp(typeof(API)), "luaEX_nexttype");
				registers.stats.Add(new CodeExpStat(nexttype));
				nexttype.param.Add(new CodeParamExp("L"));
			}

			/**
				file.WriteLine("Type: {0}", typeset[type.type]);
				for (int j = 0; j < type.methods.Length; j++)
				{
					file.WriteLine("methods: {0}", type.methods[j].name);
				}
				file.WriteLine("");
			 */

			for (int i = 0; i < classtypes.Count; i++)
			{
				ClassType type = classtypes[i];
				string name = typeset[type.type];
				string prefix = name.Replace(".", "_");
				CodeMethodInvokeExp newtype = new CodeTypeMethodInvokeExp(new CodeTypeExp(typeof(API)), "luaEX_newtype");
				registers.stats.Add(new CodeExpStat(newtype));
				newtype.param.Add(new CodeParamExp("L"));
				newtype.param.Add(new CodeTypeOfExp(new CodeTypeExp(type.type)));
				if (!string.IsNullOrEmpty(name))
				{
					newtype.param.Add(new CodeLiteralExp(name));
				}
				if (type.parent != null)
				{
					CodeMethodInvokeExp basetype = new CodeTypeMethodInvokeExp(new CodeTypeExp(typeof(API)), "luaEX_basetype");
					registers.stats.Add(new CodeExpStat(basetype));
					basetype.param.Add(new CodeParamExp("L"));
					basetype.param.Add(new CodeTypeOfExp(new CodeTypeExp(type.parent)));
				}
				if (!string.IsNullOrEmpty(name) && type.news.Length > 0)
				{
					string fnname = prefix + "_New";
					CodeTypeMethod fn = new CodeTypeMethod(fnname);
					classdef.Add(fn);
					fn.modify = "public static";
					fn.attributes.Add("MonoPInvokeCallbackAttribute(typeof(lua_CFunction))");
					fn.method.Return(new CodeTypeExp(typeof(int)));
					fn.method.param.Add(new CodeParam(typeof(IntPtr), "L"));
					CodeBlockStat fnstat = fn.method.block;
					CodeTryStat trystat = new CodeTryStat();

					// 填充胶水内容 to trystat
					if (type.news[0].args.Length == type.news[type.news.Length - 1].args.Length)
					{
						CodeDoWhileStat stat = new CodeDoWhileStat(new CodeLiteralExp(false));
						for (int j = 0; j < type.news.Length; ++j)
						{
							//stat.stat.stats.Add
						}

						trystat.trystat.stats.Add(stat);
					}
					else
					{

					}
					// 结束填充

					CodeCatchStat catchstat = new CodeCatchStat(new CodeTypeExp(typeof(Exception)), "e");
					trystat.catchstat.Add(catchstat);
					CodeMethodInvokeExp error = new CodeTypeMethodInvokeExp(new CodeTypeExp(typeof(API)), "luaEX_error");
					error.param.Add(new CodeParamExp("L"));
					error.param.Add(new CodeParamExp("e"));
					catchstat.stat.stats.Add(new CodeReturnStat(error));
					fnstat.stats.Add(trystat);
					fnstat.stats.Add(new CodeReturnStat(new CodeLiteralExp(1)));
					CodeMethodInvokeExp construct = new CodeTypeMethodInvokeExp(new CodeTypeExp(typeof(API)), "luaEX_construct");
					registers.stats.Add(new CodeExpStat(construct));
					construct.param.Add(new CodeParamExp("L"));
					construct.param.Add(new CodeThisMemberExp(fnname));
				}

				CodeMethodInvokeExp nexttype = new CodeTypeMethodInvokeExp(new CodeTypeExp(typeof(API)), "luaEX_nexttype");
				registers.stats.Add(new CodeExpStat(nexttype));
				nexttype.param.Add(new CodeParamExp("L"));
			}
			registers.stats.Add(new CodeReturnStat(new CodeLiteralExp(0)));
			code.Compile(file);
			file.Close();
			stream.Close();
		}

		private static readonly List<Regex> regexfilters = new List<Regex>();
		private static readonly Dictionary<string, bool> matchfilters = new Dictionary<string, bool>();

		private static string NameOf(Type type)
		{
			if (type.IsGenericType && type.IsGenericTypeDefinition)
				throw new ArgumentException(type.FullName);
			if (type == typeof(int))
				return "int";
			if (type == typeof(uint))
				return "uint";
			if (type == typeof(short))
				return "short";
			if (type == typeof(ushort))
				return "ushort";
			if (type == typeof(byte))
				return "byte";
			if (type == typeof(sbyte))
				return "sbyte";
			if (type == typeof(char))
				return "char";
			if (type == typeof(double))
				return "double";
			if (type == typeof(float))
				return "float";
			if (type == typeof(bool))
				return "bool";
			if (type == typeof(string))
				return "string";
			if (type == typeof(ulong))
				return "ulong";
			if (type == typeof(long))
				return "long";
			if (type == typeof(decimal))
				return "decimal";
			if (type == typeof(object))
				return "object";
			if (type.IsGenericType)
			{
				string name = type.GetGenericTypeDefinition().Name;
				name = name.Substring(0, name.IndexOf("`", StringComparison.Ordinal));
				name = type.DeclaringType != null ? NameOf(type.DeclaringType) + "." + name : (string.IsNullOrEmpty(type.Namespace) ? name : type.Namespace + "." + name);
				List<string> args = new List<string>();
				foreach (Type t in type.GetGenericArguments())
				{
					args.Add(NameOf(t));
				}
				return name + "<" + string.Join(", ", args.ToArray()) + ">";
			}
			return type.DeclaringType != null ? NameOf(type.DeclaringType) + "." + type.Name : (string.IsNullOrEmpty(type.Namespace) ? type.Name : type.Namespace + "." + type.Name);
		}

		private static bool IsObsolete(Type type)
		{
			foreach (var attr in type.GetCustomAttributes(true))
			{
				Type t = attr.GetType();
				if (t == typeof(ObsoleteAttribute) || t == typeof(NotToLuaAttribute))
				{
					return true;
				}
			}
			return false;
		}

		private static bool IsStaticType(Type type)
		{
			if (type.IsSealed && type.IsAbstract)
				return true;
			return Array.IndexOf(Define.StaticTypes, type) >= 0;
		}

		private static bool IsObsolete(MemberInfo member)
		{
			foreach (var attr in member.GetCustomAttributes(true))
			{
				Type t = attr.GetType();
				if (t == typeof(ObsoleteAttribute) || t == typeof(NotToLuaAttribute))
				{
					return true;
				}
			}
			return false;
		}

		private static bool Filter(string name)
		{
			if (matchfilters.ContainsKey(name))
				return false;
			for (int i = 0; i < regexfilters.Count; ++i)
			{
				if (regexfilters[i].IsMatch(name))
				{
					return false;
				}
			}
			return true;
		}

		private static bool Filter(Type type)
		{
			if (IsObsolete(type))
				return false;
			string name = NameOf(type);
			return Filter(name);
		}

		private static bool Filter(Type type, PropertyInfo info)
		{
			if (IsObsolete(info))
				return false;
			string name = NameOf(type) + "." + info.Name;
			return Filter(name);
		}

		private static bool Filter(Type type, FieldInfo info)
		{
			if (IsObsolete(info))
				return false;
			string name = NameOf(type) + "." + info.Name;
			return Filter(name);
		}

		private static bool Filter(Type type, MethodInfo info)
		{
			if (IsObsolete(info))
				return false;
			string name = NameOf(type) + "." + info.Name;
			return Filter(name);
		}
	}
}

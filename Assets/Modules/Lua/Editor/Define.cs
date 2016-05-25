using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lua
{
	public static class Define
	{
		public const string Path = "/Source/GameToLua.cs";
		public const string Module = "game";

		public static readonly List<BuildType> Buildtypes = new List<BuildType>
		{
			T(typeof(TTT)),
			T(typeof(Test)),
		};

		public static readonly List<string> Filters = new List<string>
		{
			"System.String.Chars",
			"UnityEngine.AnimationClip.averageDuration",
			"UnityEngine.AnimationClip.averageAngularSpeed",
			"UnityEngine.AnimationClip.averageSpeed",
			"UnityEngine.AnimationClip.apparentSpeed",
			"UnityEngine.AnimationClip.isLooping",
			"UnityEngine.AnimationClip.isAnimatorMotion",
			"UnityEngine.AnimationClip.isHumanMotion",
			"UnityEngine.AnimatorOverrideController.PerformOverrideClipListCleanup",
			"UnityEngine.Caching.SetNoBackupFlag",
			"UnityEngine.Caching.ResetNoBackupFlag",
			"UnityEngine.Light.areaSize",
			"UnityEngine.Security.GetChainOfTrustValue",
			"UnityEngine.Texture2D.alphaIsTransparency",
			"UnityEngine.WebCamTexture.MarkNonReadable",
			"UnityEngine.WebCamTexture.isReadable",
			"UnityEngine.Graphic.OnRebuildRequested",
			"UnityEngine.Text.OnRebuildRequested",
			"UnityEngine.Resources.LoadAssetAtPath",
			"UnityEngine.Application.ExternalEval",
		};

		public static readonly List<Type> DropTypes = new List<Type>
		{
			typeof(Motion),                                     //很多平台只是空类
			typeof(System.Array),
			typeof(System.Delegate),
			typeof(System.Enum),
			typeof(System.Reflection.MemberInfo),
		};

		public static readonly List<Type> BaseTypes = new List<Type>
		{
			typeof(UnityEngine.Object),
			typeof(object),
			typeof(ValueType),
			typeof(Type),
		};

		public class BuildType
		{
			public Type type;
			public string name;
			public string module;

			public void Module(string s)
			{
				module = s;
			}

			public void Name(string s)
			{
				name = s;
			}
		}

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
				name = type.DeclaringType != null ? NameOf(type.DeclaringType) + "_" + name : name;
				List<string> args = new List<string>();
				foreach (Type t in type.GetGenericArguments())
				{
					if (string.IsNullOrEmpty(t.Namespace))
						args.Add(NameOf(t));
					else
						args.Add(t.Namespace + "_" + NameOf(t));
				}
				return name + "___" + string.Join("__", args.ToArray()) + "___";
			}
			return type.DeclaringType != null ? NameOf(type.DeclaringType) + "_" + type.Name : type.Name;
		}

		public static BuildType T(Type t)
		{
			return T(t, "." + NameOf(t));
		}

		public static BuildType T(Type t, string name)
		{
			BuildType buildtype = new BuildType
			{
				type = t,
				name = name,
				module = Module,
			};
			return buildtype;
		}
	}
}
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

		public static BuildType T(Type t)
		{
			string name = "";
			for (Type parent = t; parent != null; parent = parent.DeclaringType)
			{
				name = "." + parent.Name + name;
			}
			return T(t, name);
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
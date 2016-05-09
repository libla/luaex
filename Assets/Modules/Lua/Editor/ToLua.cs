using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Collections.Generic;

namespace Lua
{
	public static class ToLua
	{
		[MenuItem("&ToLua/Gen Binding Files _G", false, 11)]
		public static void Binding()
		{
			//if (!Application.isPlaying)
			{
				//EditorUtility.DisplayDialog("Warning", "Use it must in running", "OK");
				//return;
			}
			Parse.Build();
		}
	}
}

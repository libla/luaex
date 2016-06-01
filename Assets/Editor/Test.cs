using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditorInternal;
using UnityObject = UnityEngine.Object;
using Lua;

public static class Test
{
	[MenuItem("Test/Test", false, 11)]
	public static void Start()
	{
		Parse.Build();
	}
}
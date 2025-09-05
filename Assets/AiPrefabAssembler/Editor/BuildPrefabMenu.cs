using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.WSA;

public class BuildPrefabMenu : EditorWindow
{
	private static BuildPrefabMenu window;

	[MenuItem("AI Prefab Assembly/Create Prefab", false, 200)]

	static void Init()
	{

		window = (BuildPrefabMenu)EditorWindow.GetWindow(typeof(BuildPrefabMenu));
		window.Show();
	}

	private string prompt;

	void OnGUI()
	{
		prompt = EditorGUILayout.TextField("Prompt:", prompt);

		if (GUILayout.Button("Build!"))
		{
			window.Close();
			BuildPrefabRequester.RequestAiAction(prompt);
		}
	}
}
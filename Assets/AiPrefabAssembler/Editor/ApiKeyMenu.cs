using System;
using UnityEditor;
using UnityEngine;

public class ApiKeyMenu : EditorWindow
{
	private static ApiKeyMenu window;

	[MenuItem("AI Prefab Assembly/Set API Key", false, 0)]

	static void Init()
	{
		
		window = (ApiKeyMenu)EditorWindow.GetWindow(typeof(ApiKeyMenu));
		window.apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
		window.Show();
	}

	private string apiKey;

	void OnGUI()
	{
		apiKey = EditorGUILayout.TextField("OpenAI API Key:", apiKey);

		if (GUILayout.Button("Done!"))
		{
			SetKey();

			window.Close();
		}
	}

	private void SetKey()
	{
		Environment.SetEnvironmentVariable("OPENAI_API_KEY", apiKey);
	}

}
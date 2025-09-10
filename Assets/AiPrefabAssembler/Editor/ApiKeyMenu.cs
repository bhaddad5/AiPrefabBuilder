using System;
using UnityEditor;
using UnityEngine;

public class ApiKeyMenu : EditorWindow
{
	private static ApiKeyMenu window;

	[MenuItem("Forge of Realms/Set API Key", false, 0)]

	static void Init()
	{
		window = (ApiKeyMenu)EditorWindow.GetWindow(typeof(ApiKeyMenu));
		window.openAIApiKey = EditorPrefs.GetString("OPENAI_API_KEY");
		window.anthropicApiKey = EditorPrefs.GetString("ANTHROPIC_API_KEY");
		window.Show();
	}

	private string openAIApiKey;
	private string anthropicApiKey;

	void OnGUI()
	{
		openAIApiKey = EditorGUILayout.TextField("OpenAI API Key:", openAIApiKey);
		anthropicApiKey = EditorGUILayout.TextField("Anthropic API Key:", anthropicApiKey);

		if (GUILayout.Button("Done!"))
		{
			SetKey();

			window.Close();
		}
	}

	private void SetKey()
	{
		EditorPrefs.SetString("OPENAI_API_KEY", openAIApiKey);
		EditorPrefs.SetString("ANTHROPIC_API_KEY", anthropicApiKey);
	}

}
using UnityEditor;
using UnityEngine;

public class AIRequestMenu : EditorWindow
{
	private static AIRequestMenu window;

	[MenuItem("AI Prefab Assembly/Request AI Action", false, 200)]

	static void Init()
	{
		window = (AIRequestMenu)EditorWindow.GetWindow(typeof(AIRequestMenu));
		window.Show();
	}

	private string prompt;

	void OnGUI()
	{
		prompt = EditorGUILayout.TextField("Prompt:", prompt);

		if (GUILayout.Button("Go!"))
		{
			AiActionRequester.RequestAiAction(prompt);
		}
	}
}
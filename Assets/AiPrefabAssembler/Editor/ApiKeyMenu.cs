using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using System.Linq;

public class ApiKeyMenu : EditorWindow
{
	private static ApiKeyMenu window;

	[MenuItem("Forge of Realms/Set API Key", false, 0)]

	static void Init()
	{
		window = (ApiKeyMenu)EditorWindow.GetWindow(typeof(ApiKeyMenu));
		window.openAIApiKey = EditorPrefs.GetString("OPENAI_API_KEY");
		window.anthropicApiKey = EditorPrefs.GetString("ANTHROPIC_API_KEY");

		window.selectedModelId = EditorPrefs.GetString("SELECTED_MODEL_ID");

		window.Show();
	}

	private string openAIApiKey;
	private string anthropicApiKey;
	private string selectedModelId = "";

	void OnGUI()
	{
		var models = ModelFetcher.FetchAllAvailableModels();
		var selectedModel = models.FirstOrDefault(m => m.Id == selectedModelId);

		if(selectedModel != null)
			gmSelection = models.IndexOf(selectedModel);

		DrawGenericMenuSection(models);

		EditorGUILayout.Space(12);

		if (selectedModel != null && selectedModel.Provider == AiModel.ApiProvider.OpenAI)
			openAIApiKey = EditorGUILayout.PasswordField("OpenAI API Key:", openAIApiKey);
		else if (selectedModel != null && selectedModel.Provider == AiModel.ApiProvider.Anthropic)
			anthropicApiKey = EditorGUILayout.PasswordField("Anthropic API Key:", anthropicApiKey);

		EditorGUILayout.Space(12);

		if (GUILayout.Button("Done!"))
		{
			SetKey();

			window.Close();
		}
	}

	private void SetKey()
	{
		EditorPrefs.SetString("SELECTED_MODEL_ID", selectedModelId);
		EditorPrefs.SetString("OPENAI_API_KEY", openAIApiKey);
		EditorPrefs.SetString("ANTHROPIC_API_KEY", anthropicApiKey);
	}

	private int gmSelection = -1;
	private void DrawGenericMenuSection(List<AiModel> models)
	{
		using (new EditorGUILayout.HorizontalScope())
		{
			EditorGUILayout.LabelField("AI Model:", GUILayout.Width(70));
			if (GUILayout.Button(gmSelection >= 0 ? models[gmSelection].DisplayName : "Choose…", GUILayout.MaxWidth(160)))
			{
				var menu = new GenericMenu();
				for (int i = 0; i < models.Count; i++)
				{
					int idx = i;
					menu.AddItem(new GUIContent(models[i].DisplayName), gmSelection == idx, () =>
					{
						gmSelection = idx;
						selectedModelId = models[gmSelection].Id;
						Repaint();
					});
				}
				menu.DropDown(GUILayoutUtility.GetLastRect());
			}
		}
	}
}
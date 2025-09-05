using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class AiActionRequester
{
	const string folder = "Assets/AiPrefabAssembler/Contextualized_Assets";

	public static void RequestAiAction(string prompt)
	{
		string generalUnityPrompt = "You are helping a developer perform actions in Unity. " +
			"You can respond to the user with Questions if you need clarification, or you can use the provided Tools to interact with the scene and perform their request, then provide a helpful description of what you did. " +
			"Always call ContextRequest to understand the objects and prefabs you are using! " +
			"Always try to figure out what they need and call the InteractWithScene tool to do it, before responding! " +
			"Unity is a Y-up coordinate system where a Distance of 1 = 1 meter. " +
			"If placing objects contextually to another object, always try to place it under the same parent! ";
			

		string sceneDescriptionPrompt = "The current Unity scene is described as such: " +
			"[objectUniqueId,objectName,localPos:(x;y;z),localEuler:(x;y;z),localScale(x;y;z),children(assetUniqueGuid,assetUniqueId,etc...)]. " +
			"The selected object will have the keyword \"Selected\" in its description. " +
			"Here is the current scene: " + SceneDescriptionBuilder.BuildSceneDescription();

		string prefabsPrompt = "These are the prefabs you have available (you can request additional info/context/bounds using the provided Tools): " + BuildPrefabsListString();

		Debug.Log(generalUnityPrompt);
		Debug.Log(sceneDescriptionPrompt);
		Debug.Log(prefabsPrompt);
		Debug.Log(prompt);

		var tools = new List<ITool>() { new InteractWithSceneTool(), new ContextRequestTool() };

		AiRequestBackend.OpenAISdk.AskContinuous(new List<string>() { generalUnityPrompt, sceneDescriptionPrompt, prefabsPrompt }, prompt, AiRequestBackend.OpenAISdk.Model.mini, tools, res =>
		{
			Debug.Log(res);
		});
	}

	private static string BuildPrefabsListString()
	{
		string prefabsStr = "";
		var assets = GetAssetPathsInFolder(folder);
		foreach (var asset in assets)
			prefabsStr += $"{folder}/{Path.GetFileName(asset)}, ";
		if (prefabsStr.EndsWith(", "))
			prefabsStr = prefabsStr.Substring(0, prefabsStr.Length - 2);

		return prefabsStr;
	}

	private static string[] GetAssetPathsInFolder(string folderPath, string filter = "")
	{
		return AssetDatabase.FindAssets(filter, new[] { folderPath })
			.Select(AssetDatabase.GUIDToAssetPath)
			.ToArray();
	}
}

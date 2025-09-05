using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildPrefabRequester
{
	const string folder = "Assets/AiPrefabAssembler/Contextualized_Assets";

	public static async void RequestAiAction(string prompt)
    {
		string generalUnityPrompt = "You are helping a developer perform actions in Unity.  Unity is a Y-up coordinate system where a Distance of 1 = 1 meter.";

		string sceneDescriptionPrompt = "The current Unity scene is described as such: " +
			"[objectUniqueId,objectName,localPos:(x;y;z),localEuler:(x;y;z),localScale(x;y;z),children(assetUniqueGuid,assetUniqueId,etc...)]. " +
			"The selected object will have the keyword \"Selected\" in its description." + 
			"Here is the current scene: " + SceneDescriptionBuilder.BuildSceneDescription();

		string aiActionsPrompt = "In response to the user's prompt, respond with a list of Actions. They are: " +
			$"{nameof(AiCommandImpl.CreateObject)}[objectCreationUniqueId,prefabPath,newObjectName,localPos:(x;y;z),localEuler:(x;y;z),localScale:(x;y;z),parentUniqueId]. If making a new GameObject with no prefab leave prefabPath blank, if you want it under the root leave parentUniqueId blank,  but leave the commas in both cases." +
			$"{nameof(AiCommandImpl.DeleteObject)}[objectUniqueId]. " +
			$"{nameof(AiCommandImpl.SetObjectParent)}[objectUniqueId,parentObjectUniqueId]. " +
			$"{nameof(AiCommandImpl.SetObjectTransform)}[objectUniqueId,localPos:(x;y;z),localEuler:(x;y;z),localScale(x;y;z)]. ";

		string prefabsPrompt = "These are the prefabs you have available (you can request additional info/context/bounds using GetPartsMetadata): " + BuildPrefabsListString();

		Debug.Log(generalUnityPrompt);
		Debug.Log(sceneDescriptionPrompt);
		Debug.Log(aiActionsPrompt);
		Debug.Log(prefabsPrompt);
		Debug.Log(prompt);

		string res = await AiRequestBackend.OpenAISdk.AskAsync(EditorPrefs.GetString("OPENAI_API_KEY"), new List<string>() { generalUnityPrompt, sceneDescriptionPrompt, aiActionsPrompt, prefabsPrompt }, prompt, AiRequestBackend.OpenAISdk.ModelLevel.mini);

		Debug.Log(res);

		AiCommandParser.TryExecuteCommands(res);

		Debug.Log("Done!");
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

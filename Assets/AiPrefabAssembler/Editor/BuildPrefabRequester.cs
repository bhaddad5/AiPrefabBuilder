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
			"[assetUniqueId,assetName,pos:(x;y;z),euler:(x;y;z),scale(x,y,z),children(assetUniqueGuid,assetUniqueId,etc...)]. " +
			"The selected asset will have the keyword \"Selected\" in its description." + 
			"Here is the current scene: " + SceneDescriptionBuilder.BuildSceneDescription();

		string aiActionsPrompt = "In response to the user's prompt, respond with a list of Actions. They are: " +
			"CreateAsset[assetUniqueId,assetName,pos:(x;y;z),euler:(x;y;z),scale(x,y,z),children([assetName...])]. " +
			"SetAssetParent[assetUniqueId,parentAssetUniqueId]. " +
			"SetAssetTransform[assetUniqueId,pos:(x;y;z),euler:(x;y;z),scale(x,y,z)]. ";

		string assetsPrompt = "These are the prefabs you have available (you can request additional info/context/bounds using GetPartsMetadata): " + BuildAssetsListString();

		string res = await AiRequestBackend.OpenAIChatSdk.AskAsync(EditorPrefs.GetString("OPENAI_API_KEY"), new List<string>() { generalUnityPrompt, sceneDescriptionPrompt, aiActionsPrompt, assetsPrompt }, prompt, AiRequestBackend.OpenAIChatSdk.ModelLevel.mini);

		Debug.Log(res);
		Debug.Log("Done!");
	}

	private static string BuildAssetsListString()
	{
		string assetsStr = "";
		var assets = GetAssetPathsInFolder(folder);
		foreach (var asset in assets)
			assetsStr += $"{Path.GetFileNameWithoutExtension(asset)}, ";
		if (assetsStr.EndsWith(", "))
			assetsStr = assetsStr.Substring(0, assetsStr.Length - 2);

		return assetsStr;
	}

	private static string[] GetAssetPathsInFolder(string folderPath, string filter = "")
	{
		return AssetDatabase.FindAssets(filter, new[] { folderPath })
			.Select(AssetDatabase.GUIDToAssetPath)
			.ToArray();
	}
}

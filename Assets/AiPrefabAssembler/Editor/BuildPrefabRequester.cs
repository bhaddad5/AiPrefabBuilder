using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public static class BuildPrefabRequester
{
	const string folder = "Assets/AiPrefabAssembler/Contextualized_Assets";

	public static void RequestBuildPrefab(string prompt)
    {
		string assetsStr = "";
		var assets = GetAssetPathsInFolder(folder);
		foreach (var asset in assets)
			assetsStr += $"{Path.GetFileNameWithoutExtension(asset)}, ";
		if (assetsStr.EndsWith(", "))
			assetsStr = assetsStr.Substring(0, assetsStr.Length - 2);

		string fullPrompt = $"You will be assembling a a Unity Prefab matching the prompt: {prompt}. " +
			"Use the provided tools to request data on the exact size of the sub-parts you would like to use. " +
			"Format your final return string as \"[assetName,pos:(x;y;z),euler:(x;y;z)]\" for each part instance. " +
			"Be mindful of the sizes of the parts, and ensure they all line-up properly in the final prefab. " + 
			"Here is a list of available assets: " +
			assetsStr;

		AiRequestBackend.OpenAIChatSdk.AskContinuous(Environment.GetEnvironmentVariable("OPENAI_API_KEY"), fullPrompt, new ToolsImplementation(), (s) => Debug.Log(s), (res) => 
		{
			FinalResultPrefabBuilder.BuildPrefabFromInstructions(res);
		});
	}

	private static string[] GetAssetPathsInFolder(string folderPath, string filter = "")
	{
		return AssetDatabase.FindAssets(filter, new[] { folderPath })
			.Select(AssetDatabase.GUIDToAssetPath)
			.ToArray();
	}
}

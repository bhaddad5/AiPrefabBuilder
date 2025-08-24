using System;
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

		//string filterPrompt = $"You will be assembling a a Unity Prefab matching the prompt: {prompt}. Given this list of assets, return only a \",\"-deliniated list of which assets you would like to use to build the prefab.  You will then recieve more data on each asset to complete the task. Assets:";





		/*Debug.Log($"Filter Request: {filterPrompt}");
		var filterRes = await AiRequestBackend.OpenAIChatSdk.AskAsync(, filterPrompt);
		Debug.Log(filterRes);

		string buildPrompt = $"You are assembling a Unity Prefab to match this prompt {prompt}.  Return only a list of asset names and their transforms formatted as \"[assetName,pos:(x;y;z),euler:(x;y;z)]\". Be mindful of the sizes of Asset bounds.  Assume that, where relevant, assets face in the positive-z direction.  Assets:";


		Debug.Log($"Build Request: {buildPrompt}");
		var prefabRes = await AiRequestBackend.OpenAIChatSdk.AskAsync(Environment.GetEnvironmentVariable("OPENAI_API_KEY"), buildPrompt);
		Debug.Log(prefabRes);

		FinalResultPrefabBuilder.BuildPrefabFromInstructions(prefabRes);

		Debug.Log("Done!");*/
	}

	// If you want paths (any type), optionally with a filter like "t:Material name:Brick"
	private static string[] GetAssetPathsInFolder(string folderPath, string filter = "")
	{
		return AssetDatabase.FindAssets(filter, new[] { folderPath })
			.Select(AssetDatabase.GUIDToAssetPath)
			.ToArray();
	}
}

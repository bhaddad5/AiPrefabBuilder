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
			"As you go, use the provided tools to inform the user in plaintext of your current reasoning. " +
			"Use the provided tools to request data on the exact size of the sub-parts you would like to know more about (each metadata is ~3 sentances to request a lot). " +
			"Before returning a final value, use the provided tools to try building what you think would look good.  Then anylize the resulting images to see if you need to fix anything.  Do this up to 4 times (or until you are satisfied) to get the best possible result. " +
			"Format your final return string as \"[assetName,pos:(x;y;z),euler:(x;y;z)]\" for each part instance. " +
			"Be mindful of the sizes of the parts, and ensure they all line-up properly in the final prefab. " + 
			"Here is a list of available assets: " +
			assetsStr;

		AiRequestBackend.OpenAIChatSdk.AskContinuous(EditorPrefs.GetString("OPENAI_API_KEY"), fullPrompt, new ToolsImplementation(), AiRequestBackend.OpenAIChatSdk.ModelLevel.mini, 
			(s) => 
			{
				Debug.Log($"[{DateTime.Now}] {s}");
			}, (res) => 
			{
				FinalResultPrefabBuilder.BuildPrefabFromInstructions(res);
				Debug.Log($"[{DateTime.Now}] Complete!");
			});
	}

	private static string[] GetAssetPathsInFolder(string folderPath, string filter = "")
	{
		return AssetDatabase.FindAssets(filter, new[] { folderPath })
			.Select(AssetDatabase.GUIDToAssetPath)
			.ToArray();
	}
}

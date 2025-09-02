using AiRequestBackend;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;

public class ToolsImplementation : IToolsImplementation
{
	const string folder = "Assets/AiPrefabAssembler/Contextualized_Assets";

	public string GetPartMetadata(string part)
	{
		string prefabPath = $"{folder}/{part}.prefab";
		var comp = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)?.GetComponent<AiMetadataFlag>();
		if (comp == null)
		{
			return "";
		}

		var bounds = MetadataRequester.GetBoundsRecursive(comp.gameObject);

		return $"[{part}, metadata:{comp.AiMetadata}, boundsMin:{bounds.min}, boundsMax:{bounds.max}],";
	}

	public (string Info, Dictionary<string, BinaryData> Renders) BuildSubPrefab(string instructions)
	{
		String assetName = Guid.NewGuid().ToString();

		if (!Directory.Exists($"{folder}/SubAssemblies"))
			Directory.CreateDirectory($"{folder}/SubAssemblies");

		string prefabPath = $"{folder}/SubAssemblies/{assetName}.prefab";

		var subPrefabObj = FinalResultPrefabBuilder.BuildPrefabFromInstructions(instructions);

		GameObject prefab = PrefabUtility.SaveAsPrefabAsset(subPrefabObj, prefabPath);

		GameObject.DestroyImmediate(subPrefabObj);

		var res = MetadataRequester.BuildMetadataInfo(prefab);

		string boundsStr = $"AsetName:{assetName}, Bounds(min:{res.Bounds.min},max{res.Bounds.max})";

		return (boundsStr, res.Renders);
	}

	public (string Info, Dictionary<string, BinaryData> Renders) AttemptFinalBuild(string instructions)
	{
		if (!Directory.Exists($"{folder}/FinalBuildAttempts"))
			Directory.CreateDirectory($"{folder}/FinalBuildAttempts");

		int numFinalBuilds = Directory.GetFiles($"{folder}/FinalBuildAttempts").Where(f => f.EndsWith(".prefab")).ToList().Count;

		string prefabPath = $"{folder}/FinalBuildAttempts/Final Build Attempt {numFinalBuilds}.prefab";

		var subPrefabObj = FinalResultPrefabBuilder.BuildPrefabFromInstructions(instructions);

		GameObject prefab = PrefabUtility.SaveAsPrefabAsset(subPrefabObj, prefabPath);

		GameObject.DestroyImmediate(subPrefabObj);

		var res = MetadataRequester.BuildMetadataInfo(prefab);

		string boundsStr = $"Final Build, Bounds(min:{res.Bounds.min},max{res.Bounds.max})";

		return (boundsStr, res.Renders);
	}
}

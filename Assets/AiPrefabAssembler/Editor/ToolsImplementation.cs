using AiRequestBackend;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ToolsImplementation : IToolsImplementation
{
	const string folder = "Assets/AiPrefabAssembler/Contextualized_Assets";

	public (string Info, Dictionary<string, BinaryData> Renders) BuildSubPrefab(string instructions)
	{
		String assetName = Guid.NewGuid().ToString();

		string prefabPath = $"{folder}/SubAssemblies/{assetName}.prefab";

		var subPrefabObj = FinalResultPrefabBuilder.BuildPrefabFromInstructions(instructions);

		GameObject prefab = PrefabUtility.SaveAsPrefabAsset(subPrefabObj, prefabPath);

		GameObject.Destroy(subPrefabObj);

		var res = MetadataRequester.BuildMetadataInfo(prefab);

		string boundsStr = $"AsetName:{assetName}, Bounds(min:{res.Bounds.min},max{res.Bounds.max})";

		return (boundsStr, res.Renders);
	}

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
}

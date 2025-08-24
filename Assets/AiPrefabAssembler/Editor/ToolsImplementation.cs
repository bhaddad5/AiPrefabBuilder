using AiRequestBackend;
using UnityEditor;
using UnityEngine;

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

		return $"[{part}, metadata:{comp.AiMetadata}],";
	}
}

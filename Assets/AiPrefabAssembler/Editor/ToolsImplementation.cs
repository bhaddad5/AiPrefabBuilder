using AiRequestBackend;
using UnityEditor;
using UnityEngine;

public class ToolsImplementation : IToolsImplementation
{
	const string folder = "Assets/AiPrefabAssembler/Contextualized_Assets";

	public string GetPartMetadata(string part)
	{
		string prefabPath = $"{folder}/{part}.prefab";
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
		if (prefab == null)
		{
			return "";
		}

		var bounds = GetPrefabBounds(prefabPath);
		if (bounds == null)
			return "";

		return $"[{part}, width:{bounds.Value.max.x - bounds.Value.min.x}, height:{bounds.Value.max.y - bounds.Value.min.y}, depth:{bounds.Value.max.z - bounds.Value.min.z}],";
	}

	private static Bounds? GetPrefabBounds(string prefabPath)
	{
		// Load prefab asset
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
		if (prefab == null)
		{
			Debug.LogError($"Could not load prefab at path: {prefabPath}");
			return null;
		}

		// Get the MeshCollider on the root
		MeshCollider meshCollider = prefab.GetComponent<MeshCollider>();
		if (meshCollider == null)
		{
			Debug.LogError("Prefab root does not have a MeshCollider.");
			return null;
		}

		// Return collider bounds (local to prefab)
		// In editor, collider.bounds is world-space, so we use sharedMesh.bounds
		return meshCollider.sharedMesh != null ? meshCollider.sharedMesh.bounds : null;
	}
}

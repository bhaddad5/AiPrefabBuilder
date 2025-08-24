using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class Helpers
{
	public static GameObject GetSelectedPrefab()
	{
		var obj = Selection.activeObject as GameObject;
		if (PrefabUtility.GetPrefabAssetType(obj) == PrefabAssetType.NotAPrefab)
		{
			return null;
		}
		return obj;
	}

	public static List<GameObject> GetAllSelectedPrefabs()
	{
		var results = new List<GameObject>();
		var seenPaths = new HashSet<string>();


		// 1) Project selection prefab assets
		foreach (var obj in Selection.GetFiltered<GameObject>(SelectionMode.Assets))
		{
			var path = AssetDatabase.GetAssetPath(obj);
			if (string.IsNullOrEmpty(path)) continue;
			if (!path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase)) continue;

			var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			if (!go) continue;

			var type = PrefabUtility.GetPrefabAssetType(go);
			if (type == PrefabAssetType.NotAPrefab) continue; // safety

			if (seenPaths.Add(path))
				results.Add(go);
		}

		return results;
	}
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling.Memory.Experimental;

public static class PrePopulateMetadata
{
	[MenuItem("AI Prefab Assembly/Add Metadata To Selected Objects", false, 300)]
	public static async void AddMetadataToSelections()
	{
		SetMetadataOnSelections(false);
	}

	[MenuItem("AI Prefab Assembly/Override Metadata on Selected Objects", false, 300)]
	public static async void OverrideMetadataOnSelections()
	{
		SetMetadataOnSelections(true);
	}

	private static async void SetMetadataOnSelections(bool overrideValues)
	{
		var prefabs = GetAllSelectedPrefabs();
		if (prefabs.Count == 0)
		{
			Debug.LogError("No prefabs selected!");
			return;
		}

		Debug.Log($"Generating AI Metadata for {prefabs.Count} prefabs.  This might take awhile...");
		for (int i = 0; i < prefabs.Count; i++)
		{
			var flag = prefabs[i].GetComponent<AiMetadataFlag>();
			if (flag == null)
				flag = prefabs[i].AddComponent<AiMetadataFlag>();

			if (!overrideValues && !String.IsNullOrWhiteSpace(flag.AiMetadata))
			{
				Debug.Log($"Skipping {prefabs[i].name} ({i + 1}/{prefabs.Count}) because it already has Metadata.");
				continue;
			}

			Debug.Log($"Processing {prefabs[i].name} ({i + 1}/{prefabs.Count})...");

			var metadata = await MetadataRequester.GeneratePrefabMetadata(prefabs[i]);

			flag.AiMetadata = metadata;

			Debug.Log($"Completed Metadata on {prefabs[i].name}!");
		}

		Debug.Log("Done!");
	}

	private static List<GameObject> GetAllSelectedPrefabs()
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

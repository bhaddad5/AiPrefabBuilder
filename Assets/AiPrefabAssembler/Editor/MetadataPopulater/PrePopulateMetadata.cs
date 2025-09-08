using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling.Memory.Experimental;

public static class PrePopulateMetadata
{
	[MenuItem("Forge of Realms/Add Metadata To Selected Objects", false, 300)]
	public static void AddMetadataToSelections()
	{
		SetMetadataOnSelections(false);
	}

	[MenuItem("Forge of Realms/Override Metadata on Selected Objects", false, 300)]
	public static void OverrideMetadataOnSelections()
	{
		SetMetadataOnSelections(true);
	}

	[MenuItem("Forge of Realms/Clear All Metadata", false, 303)]
	public static void ClearAllMetadata()
	{
		var prefabs = Helpers.GetAllSelectedPrefabs();
		if (prefabs.Count == 0)
		{
			Debug.LogError("No prefabs selected!");
			return;
		}

		foreach(var p in prefabs)
		{
			var comp = p.GetComponent<AiMetadataFlag>();
			if (comp)
			{
				AiMetadataFlag.DestroyImmediate(comp, true);
				EditorUtility.SetDirty(p);
			}
		}
	}

	private static async void SetMetadataOnSelections(bool overrideValues)
	{
		var prefabs = Helpers.GetAllSelectedPrefabs();
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

			EditorUtility.SetDirty(prefabs[i]);

			Debug.Log($"Completed Metadata on {prefabs[i].name}!");
		}

		Debug.Log("Done!");
	}
}

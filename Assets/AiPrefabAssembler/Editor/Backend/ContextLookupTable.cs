using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ContextLookupTable
{
	public List<(string name, int id)> ObjectNames = new List<(string name, int id)>();


	public Dictionary<string, AiMetadataFlag> PrefabContextLookup = new Dictionary<string, AiMetadataFlag>();
	public List<(string id, List<string> tags)> PrefabContextInfoTags = new List<(string id, List<string> tags)>();

	public ContextLookupTable()
    {
		Rebuild();
	}

    public void Rebuild()
    {
		FindAllSceneObjects();
		FindAllFlaggedPrefabs();
	}

	public void FindAllSceneObjects()
	{
		var scene = SceneManager.GetActiveScene();
		if (!scene.IsValid() || !scene.isLoaded)
		{
			Debug.LogError("No active scene loaded.");
			return;
		}

		var roots = scene.GetRootGameObjects();

		foreach (var root in roots)
			CheckNodeRecursive(root);
	}

	private void CheckNodeRecursive(GameObject g)
	{
		ObjectNames.Add((g.name, g.GetInstanceID()));

		// Children in deterministic order
		for (int i = 0; i < g.transform.childCount; i++)
			CheckNodeRecursive(g.transform.GetChild(i).gameObject);
	}

	public void FindAllFlaggedPrefabs()
	{
		List<string> res = new List<string>();

		string[] prefabGuids = AssetDatabase.FindAssets("t:prefab");

		for (int i = 0; i < prefabGuids.Length; i++)
		{
			string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
			if (string.IsNullOrEmpty(prefabPath))
				continue;

			var obj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
			if (obj == null)
			{
				Debug.LogError($"Failed to find Prefab at path: {prefabPath}");
				continue;
			}

			var flag = obj.GetComponent<AiMetadataFlag>();
			if (flag != null)
			{
				PrefabContextLookup[prefabPath] = flag;
				PrefabContextInfoTags.Add((prefabPath, flag.AiMetadataTags));
			}
		}
	}

	public List<string> SearchPrefabTags(List<string> tags)
	{
		var matches = ContextLookupHelpers.TopMatches(tags, PrefabContextInfoTags);

		List<string> res = new List<string>();
		foreach (var match in matches)
			res.Add(match.id);

		return res;
	}

	public List<int> SearchObjectNames(string searchString)
	{
		var matches = ContextLookupHelpers.TopMatches(searchString, ObjectNames);

		List<int> res = new List<int>();
		foreach (var match in matches)
		{
			Debug.Log(match.id + ", " + match.score);
			res.Add(match.id);
		}

		return res;
	}
}


using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ContextLookupTable
{
	public Dictionary<string, AiMetadataFlag> ContextLookup = new Dictionary<string, AiMetadataFlag>();
	//public List<(string title, string description, string id)> ContextInfo = new List<(string title, string description, string id)>();
	public List<(string id, List<string> tags)> ContextInfoTags = new List<(string id, List<string> tags)>();

	public ContextLookupTable()
    {
		Rebuild();
	}

    public void Rebuild()
    {
		FindAllFlaggedPrefabs();
	}

	public void FindAllFlaggedPrefabs()
	{
		List<string> res = new List<string>();

		string[] prefabGuids = AssetDatabase.FindAssets("t:prefab");

		foreach (var guid in prefabGuids)
		{
			string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
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
				ContextLookup[prefabPath] = flag;
				//ContextInfo.Add((flag.AiMetadataTitle, flag.AiMetadataSummary, prefabPath));
				ContextInfoTags.Add((prefabPath, flag.AiMetadataTags));
			}
		}
	}

	public List<string> SearchTags(List<string> tags, int count)
	{
		var matches = ContextLookupHelpers.TopMatches(tags, ContextInfoTags, count);

		List<string> res = new List<string>();
		foreach (var match in matches)
		{
			res.Add(match.id);
		}

		return res;
	}
}


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SearchHelpers
{
	public static Parameter Top25Param => new Parameter("resultsIndex25", Parameter.ParamType.Int, "Index-0 identifier of which 25 results you would like.  So pass in 0 for results 1-25, 1 for 26-50, etc.", false);

	public static (List<string> results, int startIndex, int endIndex) FilterOn25Index(int index, List<string> inputs)
	{
		int desiredStartIndex = index * 25;
		if (desiredStartIndex >= inputs.Count)
			desiredStartIndex = 0;
		int endIndex = Math.Min(desiredStartIndex + 25, inputs.Count);

		List<string> res = new List<string>();
		for (int i = desiredStartIndex; i < endIndex; i++)
		{
			res.Add(inputs[i]);
		}

		return (res, desiredStartIndex, endIndex-1);
	}
}

public class GetPrefabContextCommand : ICommand
{
	public string CommandName => "GetPrefabContext";

	public string CommandDescription => "Request Context for a specific prefab. Returns the name, bounds, and a short description if one exists.";

	public List<Parameter> Parameters => new List<Parameter>() { new Parameter("prefabPath") };

	public bool EndConversation => false;

	public List<UserToAiMsg> ParseArgsAndExecute(TypedArgs args)
	{
		string prefabPath = Parameters[0].Get<string>(args);

		return new List<UserToAiMsg>() { new UserToAiMsgText(GetPrefabContext(prefabPath)) };
	}

	public static string GetPrefabContext(string prefabPath)
	{
		var obj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
		if (obj == null)
		{
			Debug.LogError($"Failed to find Prefab at path: {prefabPath}");
			return "";
		}

		var bounds = Helpers.GetCombinedLocalBounds(obj.transform);

		var comp = obj.GetComponent<AiMetadataFlag>();
		if (comp == null)
		{
			return $"[{prefabPath}, boundsMin:{bounds.min}, boundsMax:{bounds.max}],"; ;
		}

		return $"[{prefabPath}, boundsMin:{bounds.min}, boundsMax:{bounds.max}, description:\"{comp.AiMetadataDescription}\"],";
	}
}

public class SearchPrefabsWithTagsCommand : ICommand
{
	public string CommandName => "SearchPrefabsWithTags";

	public string CommandDescription => "Returns a list of the top-25 prefabs that best match the provided tags.";

	public bool EndConversation => false;

	public List<Parameter> Parameters => new List<Parameter>() { new Parameter("tags", Parameter.ParamType.StringList), SearchHelpers.Top25Param };

	private ContextLookupTable lookupTable;
	public SearchPrefabsWithTagsCommand(ContextLookupTable lookupTable)
	{
		this.lookupTable = lookupTable;
	}

	public List<UserToAiMsg> ParseArgsAndExecute(TypedArgs args)
	{
		List<string> tags = Parameters[0].Get<List<string>>(args);
		int index = Parameters[1].Get<int>(args);

		var foundItems = lookupTable.SearchPrefabTags(tags);

		var filter = SearchHelpers.FilterOn25Index(index, foundItems);

		if (filter.results.Count == 0)
			return new List<UserToAiMsg>() { new UserToAiMsgText($"No prefabs found.") };

		return new List<UserToAiMsg>() { new UserToAiMsgText($"Found {foundItems.Count} prefabs.  ({filter.startIndex},{filter.endIndex})={string.Join(',', filter.results)}") };
	}
}

public class GetAllPrefabTagsCommand : ICommand
{
	public string CommandName => "GetAllPrefabTags";

	public string CommandDescription => "Get a list of all Tags that have currently been assigned to Prefabs.";

	public bool EndConversation => false;

	public List<Parameter> Parameters => new List<Parameter>() { };

	public List<UserToAiMsg> ParseArgsAndExecute(TypedArgs args)
	{
		return new List<UserToAiMsg>() { new UserToAiMsgText(string.Join(',', GetAllTagsInFileSystem())) };
	}

	public static List<string> GetAllTagsInFileSystem()
	{
		HashSet<string> res = new HashSet<string>();

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
				foreach (var tag in flag.AiMetadataTags)
				{
					if(!res.Contains(tag))
						res.Add(tag);
				}
			}
		}

		return res.ToList();
	}
}

public class GetObjectContextCommand : ICommand
{
	public string CommandName => "GetObjectContext";

	public string CommandDescription => "Request Context for a specific Object. Returns the name, bounds, children, etc.";

	public bool EndConversation => false;

	public List<Parameter> Parameters => new List<Parameter>() { new Parameter("objectUniqueId", Parameter.ParamType.Int) };

	public List<UserToAiMsg> ParseArgsAndExecute(TypedArgs args)
	{
		var objectUniqueId = Parameters[0].Get<int>(args);

		return new List<UserToAiMsg>() { new UserToAiMsgText(GetObjectContext(objectUniqueId)) };
	}

	private static string GetObjectContext(int objectUniqueId)
	{
		GameObject obj = SessionHelpers.LookUpObjectById(objectUniqueId);

		if (obj == null)
			return "";

		return SceneDescriptionBuilder.BuildGameObjectDescription(obj.transform);
	}
}

public class SearchObjectsContextCommand : ICommand
{
	public string CommandName => "SearchObjectsByString";

	public string CommandDescription => "Search for object uniqueIds in the Scene using the given String.";

	public bool EndConversation => false;

	public List<Parameter> Parameters => new List<Parameter>() { new Parameter("searchString"), SearchHelpers.Top25Param };

	private ContextLookupTable lookupTable;
	public SearchObjectsContextCommand(ContextLookupTable lookupTable)
	{
		this.lookupTable = lookupTable;
	}

	public List<UserToAiMsg> ParseArgsAndExecute(TypedArgs args)
	{
		var searchString = Parameters[0].Get<string>(args);
		int index = Parameters[1].Get<int>(args);

		List<string> foundItems = new List<string>();
		var foundIds = lookupTable.SearchObjectNames(searchString);
		foreach (var id in foundIds)
			foundItems.Add(id.ToString());

		var filter = SearchHelpers.FilterOn25Index(index, foundItems);

		if (filter.results.Count == 0)
			return new List<UserToAiMsg>() { new UserToAiMsgText($"No objects found.") };

		return new List<UserToAiMsg>() { new UserToAiMsgText($"Found {foundItems.Count} objects.  ({filter.startIndex},{filter.endIndex})={string.Join(',', filter.results)}") };
	}
}
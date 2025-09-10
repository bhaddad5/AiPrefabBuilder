using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GetPrefabContextCommand : ICommand
{
	public string CommandName => "GetPrefabContext";

	public string CommandDescription => "Request Context for a specific prefab. Returns the name, bounds, and a short description if one exists.";

	public List<Parameter> Parameters => new List<Parameter>() { new Parameter("prefabPath") };

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

		return $"[{prefabPath}, boundsMin:{bounds.min}, boundsMax:{bounds.max}, metadata:\"{comp.AiMetadata}\"],";
	}
}

public class SearchPrefabsContextCommand : ICommand
{
	public string CommandName => "SearchPrefabsContext";

	public string CommandDescription => "Search for prefabs and their context info in the Assets folder using the given String.";

	public List<Parameter> Parameters => new List<Parameter>() { new Parameter("searchString") };

	public List<UserToAiMsg> ParseArgsAndExecute(TypedArgs args)
	{
		string searchString = Parameters[0].Get<string>(args);

		return new List<UserToAiMsg>() { new UserToAiMsgText(SearchForPrefabsContext(searchString)) };
	}

	private static string SearchForPrefabsContext(string searchString)
	{
		List<string> foundItems = new List<string>();
		SearchThroughFileSystem(searchString.ToLowerInvariant(), foundItems);

		StringBuilder sb = new StringBuilder();
		foreach (var item in foundItems)
		{
			sb.Append(GetPrefabContextCommand.GetPrefabContext(item));
		}

		string res = sb.ToString();

		if (res == "")
			res = "No objects found.";

		return res;
	}

	public static void SearchThroughFileSystem(string searchString, List<string> foundItems)
	{
		string[] prefabGuids = AssetDatabase.FindAssets("t:prefab");

		foreach (var guid in prefabGuids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			if (string.IsNullOrEmpty(path)) 
				continue;

			// File name check (fast)
			string fileNameNoExt = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

			bool match = fileNameNoExt?.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0;

			if (match)
				foundItems.Add(path);
		}
	}
}

public class GetObjectContextCommand : ICommand
{
	public string CommandName => "GetObjectContext";

	public string CommandDescription => "Request Context for a specific Object. Returns the name, bounds, children, etc.";

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
	public string CommandName => "SearchObjectsContext";

	public string CommandDescription => "Search for objects and their context info in the Scene using the given String.";

	public List<Parameter> Parameters => new List<Parameter>() { new Parameter("searchString") };

	public List<UserToAiMsg> ParseArgsAndExecute(TypedArgs args)
	{
		var searchString = Parameters[0].Get<string>(args);

		return new List<UserToAiMsg>() { new UserToAiMsgText(SearchForObjectsContext(searchString)) };
	}

	private static string SearchForObjectsContext(string searchString)
	{
		List<GameObject> foundItems = new List<GameObject>();
		SearchThroughWholeScene(searchString.ToLowerInvariant(), foundItems);

		StringBuilder sb = new StringBuilder();
		foreach(var item in foundItems)
		{
			sb.Append(SceneDescriptionBuilder.BuildGameObjectDescription(item.transform));
		}

		string res = sb.ToString();

		if (res == "")
			res = "No objects found.";

		return res;
	}

	public static void SearchThroughWholeScene(string searchString, List<GameObject> foundItems)
	{
		var scene = SceneManager.GetActiveScene();
		if (!scene.IsValid() || !scene.isLoaded)
		{
			Debug.LogError("No active scene loaded.");
			return;
		}

		var roots = scene.GetRootGameObjects();

		foreach (var root in roots)
			CheckNodeRecursive(root, searchString, foundItems);
	}

	private static void CheckNodeRecursive(GameObject g, string searchString, List<GameObject> foundItems)
	{
		if (g.name.ToLowerInvariant().Contains(searchString))
			foundItems.Add(g);

		// Children in deterministic order
		for (int i = 0; i < g.transform.childCount; i++)
			CheckNodeRecursive(g.transform.GetChild(i).gameObject, searchString, foundItems);
	}
}
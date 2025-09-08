using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GetPrefabContextCommand : ICommand
{
	public string CommandName => "GetPrefabContext";

	public string CommandDescription => "Request Context for a specific prefab. Returns the name, bounds, and a short description if one exists.";

	public List<Parameter> Parameters => new List<Parameter>() { new Parameter("prefabPath") };

	public string ParseArgsAndExecute(Dictionary<string, string> args)
	{
		string prefabPath = Parameters[0].GetParameter(args);

		return GetPrefabContext(prefabPath);
	}

	private static string GetPrefabContext(string prefabPath)
	{
		var obj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
		if (obj == null)
		{
			Debug.LogError($"Failed to find Prefab at path: {prefabPath}");
			return "";
		}

		var bounds = MetadataRequester.GetCombinedLocalBounds(obj.transform);

		var comp = obj.GetComponent<AiMetadataFlag>();
		if (comp == null)
		{
			return $"[{prefabPath}, boundsMin{bounds.min}, boundsMax{bounds.max}],"; ;
		}

		return $"[{prefabPath}, boundsMin{bounds.min}, boundsMax{bounds.max}, metadata:\"{comp.AiMetadata}\"],";
	}
}

public class GetObjectContextCommand : ICommand
{
	public string CommandName => "GetObjectContext";

	public string CommandDescription => "Request Context for a specific Object. Returns the name, bounds, children, etc.";

	public List<Parameter> Parameters => new List<Parameter>() { new Parameter("objectUniqueId") };

	public string ParseArgsAndExecute(Dictionary<string, string> args)
	{
		string objectUniqueId = Parameters[0].GetParameter(args);

		return GetObjectContext(objectUniqueId);
	}

	private static string GetObjectContext(string objectUniqueId)
	{
		GameObject obj = SessionHelpers.LookUpObjectById(objectUniqueId);

		if (obj == null)
			return "";

		return SceneDescriptionBuilder.BuildGameObjectDescription(obj.transform);
	}
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GetPrefabContextCommand : ICommand
{
	public string CommandName => "GetPrefabContext";

	public string CommandFormattingString => $"{CommandName}[prefabPath]";

	public int NumArgs => 1;

	public string ParseArgsAndExecute(List<string> args)
	{
		string prefabPath = args[0];

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

	public string CommandFormattingString => $"{CommandName}[objectUniqueId]";

	public int NumArgs => 1;

	public string ParseArgsAndExecute(List<string> args)
	{
		string objectUniqueId = args[0];

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
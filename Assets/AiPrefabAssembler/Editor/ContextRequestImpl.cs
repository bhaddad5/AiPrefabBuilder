using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ContextRequestImpl
{
    public static string GetPrefabContext(string prefabPath)
    {
		var obj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
		if (obj == null)
		{
			Debug.LogError($"Failed to find Prefab at path: {prefabPath}");
			return "";
		}

		var bounds = MetadataRequester.GetBoundsRecursive(obj) ?? new Bounds();

		var comp = obj.GetComponent<AiMetadataFlag>();
		if (comp == null)
		{
			return $"[{prefabPath}, boundsMin:{bounds.min}, boundsMax:{bounds.max}],"; ;
		}

		return $"[{prefabPath}, metadata:{comp.AiMetadata}, boundsMin:{bounds.min}, boundsMax:{bounds.max}],";
	}

	public static string GetObjectContext(string objectUniqueId)
	{
		GameObject obj = LookUpObjectById(objectUniqueId);

		if (obj == null)
			return "";

		return SceneDescriptionBuilder.BuildGameObjectDescription(obj.transform);
	}

	private static GameObject LookUpObjectById(string id)
	{
		int instanceId = 0;

		if (!Int32.TryParse(id, out instanceId))
		{
			Debug.LogError("Failed to parse InstanceId " + id);
			return null;
		}

		var obj = EditorUtility.InstanceIDToObject(instanceId) as GameObject;

		if (obj == null)
		{
			Debug.LogError("Failed to find GameObject with InstanceId " + id);
			return null;
		}

		return obj;
	}
}

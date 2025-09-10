using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SessionContext
{
	public static Dictionary<int, GameObject> CreatedAssetsLookup = new Dictionary<int, GameObject>();
}

public static class SessionHelpers
{
	public static GameObject LookUpObjectById(int id)
	{
		if (SessionContext.CreatedAssetsLookup.ContainsKey(id))
			return SessionContext.CreatedAssetsLookup[id];

		var obj = EditorUtility.InstanceIDToObject(id) as GameObject;

		if (obj == null)
		{
			Debug.LogError("Failed to find GameObject with InstanceId " + id);
			return null;
		}

		return obj;
	}
}
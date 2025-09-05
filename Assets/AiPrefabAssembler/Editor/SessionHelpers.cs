using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SessionContext
{
	public static Dictionary<string, GameObject> CreatedAssetsLookup = new Dictionary<string, GameObject>();
}

public static class SessionHelpers
{
	public static GameObject LookUpObjectById(string id)
	{
		//Sometimes the AI like to include the parameter name
		if (id.Contains(':'))
			id = id.Substring(id.IndexOf(':') + 1);

		if (SessionContext.CreatedAssetsLookup.ContainsKey(id))
			return SessionContext.CreatedAssetsLookup[id];

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
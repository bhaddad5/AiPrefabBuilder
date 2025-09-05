using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AiCommandImpl
{
    private Dictionary<string, GameObject> CreatedAssetsLookup = new Dictionary<string, GameObject>();

    public void CreateObject(string creationId, string prefabPath, string newObjectName, Vector3 pos, Vector3 rot, Vector3 scl, string optionalParentUniqueId)
    {
		var asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

		if (asset == null)
		{
			Debug.LogError($"Failed to find prefab {prefabPath}");
			return;
		}

        Transform parent = null;
		if (optionalParentUniqueId != "")
			parent = LookUpObjectById(optionalParentUniqueId)?.transform;

		var obj = GameObject.Instantiate(asset, parent);
        obj.name = newObjectName;
        obj.transform.localPosition = pos;
        obj.transform.localEulerAngles = rot;
        obj.transform.localScale = scl;

        CreatedAssetsLookup[creationId] = obj;
	}

    public void DeleteObject(string objectUniqueId)
    {
        var obj = LookUpObjectById(objectUniqueId);
        if (obj != null)
            GameObject.DestroyImmediate(obj);
    }

    public void SetObjectParent(string objectUniqueId, string parentObjectUniqueId)
    {
		var obj = LookUpObjectById(objectUniqueId);

		var parent = LookUpObjectById(parentObjectUniqueId);

        if (obj != null && parent != null)
            obj.transform.SetParent(parent.transform);
	}

    public void SetObjectTransform(string objectUniqueId, Vector3 pos, Vector3 rot, Vector3 scl)
    {
		var obj = LookUpObjectById(objectUniqueId);

        if(obj != null)
        {
			obj.transform.localPosition = pos;
			obj.transform.localEulerAngles = rot;
			obj.transform.localScale = scl;
		}
	}

    private GameObject LookUpObjectById(string id)
    {
        if (CreatedAssetsLookup.ContainsKey(id))
            return CreatedAssetsLookup[id];

        int instanceId = 0;

        if(!Int32.TryParse(id, out instanceId))
        {
            Debug.LogError("Failed to parse InstanceId " + id);
            return null;
        }

        var obj = EditorUtility.InstanceIDToObject(instanceId) as GameObject;

        if(obj == null)
        {
			Debug.LogError("Failed to find GameObject with InstanceId " + id);
			return null;
		}

        return obj;
	}
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CreateObjectCommand : ICommand
{
	public string CommandName => "CreateObject";

	public string CommandFormattingString => $"{CommandName}[objectCreationUniqueId,prefabPath,newObjectName,localPos:(x;y;z),localEuler:(x;y;z),localScale:(x;y;z),parentUniqueId]. *If making a new GameObject with no prefab leave prefabPath blank, if you want it under the root leave parentUniqueId blank,  but leave the commas in both cases.";

	public int NumArgs => 7;

	public string ParseArgsAndExecute(List<string> args)
	{
		string creationId = args[0];
		string prefabPath = args[1];
		string newObjectName = args[2];
		Vector3 pos = CommandHelpers.ParseVec3(args[3]);
		Vector3 rot = CommandHelpers.ParseVec3(args[4]);
		Vector3 scl = CommandHelpers.ParseVec3(args[5]);
		string parentId = args[6];

		CreateObject(creationId, prefabPath, newObjectName, pos, rot, scl, parentId);

		return "";
	}

	public static void CreateObject(string creationId, string prefabPath, string newObjectName, Vector3 pos, Vector3 rot, Vector3 scl, string optionalParentUniqueId)
	{
		var asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

		if (asset == null)
		{
			Debug.LogError($"Failed to find prefab {prefabPath}");
			return;
		}

		Transform parent = null;
		if (optionalParentUniqueId != "")
			parent = SessionHelpers.LookUpObjectById(optionalParentUniqueId)?.transform;

		var obj = GameObject.Instantiate(asset, parent);
		obj.name = newObjectName;
		obj.transform.localPosition = pos;
		obj.transform.localEulerAngles = rot;
		obj.transform.localScale = scl;

		SessionContext.CreatedAssetsLookup[creationId] = obj;
	}
}

public class DeleteObjectCommand : ICommand
{
	public string CommandName => "DeleteObject";

	public string CommandFormattingString => $"{CommandName}[objectUniqueId]";

	public int NumArgs => 1;

	public string ParseArgsAndExecute(List<string> args)
	{
		string objectUniqueId = args[0];

		DeleteObject(objectUniqueId);

		return "";
	}

	public static void DeleteObject(string objectUniqueId)
	{
		var obj = SessionHelpers.LookUpObjectById(objectUniqueId);
		if (obj != null)
			GameObject.DestroyImmediate(obj);
	}
}

public class SetObjectParentCommand : ICommand
{
	public string CommandName => "SetObjectParent";

	public string CommandFormattingString => $"{CommandName}[objectUniqueId,parentObjectUniqueId]";

	public int NumArgs => 2;

	public string ParseArgsAndExecute(List<string> args)
	{
		string objectUniqueId = args[0];
		string parentObjectUniqueId = args[1];

		SetObjectParent(objectUniqueId, parentObjectUniqueId);

		return "";
	}

	public static void SetObjectParent(string objectUniqueId, string parentObjectUniqueId)
	{
		var obj = SessionHelpers.LookUpObjectById(objectUniqueId);

		var parent = SessionHelpers.LookUpObjectById(parentObjectUniqueId);

		if (obj != null && parent != null)
			obj.transform.SetParent(parent.transform);
	}
}

public class SetObjectTransformCommand : ICommand
{
	public string CommandName => "SetObjectTransform";

	public string CommandFormattingString => $"{CommandName}[objectUniqueId,localPos:(x;y;z),localEuler:(x;y;z),localScale(x;y;z)]";

	public int NumArgs => 4;

	public string ParseArgsAndExecute(List<string> args)
	{
		string objectUniqueId = args[0];
		Vector3 pos = CommandHelpers.ParseVec3(args[1]);
		Vector3 rot = CommandHelpers.ParseVec3(args[2]);
		Vector3 scl = CommandHelpers.ParseVec3(args[3]);

		SetObjectTransform(objectUniqueId, pos, rot, scl);

		return "";
	}

	public static void SetObjectTransform(string objectUniqueId, Vector3 pos, Vector3 rot, Vector3 scl)
	{
		var obj = SessionHelpers.LookUpObjectById(objectUniqueId);

		if (obj != null)
		{
			obj.transform.localPosition = pos;
			obj.transform.localEulerAngles = rot;
			obj.transform.localScale = scl;
		}
	}
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CreateObjectCommand : ICommand
{
	public string CommandName => "CreateObject";

	public string CommandDescription => "Creates a new Object.";

	public List<Parameter> Parameters => new List<Parameter>() { 
		new Parameter("objectCreationUniqueId"), 
		new Parameter("prefabPath"), 
		new Parameter("newObjectName"), 
		new Parameter("localPos", "A Vector3 formatted as (x;y;z)"), 
		new Parameter("localEuler", "A Vector3 formatted as (x;y;z)"),
		new Parameter("localScale", "A Vector3 formatted as (x;y;z)"), 
		new Parameter("parentUniqueId", "Don't include if you want to place the object under the scene root", false) };

	public string ParseArgsAndExecute(Dictionary<string, string> args)
	{
		string creationId = Parameters[0].GetParameter(args);
		string prefabPath = Parameters[1].GetParameter(args);
		string newObjectName = Parameters[2].GetParameter(args);
		Vector3 pos = Parameters[3].GetParameterAsVec3(args);
		Vector3 rot = Parameters[4].GetParameterAsVec3(args);
		Vector3 scl = Parameters[5].GetParameterAsVec3(args);
		string parentId = Parameters[6].GetParameter(args);

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

	public string CommandDescription => "Delete an Object.";

	public List<Parameter> Parameters => new List<Parameter>() { new Parameter("objectUniqueId") };

	public string ParseArgsAndExecute(Dictionary<string, string> args)
	{
		string objectUniqueId = Parameters[0].GetParameter(args);

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

	public string CommandDescription => "Set an Object's Parent.";

	public List<Parameter> Parameters => new List<Parameter>()	{ 
		new Parameter("objectUniqueId"),
		new Parameter("parentObjectUniqueId"),
	};

	public string CommandFormattingString => $"{CommandName}[objectUniqueId,parentObjectUniqueId]";

	public int NumArgs => 2;

	public string ParseArgsAndExecute(Dictionary<string, string> args)
	{
		string objectUniqueId = Parameters[0].GetParameter(args);
		string parentObjectUniqueId = Parameters[1].GetParameter(args);

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

	public string CommandDescription => "Set an Object's Transform.";

	public List<Parameter> Parameters => new List<Parameter>() { 
		new Parameter("objectUniqueId"),
		new Parameter("localPos", "A Vector3 formatted as (x;y;z)"),
		new Parameter("localEuler", "A Vector3 formatted as (x;y;z)"), 
		new Parameter("localScale", "A Vector3 formatted as (x;y;z)") };

	public string ParseArgsAndExecute(Dictionary<string, string> args)
	{
		string objectUniqueId = Parameters[0].GetParameter(args);
		Vector3 pos = Parameters[1].GetParameterAsVec3(args);
		Vector3 rot = Parameters[2].GetParameterAsVec3(args);
		Vector3 scl = Parameters[3].GetParameterAsVec3(args);

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
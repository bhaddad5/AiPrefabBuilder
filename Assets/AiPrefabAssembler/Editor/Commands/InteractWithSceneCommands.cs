using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CreateObjectCommand : ICommand
{
	public string CommandName => "CreateObject";

	public string CommandDescription => "Creates a new Object.";

	public List<Parameter> Parameters => new List<Parameter>() { 
		new Parameter("objectCreationUniqueId", Parameter.ParamType.Int), 
		new Parameter("prefabPath"), 
		new Parameter("newObjectName"), 
		new Parameter("localPos", Parameter.ParamType.Vector3, "A Vector3 formatted as (x;y;z)"), 
		new Parameter("localEuler", Parameter.ParamType.Vector3, "A Vector3 formatted as (x;y;z)"),
		new Parameter("localScale", Parameter.ParamType.Vector3, "A Vector3 formatted as (x;y;z)"), 
		new Parameter("parentUniqueId", Parameter.ParamType.Int, "Don't include if you want to place the object under the scene root", false) };

	public List<UserToAiMsg> ParseArgsAndExecute(TypedArgs args)
	{
		int creationId = Parameters[0].Get<int>(args);
		string prefabPath = Parameters[1].Get<string>(args);
		string newObjectName = Parameters[2].Get<string>(args);
		Vector3 pos = Parameters[3].Get<Vector3>(args);
		Vector3 rot = Parameters[4].Get<Vector3>(args);
		Vector3 scl = Parameters[5].Get<Vector3>(args);
		int parentId = Parameters[6].Get<int>(args);

		CreateObject(creationId, prefabPath, newObjectName, pos, rot, scl, parentId);

		return new List<UserToAiMsg>();
	}

	public static void CreateObject(int creationId, string prefabPath, string newObjectName, Vector3 pos, Vector3 rot, Vector3 scl, int optionalParentUniqueId)
	{
		var asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

		if (asset == null)
		{
			Debug.LogError($"Failed to find prefab {prefabPath}");
			return;
		}

		Transform parent = null;
		if (optionalParentUniqueId != 0)
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

	public List<Parameter> Parameters => new List<Parameter>() { new Parameter("objectUniqueId", Parameter.ParamType.Int) };

	public List<UserToAiMsg> ParseArgsAndExecute(TypedArgs args)
	{
		int objectUniqueId = Parameters[0].Get<int>(args);

		DeleteObject(objectUniqueId);

		return new List<UserToAiMsg>();
	}

	public static void DeleteObject(int objectUniqueId)
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
		new Parameter("objectUniqueId", Parameter.ParamType.Int),
		new Parameter("parentObjectUniqueId", Parameter.ParamType.Int),
	};

	public List<UserToAiMsg> ParseArgsAndExecute(TypedArgs args)
	{
		int objectUniqueId = Parameters[0].Get<int>(args);
		int parentObjectUniqueId = Parameters[1].Get<int>(args);
		SetObjectParent(objectUniqueId, parentObjectUniqueId);

		return new List<UserToAiMsg>();
	}

	public static void SetObjectParent(int objectUniqueId, int parentObjectUniqueId)
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
		new Parameter("objectUniqueId", Parameter.ParamType.Int),
		new Parameter("localPos", Parameter.ParamType.Vector3, "A Vector3 formatted as (x;y;z)"),
		new Parameter("localEuler", Parameter.ParamType.Vector3, "A Vector3 formatted as (x;y;z)"), 
		new Parameter("localScale", Parameter.ParamType.Vector3, "A Vector3 formatted as (x;y;z)") };

	public List<UserToAiMsg> ParseArgsAndExecute(TypedArgs args)
	{
		int objectUniqueId = Parameters[0].Get<int>(args);
		Vector3 pos = Parameters[1].Get<Vector3>(args	);
		Vector3 rot = Parameters[2].Get<Vector3>(args);
		Vector3 scl = Parameters[3].Get<Vector3>(args);

		SetObjectTransform(objectUniqueId, pos, rot, scl);

		return new List<UserToAiMsg>();
	}

	public static void SetObjectTransform(int objectUniqueId, Vector3 pos, Vector3 rot, Vector3 scl)
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
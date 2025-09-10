using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;
using UnityEngine.Profiling.Memory.Experimental;

public class RequestPrefabRendersCommand : ICommand
{
	public string CommandName => "RequestPrefabRenders";

	public string CommandDescription => "Request low-resolution renderings of all sides of a prefab. " +
		"After you have done this and analyzed the images, it may be wise to call AssignMetadataToPrefab with a brief 2-sentance summary of what the prefab looks like. " +
		"This will cache that information for you to use later.";

	public List<Parameter> Parameters => new List<Parameter>() { new Parameter("prefabPath") };

	public List<UserToAiMsg> ParseArgsAndExecute(TypedArgs args)
	{
		string prefabPath = Parameters[0].Get<string>(args);

		var renders = GetPrefabRenders(prefabPath);

		var res = new List<UserToAiMsg>();

		if (renders.Bounds.HasValue)
			res.Add(new UserToAiMsgText($"boundsMin{renders.Bounds.Value.min}, boundsMax{renders.Bounds.Value.max}"));

		foreach(var r in renders.Renders)
		{
			res.Add(new UserToAiMsgText(r.Key));
			res.Add(new UserToAiMsgImage(r.Value));
		}

		return res;
	}

	public static (Bounds? Bounds, Dictionary<string, Texture2D> Renders) GetPrefabRenders(string prefabPath)
	{
		var obj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
		if (obj == null)
		{
			Debug.LogError($"Failed to find Prefab at path: {prefabPath}");
			return (null, new Dictionary<string, Texture2D>());
		}

		var bounds = Helpers.GetCombinedLocalBounds(obj.transform);

		Dictionary<string, Texture2D> six = TextureRenderer.RenderAllSides(obj);

		return (bounds, six);
	}
}

public class AssignDescriptionToPrefabCommand : ICommand
{
	public string CommandName => "AssignMetadataToPrefab";

	public string CommandDescription => "Assign a text-description to a well-known component on the selected prefab that you can use to understand the prefab for future tasks. "+
		"Generally call this using the information you have inferred from RequestPrefabRenders, unless otherwise instructed. " +
		"Keep your description to 2-sentances.";

	public List<Parameter> Parameters => new List<Parameter>() { new Parameter("prefabPath"), new Parameter("description") };

	public List<UserToAiMsg> ParseArgsAndExecute(TypedArgs args)
	{
		string prefabPath = Parameters[0].Get<string>(args);
		string description = Parameters[1].Get<string>(args);

		AssignToPrefab(prefabPath, description);

		return new List<UserToAiMsg>();
	}

	private void AssignToPrefab(string prefabPath, string description)
	{
		var obj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
		if (obj == null)
		{
			Debug.LogError($"Failed to find Prefab at path: {prefabPath}");
			return;
		}

		var flag = obj.GetComponent<AiMetadataFlag>();
		if (flag == null)
			flag = obj.AddComponent<AiMetadataFlag>();

		flag.AiMetadata = description;
		EditorUtility.SetDirty(obj);

	}
}
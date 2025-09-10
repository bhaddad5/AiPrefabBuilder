using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RequestRendersForPrefabDescriptionCommand : ICommand
{
	public string CommandName => "RequestRendersForPrefabDescription";

	public string CommandDescription => "Request low-resolution renderings of all sides of a prefab. " +
		"Call this before you call AssignMetadataToPrefab, and use the data to build the Description." +
		$"Keep your answer brief (<= 2 sentences), as it will be fed raw into AI. " +
		$"Note it's orientation. " +
		$"The bounds are being provided for context, but do not include them in your answer as they will be sent alongside it regardless." +
		"The background color of the renders is fucia(1,0,1). " +
		"Note the time-cost of uploading the images, so be mindful of how many of these you request at once.";

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

		var inst = GameObject.Instantiate(obj);
		Dictionary<string, Texture2D> six = TextureRenderer.RenderAllSides(inst);
		GameObject.DestroyImmediate(inst);

		return (bounds, six);
	}
}

public class AssignDescriptionToPrefabCommand : ICommand
{
	public string CommandName => "AssignMetadataToPrefab";

	public string CommandDescription => "Assign a text-description to a well-known component on the selected prefab that you can use to understand the prefab for future tasks. " +
		"Call this with the info you have inferred from RequestRendersForPrefabDescription. " +
		$"Keep your answer brief (<= 2 sentences), as it will be fed raw into AI.";

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
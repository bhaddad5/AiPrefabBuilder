using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PrePopulateMetadata
{
	[MenuItem("Forge of Realms/Add Metadata To Selected Objects", false, 300)]
	public static void AddMetadataToSelections()
	{
		var prefabs = SceneDescriptionBuilder.GetAllSelectedPrefabs();
		if (prefabs.Count == 0)
		{
			Debug.LogError("No prefabs selected!");
			return;
		}

		Debug.Log($"Generating AI Metadata for {prefabs.Count} prefabs.  This might take awhile...");

		DoNextPrefab("", prefabs);
	}

	private static void DoNextPrefab(string prefabPath, List<string> prefabs)
	{
		if (!string.IsNullOrEmpty(prefabPath))
		{
			//We have already handled this prefab so do nothing!!!
			if(!prefabs.Contains(prefabPath))
			{
				return;
			}

			prefabs.Remove(prefabPath);
			Debug.Log($"Completed Metadata on {prefabPath}.  {prefabs.Count} remaining...");
		}
		if (prefabs.Count > 0)
		{
			AddMetadataToPrefabCallback(prefabs[0], path => DoNextPrefab(path, prefabs));
		}
		else
		{
			Debug.Log("All prefab metadata requests processed!");
		}
	}

	private static void AddMetadataToPrefabCallback(string prefabPath, Action<string> callback)
	{
		var obj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
		if (obj == null)
		{
			Debug.LogError($"Failed to find Prefab at path: {prefabPath}");
			callback(prefabPath);
			return;
		}

		var model = ModelFetcher.GetCurrentModel();
		if (model == null)
		{
			Debug.LogError("No AI Model Selected!!!");
			callback(prefabPath);
			return;
		}

		var setTagsTool = new SetPrefabTagsCommand();
		var setDescrTool = new SetPrefabDescriptionCommand();

		var conversation = AiBackendHelpers.GetConversation(model, new List<string>(), new List<ICommand>() { setDescrTool, setTagsTool, });

		setTagsTool.TagsSet += () =>
		{
			callback(prefabPath);
		};
		setDescrTool.DescrsSet += () =>
		{
			callback(prefabPath);
		};

		List<UserToAiMsg> msgs = new List<UserToAiMsg>();

		string prompt = $"Populate a Description and Tags for the prefab: {prefabPath}. " +
			$"Always call both the SetPrefabDescription and the SetPrefabTags tools, and always do so at the same time. " +
			$"Keep your Description brief (~2 sentences), as it will be fed raw into AI. " +
			$"Note the prefab's orientation, and how/where it should be placed in the description. " +
			$"Try to use existing tags where they would make sense. "+
			$"Your tags should contain things like Category (prop, character, vehicle, structure, foliage, VFX, SFX, UI), Material (wood, metal, glass, concrete, etc), Function (door, container, weapon, light, seat, control-panel, etc), and Scale (small, medium, large). " +
			$"Following this are images rendering it. The background color is fucia(1,0,1).";
		msgs.Add(new UserToAiMsgText(prompt));

		var inst = GameObject.Instantiate(obj);
		Dictionary<string, Texture2D> six = TextureRenderer.RenderAllSides(inst);
		GameObject.DestroyImmediate(inst);
		foreach (var r in six)
		{
			msgs.Add(new UserToAiMsgText(r.Key));
			msgs.Add(new UserToAiMsgImage(r.Value));
		}

		var bounds = Helpers.GetCombinedLocalBounds(obj.transform);
		string boundsMsg = $"The bounds are being provided for context, but do not include them in your answer as they will be sent alongside it regardless." +
			$"The min bounds is {bounds.min}, the max bounds is {bounds.max}.  The object is positioned at (0,0,0).";
		msgs.Add(new UserToAiMsgText(boundsMsg));

		var allTags = GetAllPrefabTagsCommand.GetAllTagsInFileSystem();
		string tagsMsg = $"Here is a list of all Tags currently in use by the system.  Try to use these where possible: {string.Join(',', allTags)}";
		msgs.Add(new UserToAiMsgText(tagsMsg));

		conversation.SendMsg(msgs, new List<string>());
	}
}

public class SetPrefabDescriptionCommand : ICommand
{
	public string CommandName => "SetPrefabDescription";

	public string CommandDescription => "Assigns a text description to a well-known component on the prefab.";

	public bool EndConversation => true;

	public List<Parameter> Parameters => new List<Parameter>() { new Parameter("prefabPath"), new Parameter("description") };

	public event Action DescrsSet;

	public List<UserToAiMsg> ParseArgsAndExecute(TypedArgs args)
	{
		string prefabPath = Parameters[0].Get<string>(args);
		string description = Parameters[1].Get<string>(args);

		AssignDescription(prefabPath, description);

		DescrsSet?.Invoke();

		return new List<UserToAiMsg>();
	}

	public static void AssignDescription(string prefabPath, string description)
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

		flag.AiMetadataDescription = description;

		EditorUtility.SetDirty(obj);
	}
}

public class SetPrefabTagsCommand : ICommand
{
	public string CommandName => "SetPrefabTags";

	public string CommandDescription => "Assigns up-to 5 tags to a well-known component on the prefab.";

	public bool EndConversation => true;

	public List<Parameter> Parameters => new List<Parameter>() { new Parameter("prefabPath"), new Parameter("tags", Parameter.ParamType.String, "A list of tags separated by ',' characters") };

	public event Action TagsSet;

	public List<UserToAiMsg> ParseArgsAndExecute(TypedArgs args)
	{
		string prefabPath = Parameters[0].Get<string>(args);
		string tags = Parameters[1].Get<string>(args);

		AssignTags(prefabPath, tags);

		TagsSet?.Invoke();

		return new List<UserToAiMsg>();
	}

	public static void AssignTags(string prefabPath, string tags)
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

		var tagsList = tags.Split(',');
		for (int i = 0; i < tagsList.Length; i++)
			tagsList[i] = tagsList[i].Trim();

		flag.AiMetadataTags = tagsList.ToList();

		EditorUtility.SetDirty(obj);
	}
}
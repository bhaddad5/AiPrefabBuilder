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
		var setTitleTool = new SetPrefabTitleCommand();
		var setSummaryTool = new SetPrefabSummaryCommand();
		var setDescrTool = new SetPrefabDescriptionCommand();

		var conversation = AiBackendHelpers.GetConversation(model, new List<string>(), new List<ICommand>() { setTitleTool, setSummaryTool, setDescrTool, setTagsTool, });

		setTagsTool.TagsSet += () =>
		{
			callback(prefabPath);
		};
		setTitleTool.TitleSet += () =>
		{
			callback(prefabPath);
		};
		setSummaryTool.SummarySet += () =>
		{
			callback(prefabPath);
		};
		setDescrTool.DescrsSet += () =>
		{
			callback(prefabPath);
		};

		List<UserToAiMsg> msgs = new List<UserToAiMsg>();

		string prompt = $"Populate a Description and Tags for the prefab: {prefabPath}. " +
			$"Always call the SetPrefabTitle, the SetPrefabSummary, the SetPrefabDescription, and the SetPrefabTags tools, and always do all 4 calls at the same time. " +
			$"Note the prefab's orientation in the Description, and how/where it should be placed. " +
			$"Try to use existing tags where they would make sense, but don't be afraid to make new ones, especially if this is a new \"kind of thing\". "+
			$"Your tags should contain things like Category (prop, character, vehicle, structure, foliage, VFX, SFX, UI), Function (door, container, weapon, light, seat, control-panel, etc), and other descriptors/tidbits you might find useful. " +
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

public class SetPrefabTitleCommand : ICommand
{
	public string CommandName => "SetPrefabTitle";

	public string CommandDescription => "Assigns a title to the prefab.";

	public bool EndConversation => true;

	public List<Parameter> Parameters => new List<Parameter>() { new Parameter("prefabPath"), new Parameter("title", Parameter.ParamType.String, "A Title, no more than 3 words long") };

	public event Action TitleSet;

	public List<UserToAiMsg> ParseArgsAndExecute(TypedArgs args)
	{
		string prefabPath = Parameters[0].Get<string>(args);
		string title = Parameters[1].Get<string>(args);

		AssignTitle(prefabPath, title);

		TitleSet?.Invoke();

		return new List<UserToAiMsg>();
	}

	public static void AssignTitle(string prefabPath, string title)
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

		flag.AiMetadataTitle = title;

		EditorUtility.SetDirty(obj);
	}
}

public class SetPrefabSummaryCommand : ICommand
{
	public string CommandName => "SetPrefabSummary";

	public string CommandDescription => "Assigns a summary to the prefab.";

	public bool EndConversation => true;

	public List<Parameter> Parameters => new List<Parameter>() { new Parameter("prefabPath"), new Parameter("summary", Parameter.ParamType.String, "A Summary, no more than 10 words long") };

	public event Action SummarySet;

	public List<UserToAiMsg> ParseArgsAndExecute(TypedArgs args)
	{
		string prefabPath = Parameters[0].Get<string>(args);
		string summary = Parameters[1].Get<string>(args);

		AssignSummary(prefabPath, summary);

		SummarySet?.Invoke();

		return new List<UserToAiMsg>();
	}

	public static void AssignSummary(string prefabPath, string summary)
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

		flag.AiMetadataSummary = summary;

		EditorUtility.SetDirty(obj);
	}
}

public class SetPrefabDescriptionCommand : ICommand
{
	public string CommandName => "SetPrefabDescription";

	public string CommandDescription => "Assigns a text description to a well-known component on the prefab.";

	public bool EndConversation => true;

	public List<Parameter> Parameters => new List<Parameter>() { new Parameter("prefabPath"), new Parameter("description", Parameter.ParamType.String, "A short description, no more than 2 sentances.") };

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

	public string CommandDescription => "Assigns up-to 10 tags to a well-known component on the prefab.";

	public bool EndConversation => true;

	public List<Parameter> Parameters => new List<Parameter>() { new Parameter("prefabPath"), new Parameter("tags", Parameter.ParamType.StringList) };

	public event Action TagsSet;

	public List<UserToAiMsg> ParseArgsAndExecute(TypedArgs args)
	{
		string prefabPath = Parameters[0].Get<string>(args);
		List<string> tags = Parameters[1].Get<List<string>>(args);

		AssignTags(prefabPath, tags);

		TagsSet?.Invoke();

		return new List<UserToAiMsg>();
	}

	public static void AssignTags(string prefabPath, List<string> tags)
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

		flag.AiMetadataTags = tags;

		EditorUtility.SetDirty(obj);
	}
}
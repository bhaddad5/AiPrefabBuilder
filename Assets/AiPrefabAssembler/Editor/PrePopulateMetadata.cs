using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling.Memory.Experimental;

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

		AddMetadataToPrefabCallback(prefabs[0], () => { Debug.Log("TODO: DO OTHERS!!!"); });

	}

	private static void AddMetadataToPrefabCallback(string prefabPath, Action callback)
	{
		var obj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
		if (obj == null)
		{
			Debug.LogError($"Failed to find Prefab at path: {prefabPath}");
			callback();
			return;
		}

		var model = ModelFetcher.GetCurrentModel();
		if (model == null)
		{
			Debug.LogError("No AI Model Selected!!!");
			callback();
			return;
		}

		var conversation = AiBackendHelpers.GetConversation(model, new List<string>(), new List<ICommand>());

		conversation.ChatMsgAdded += (msg) =>
		{
			if (!msg.IsFromUser)
			{
				var flag = obj.GetComponent<AiMetadataFlag>();
				if (flag == null)
					flag = obj.AddComponent<AiMetadataFlag>();

				flag.AiMetadata = msg.Text;

				EditorUtility.SetDirty(obj);

				callback();
			}
		};

		List<UserToAiMsg> msgs = new List<UserToAiMsg>();

		string prompt = $"Describe what this prefab is & looks like in 2 sentences. " +
			$"Keep your answer brief, as it will be fed raw into AI. " +
			$"Note it's orientation. " +
			$"The prefab is named {Path.GetFileNameWithoutExtension(prefabPath)}. " +
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

		conversation.SendMsg(msgs, new List<string>());
	}
}

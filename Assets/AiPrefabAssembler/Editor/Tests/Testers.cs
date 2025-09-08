using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class Testers
{
	[MenuItem("Forge of Realms - Tests/Test AI Calls", false, 1000)]
	public static async void TestAiCalls()
	{
		var res = await AiRequestBackend.OpenAISdk.AskAsync(new List<string>() { "You are a snarky asshole." }, "What day is it?");
		Debug.Log(res);
		Debug.Log("Done!");
	}

	[MenuItem("Forge of Realms - Tests/Test GameObject Description Builder", false, 1000)]
	public static void TestGameObjectDescriptionBuilder()
	{
		var direct = Selection.GetFiltered<GameObject>(SelectionMode.TopLevel | SelectionMode.Editable | SelectionMode.ExcludePrefab);

		if(direct.Length == 0)
		{
			Debug.LogError("No GameObject selected!");
			return;
		}

		string descr = SceneDescriptionBuilder.BuildGameObjectDescription(direct[0].transform);

		Debug.Log(descr);

		Debug.Log("Done!");
	}
}

using System;
using UnityEditor;
using UnityEngine;

public static class PrefabAssemblyMenu
{
	[MenuItem("AI Prefab Assembly/Test AI Calls", false, 1000)]
	public static async void TestAiCalls()
	{
		var res = await AiRequestBackend.OpenAIChatSdk.AskAsync(Environment.GetEnvironmentVariable("OPENAI_API_KEY"), "What day is it?");
		Debug.Log(res);
		Debug.Log("Done!");
	}
}

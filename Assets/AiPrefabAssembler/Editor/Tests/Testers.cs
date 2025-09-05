using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class Testers
{
	[MenuItem("AI Prefab Assembly/Test AI Calls", false, 1000)]
	public static async void TestAiCalls()
	{
		var res = await AiRequestBackend.OpenAISdk.AskAsync(EditorPrefs.GetString("OPENAI_API_KEY"), new List<string>() { "You are a snarky asshole." }, "What day is it?");
		Debug.Log(res);
		Debug.Log("Done!");
	}

	[MenuItem("AI Prefab Assembly/Test Scene Description Builder", false, 1000)]
	public static void TestSceneDescriptionBuilder()
	{
		string descr = SceneDescriptionBuilder.BuildSceneDescription();

		Debug.Log(descr);

		Debug.Log("Done!");
	}

	[MenuItem("AI Prefab Assembly/Test Command Parsing", false, 1000)]
	public static void TestParsing()
	{
		string testStr = "DeleteObject[-43080]\r\nCreateObject[20043080,SM_Bld_House_Roof_Thatch_Cover_01,SM_Bld_House_Roof_Thatch_Cover_01,localPos:(2.5;4.25;0),localEuler:(0;89.968;0),localScale:(1;1;1),46648]\r\nDeleteObject[-50482]\r\nCreateObject[20050482,SM_Bld_House_Roof_Thatch_Cover_Edge_01,SM_Bld_House_Roof_Thatch_Cover_Edge_01,localPos:(-0.25;4.25;0),localEuler:(0;90;0),localScale:(1;1;1),46648]\r\nDeleteObject[-58454]\r\nCreateObject[20058454,SM_Bld_House_Roof_Thatch_Cover_01,SM_Bld_House_Roof_Thatch_Cover_01,localPos:(5;4.25;0),localEuler:(0;89.968;0),localScale:(1;1;1),46648]\r\nDeleteObject[-63776]\r\nCreateObject[20063776,SM_Bld_House_Roof_Thatch_Cover_Edge_01,SM_Bld_House_Roof_Thatch_Cover_Edge_01,localPos:(7.5;4.25;0),localEuler:(0;90;0),localScale:(1;1;1),46648]\r\nDeleteObject[-100116]\r\nCreateObject[200100116,SM_Bld_House_Roof_Thatch_Cover_01,SM_Bld_House_Roof_Thatch_Cover_01,localPos:(7.5;4.25;0),localEuler:(0;89.968;0),localScale:(1;1;1),46648]";
		AiCommandParser.TryExecuteCommands(testStr);

		Debug.Log("Done!");
	}
}

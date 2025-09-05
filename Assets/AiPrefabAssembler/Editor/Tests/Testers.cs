using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class Testers
{
	[MenuItem("AI Prefab Assembly/Test AI Calls", false, 1000)]
	public static async void TestAiCalls()
	{
		var res = await AiRequestBackend.OpenAIChatSdk.AskAsync(EditorPrefs.GetString("OPENAI_API_KEY"), new List<string>() { "You are a snarky asshole." }, "What day is it?");
		Debug.Log(res);
		Debug.Log("Done!");
	}

	/*[MenuItem("AI Prefab Assembly/Test Parsing", false, 1000)]
	public static void TestParsing()
	{
		string testStr = "[name:SM_Bld_House_Base_Corner_01, pos:(-2.52;0.00;-2.52), euler:(0;0;0)]\r\n[name:SM_Bld_House_Base_Corner_01, pos:(2.52;0.00;-2.52), euler:(0;90;0)]\r\n[name:SM_Bld_House_Base_Corner_01, pos:(2.52;0.00;2.52), euler:(0;180;0)]\r\n[name:SM_Bld_House_Base_Corner_01, pos:(-2.52;0.00;2.52), euler:(0;270;0)]\r\n[name:SM_Bld_House_Wall_01, pos:(-1.26;0.00;-2.52), euler:(0;0;0)]\r\n[name:SM_Bld_House_Wall_Door_01, pos:(1.26;0.00;-2.52), euler:(0;0;0)]\r\n[name:SM_Bld_House_Wall_01, pos:(-1.26;0.00;2.52), euler:(0;180;0)]\r\n[name:SM_Bld_House_Wall_01, pos:(1.26;0.00;2.52), euler:(0;180;0)]\r\n[name:SM_Bld_House_Wall_02, pos:(-2.52;0.00;-1.26), euler:(0;90;0)]\r\n[name:SM_Bld_House_Wall_02, pos:(-2.52;0.00;1.26), euler:(0;90;0)]\r\n[name:SM_Bld_House_Wall_02, pos:(2.52;0.00;-1.26), euler:(0;270;0)]\r\n[name:SM_Bld_House_Wall_02, pos:(2.52;0.00;1.26), euler:(0;270;0)]\r\n[name:SM_Bld_House_Floor_Wood_01, pos:(0.00;0.00;0.00), euler:(0;0;0)]\r\n[name:SM_Bld_House_Roof_Thatch_Angled_01, pos:(0.00;2.60;0.00), euler:(0;0;0)]\r\n[name:SM_Bld_House_Roof_Thatch_Angled_01, pos:(0.00;2.60;0.00), euler:(0;180;0)]\r\n[name:SM_Bld_House_Roof_Thatch_Peak_Cap_01, pos:(0.00;2.90;0.00), euler:(0;0;0)]\r\n[name:SM_Bld_House_Chimney_Base_01, pos:(0.80;2.50;0.60), euler:(0;0;0)]\r\n[name:SM_Bld_House_Chimney_01, pos:(0.80;3.20;0.60), euler:(0;0;0)]\r\n[name:SM_Bld_House_Fireplace_01, pos:(-1.40;0.00;1.00), euler:(0;90;0)]\r\n[name:SM_Bld_House_WoodBeam_01, pos:(0.00;0.00;0.00), euler:(0;0;0)]\r\n[name:SM_Bld_House_Supports_01, pos:(2.20;-0.02;2.20), euler:(0;90;0)]";

		FinalResultPrefabBuilder.BuildPrefabFromInstructions(testStr);

		Debug.Log("Done!");
	}*/

	[MenuItem("AI Prefab Assembly/Test Generate Object Metadata", false, 1000)]
	public static async void TestGenerateObjectMetadata()
	{
		var obj = Helpers.GetSelectedPrefab();
		if(obj == null)
		{
			Debug.LogError("No prefab selected!");
			return;
		}

		var res = await MetadataRequester.GeneratePrefabMetadata(obj);

		Debug.Log(res);
		Debug.Log("Done!");
	}

	[MenuItem("AI Prefab Assembly/Test Texture Rendering", false, 1000)]
	public static void TextTextureRendering()
	{
		var obj = Helpers.GetSelectedPrefab();
		if (obj == null)
		{
			Debug.LogError("No prefab selected!");
			return;
		}

		var inst = GameObject.Instantiate(obj);

		float tileScale = 0.6f;
		float tileSpacing = 0.8f;

		// 1) Render the six views (so preview quads don't get captured)
		Dictionary<string, Texture2D> six = TextureRenderer.RenderAllSides(inst);

		// 2) Make a parent
		GameObject parent = new GameObject("SixViewPreview");

		// 3) Order and grid layout: top row (Front, Right, Back), bottom row (Left, Top, Bottom)
		string[] order = { "Front", "Right", "Back", "Left", "Top", "Bottom" };

		// Precompute grid framing so it's centered in front of camera
		int cols = 3, rows = 2;
		float gridWidth = (cols - 1) * tileSpacing;
		float gridHeight = (rows - 1) * tileSpacing;

		// 4) Create a simple unlit material per tile (fallbacks for URP/BiRP)
		Material MakeMat(Texture2D tex)
		{
			Shader s = Shader.Find("Unlit/Transparent");
			if (s == null) s = Shader.Find("Sprites/Default");
			if (s == null) s = Shader.Find("Universal Render Pipeline/Unlit");
			if (s == null) s = Shader.Find("Standard"); // last resort
			var m = new Material(s);
			// Assign texture to any common main-texture slots
			if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
			if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
			return m;
		}

		// 5) Spawn quads and assign textures
		for (int i = 0; i < order.Length; i++)
		{
			int row = i / cols;
			int col = i % cols;

			float x = -gridWidth * 0.5f + col * tileSpacing;
			float y = gridHeight * 0.5f - row * tileSpacing;

			var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
			quad.name = $"SixView_{order[i]}";
			quad.transform.SetParent(parent.transform, worldPositionStays: true);

			// Place in front of camera, oriented to face the camera
			Vector3 center = parent.transform.position;
			quad.transform.position = center + parent.transform.right * x + parent.transform.up * y;
			quad.transform.localScale = Vector3.one * tileScale;

			// Disable collider (not needed for a visual preview)
			var coll = quad.GetComponent<Collider>();
			if (coll) Collider.DestroyImmediate(coll);

			// Assign texture
			if (six.TryGetValue(order[i], out var tex))
			{
				var rend = quad.GetComponent<Renderer>();
				rend.material = MakeMat(tex);
			}
		}

		GameObject.DestroyImmediate(inst);

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

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Codice.Client.BaseCommands.BranchExplorer.Layout.BrExLayout;
using static UnityEngine.GraphicsBuffer;
using UnityEngine.UIElements;

public static class ParsingTesters
{
	[MenuItem("AI Prefab Assembly/Test Parsing", false, 1000)]
	public static void TestParsing()
	{
		string testStr = "[name:SM_Bld_House_Base_Corner_01, pos:(-2.52;0.00;-2.52), euler:(0;0;0)]\r\n[name:SM_Bld_House_Base_Corner_01, pos:(2.52;0.00;-2.52), euler:(0;90;0)]\r\n[name:SM_Bld_House_Base_Corner_01, pos:(2.52;0.00;2.52), euler:(0;180;0)]\r\n[name:SM_Bld_House_Base_Corner_01, pos:(-2.52;0.00;2.52), euler:(0;270;0)]\r\n[name:SM_Bld_House_Wall_01, pos:(-1.26;0.00;-2.52), euler:(0;0;0)]\r\n[name:SM_Bld_House_Wall_Door_01, pos:(1.26;0.00;-2.52), euler:(0;0;0)]\r\n[name:SM_Bld_House_Wall_01, pos:(-1.26;0.00;2.52), euler:(0;180;0)]\r\n[name:SM_Bld_House_Wall_01, pos:(1.26;0.00;2.52), euler:(0;180;0)]\r\n[name:SM_Bld_House_Wall_02, pos:(-2.52;0.00;-1.26), euler:(0;90;0)]\r\n[name:SM_Bld_House_Wall_02, pos:(-2.52;0.00;1.26), euler:(0;90;0)]\r\n[name:SM_Bld_House_Wall_02, pos:(2.52;0.00;-1.26), euler:(0;270;0)]\r\n[name:SM_Bld_House_Wall_02, pos:(2.52;0.00;1.26), euler:(0;270;0)]\r\n[name:SM_Bld_House_Floor_Wood_01, pos:(0.00;0.00;0.00), euler:(0;0;0)]\r\n[name:SM_Bld_House_Roof_Thatch_Angled_01, pos:(0.00;2.60;0.00), euler:(0;0;0)]\r\n[name:SM_Bld_House_Roof_Thatch_Angled_01, pos:(0.00;2.60;0.00), euler:(0;180;0)]\r\n[name:SM_Bld_House_Roof_Thatch_Peak_Cap_01, pos:(0.00;2.90;0.00), euler:(0;0;0)]\r\n[name:SM_Bld_House_Chimney_Base_01, pos:(0.80;2.50;0.60), euler:(0;0;0)]\r\n[name:SM_Bld_House_Chimney_01, pos:(0.80;3.20;0.60), euler:(0;0;0)]\r\n[name:SM_Bld_House_Fireplace_01, pos:(-1.40;0.00;1.00), euler:(0;90;0)]\r\n[name:SM_Bld_House_WoodBeam_01, pos:(0.00;0.00;0.00), euler:(0;0;0)]\r\n[name:SM_Bld_House_Supports_01, pos:(2.20;-0.02;2.20), euler:(0;90;0)]";

		FinalResultPrefabBuilder.BuildPrefabFromInstructions(testStr);
	}

	[MenuItem("AI Prefab Assembly/Test Texture Rendering", false, 1000)]
	public static void TextTextureRendering()
	{
		GameObject target = GameObject.Find("SM_Bld_House_Chimney_01");
		Camera cam = GameObject.Find("Test Camera").GetComponent<Camera>();

		if(target == null)
		{
			Debug.LogError("Could not find SM_Bld_House_Chimney_01 in scene");
			return;
		}

		if (cam == null)
		{
			Debug.LogError("Could not find Test Camera in scene");
			return;
		}


		float gridDistanceFromCamera = 2f;
		float tileScale = 0.6f;
		float tileSpacing = 0.8f;
		int texSize = 128;

		// 1) Render the six views (so preview quads don't get captured)
		Dictionary<string, Texture2D> six = TextureRenderer.RenderSixViews(target, cam, texSize);

		// 2) Make a parent
		GameObject parent = new GameObject("SixViewPreview");
		parent.transform.position = cam.transform.position + cam.transform.forward * gridDistanceFromCamera;
		parent.transform.rotation = cam.transform.rotation;

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
			quad.transform.rotation = Quaternion.LookRotation(-cam.transform.forward, cam.transform.up);
			quad.transform.localScale = Vector3.one * tileScale;

			// Disable collider (not needed for a visual preview)
			var coll = quad.GetComponent<Collider>();
			if (coll) Object.Destroy(coll);

			// Assign texture
			if (six.TryGetValue(order[i], out var tex))
			{
				var rend = quad.GetComponent<Renderer>();
				rend.material = MakeMat(tex);
			}
		}

	}
}

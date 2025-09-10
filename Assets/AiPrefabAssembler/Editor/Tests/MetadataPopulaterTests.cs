using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class MetadataPopulaterTesters
{
	[MenuItem("Forge of Realms - Tests/Test Texture Rendering", false, 1000)]
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

		int i = 0;
		foreach(var r in six)
		{
			int row = i / cols;
			int col = i % cols;

			float x = -gridWidth * 0.5f + col * tileSpacing;
			float y = gridHeight * 0.5f - row * tileSpacing;

			var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
			quad.name = $"SixView_{r.Key}";
			quad.transform.SetParent(parent.transform, worldPositionStays: true);

			// Place in front of camera, oriented to face the camera
			Vector3 center = parent.transform.position;
			quad.transform.position = center + parent.transform.right * x + parent.transform.up * y;
			quad.transform.localScale = Vector3.one * tileScale;

			// Disable collider (not needed for a visual preview)
			var coll = quad.GetComponent<Collider>();
			if (coll) Collider.DestroyImmediate(coll);

			var rend = quad.GetComponent<Renderer>();
			rend.material = MakeMat(r.Value);

			i++;
		}

		GameObject.DestroyImmediate(inst);

		Debug.Log("Done!");
	}
}

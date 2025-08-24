using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public static class MetadataRequester
{
    public static async Task<string> GeneratePrefabMetadata(GameObject prefab)
    {
		if(prefab.GetComponentInChildren<Renderer>() == null)
		{
			Debug.LogError($"No renderer found in prefab {prefab}");
			return "";
		}

		var ob = GameObject.Instantiate(prefab);

		Dictionary<string, Texture2D> six = TextureRenderer.RenderAllSides(prefab);

		Dictionary<string, BinaryData> converted = new Dictionary<string, BinaryData>();

		foreach(var t in six)
		{
			converted[t.Key] = Texture2DToPngBinaryData(t.Value);
		}

		var bounds = GetBoundsRecursive(ob);

		GameObject.DestroyImmediate(ob);

		string prompt = $"Describe what this prefab looks like, and note any information that would be useful when placing it in a larger object with other prefabs. " + 
			$"Keep your answer brief, as it will be fed raw into AI. " +
			$"The prefab is named {prefab.name}. " +
			$"The min bounds is {bounds.min}, the max bounds is {bounds.max}.  The object is positioned at (0,0,0). " +
			$"Following this are images rendering it.";

		//Debug.Log(prompt);

		var res = await AiRequestBackend.OpenAIChatSdk.AskImagesAsync(Environment.GetEnvironmentVariable("OPENAI_API_KEY"), prompt, converted);

		return res;
	}

	/// <summary>
	/// Turn a Texture2D into BinaryData with PNG bytes. MIME is "image/png".
	/// </summary>
	private static BinaryData Texture2DToPngBinaryData(Texture2D tex, bool makeReadableIfNeeded = true)
	{
		if (tex == null) throw new System.ArgumentNullException(nameof(tex));

		Texture2D src = tex;

		if (!tex.isReadable)
		{
			if (!makeReadableIfNeeded)
				throw new System.InvalidOperationException("Texture is not readable. Enable Read/Write or set makeReadableIfNeeded=true.");

			// Make a readable copy
			var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
			var prev = RenderTexture.active;
			Graphics.Blit(tex, rt);
			RenderTexture.active = rt;

			src = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
			src.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
			src.Apply();

			RenderTexture.active = prev;
			RenderTexture.ReleaseTemporary(rt);
		}

		byte[] pngBytes = src.EncodeToPNG();
		if (src != tex) Texture2D.DestroyImmediate(src); // dispose temp copy

		return BinaryData.FromBytes(pngBytes); // MIME is "image/png"
	}

	private static Bounds GetBoundsRecursive(GameObject root)
	{
		var renderers = root.GetComponentsInChildren<Renderer>();

		Bounds b = renderers[0].bounds;
		for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

		return b;
	}
}

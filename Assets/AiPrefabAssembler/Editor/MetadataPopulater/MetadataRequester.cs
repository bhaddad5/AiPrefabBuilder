using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
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

		var info = BuildMetadataInfo(prefab);

		if(info.Bounds == null)
		{
			Debug.LogError("No renderers on selected object; no metadata can be built.");
			return "";
		}

		string prompt = $"Describe what this prefab is & looks like in 2 sentences. " + 
			$"Keep your answer brief, as it will be fed raw into AI. " +
			$"Note it's orientation. " +
			$"The prefab is named {prefab.name}. " +
			$"The bounds are being provided for context, but do not include them in your answer as they will be sent alongside it regardless." +
			$"The min bounds is {info.Bounds.Value.min}, the max bounds is {info.Bounds.Value.max}.  The object is positioned at (0,0,0). " +
			$"Following this are images rendering it. The background color is fucia(1,0,1).";

		var res = await AiRequestBackend.OpenAISdk.AskImagesAsync(prompt, info.Renders);

		return res;
	}

	public static (Bounds? Bounds, Dictionary<string, BinaryData> Renders) BuildMetadataInfo(GameObject prefab)
	{
		var ob = GameObject.Instantiate(prefab);

		Dictionary<string, Texture2D> six = TextureRenderer.RenderAllSides(ob);

		Dictionary<string, BinaryData> converted = new Dictionary<string, BinaryData>();

		foreach (var t in six)
		{
			converted[t.Key] = Texture2DToJPGBinaryData(t.Value);
		}

		var bounds = Helpers.GetCombinedLocalBounds(ob.transform);

		GameObject.DestroyImmediate(ob);

		return (bounds, converted);
	}

	private static void TestRenderToFiles(Dictionary<string, Texture2D> six, string name)
	{
		string folder = Path.Combine(Application.streamingAssetsPath, "Test Renders");
		if (!Directory.Exists(folder))
			Directory.CreateDirectory(folder);

		foreach (var r in six)
		{
			string filePath = Path.Combine(folder, $"{name}-{r.Key}.jpg");

			Directory.CreateDirectory(Path.GetDirectoryName(filePath));
			File.WriteAllBytes(filePath, Texture2DToJPGBinaryData(r.Value).ToArray());
		}
	}

	/// <summary>
	/// Turn a Texture2D into BinaryData with JPG bytes. MIME is "image/jpeg".
	/// </summary>
	private static BinaryData Texture2DToJPGBinaryData(Texture2D tex, bool makeReadableIfNeeded = true)
	{
		if (tex == null) throw new System.ArgumentNullException(nameof(tex));

		var flat = Flatten(tex);
		Texture2D src = flat;

		if (!tex.isReadable)
		{
			if (!makeReadableIfNeeded)
				throw new System.InvalidOperationException("Texture is not readable. Enable Read/Write or set makeReadableIfNeeded=true.");

			// Make a readable copy
			var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.RG32);
			var prev = RenderTexture.active;
			Graphics.Blit(tex, rt);
			RenderTexture.active = rt;

			src = new Texture2D(tex.width, tex.height, TextureFormat.RG32, false);
			src.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
			src.Apply();

			RenderTexture.active = prev;
			RenderTexture.ReleaseTemporary(rt);
		}

		byte[] jpgBytes = src.EncodeToJPG(90);
		if (src != tex) Texture2D.DestroyImmediate(src); // dispose temp copy
		Texture2D.DestroyImmediate(flat); // dispose temp copy

		return BinaryData.FromBytes(jpgBytes); // MIME is "image/jpg"
	}

	private static Texture2D Flatten(Texture2D src)
	{
		Color bg = new Color(1f, 0f, 1f);

		var w = src.width; var h = src.height;
		var dst = new Texture2D(w, h, TextureFormat.RGB24, false, false);
		var p = src.GetPixels32();
		for (int i = 0; i < p.Length; i++)
		{
			// Unpremultiplied alpha blend: out = a*fg + (1-a)*bg
			float a = p[i].a / 255f;
			byte r = (byte)(a * p[i].r + (1f - a) * bg.r * 255f);
			byte g = (byte)(a * p[i].g + (1f - a) * bg.g * 255f);
			byte b = (byte)(a * p[i].b + (1f - a) * bg.b * 255f);
			p[i] = new Color32(r, g, b, 255);
		}
		dst.SetPixels32(p); dst.Apply();
		return dst;
	}
}

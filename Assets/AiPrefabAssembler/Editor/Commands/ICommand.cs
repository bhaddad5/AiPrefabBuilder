using System;
using System.Collections.Generic;
using UnityEngine;

public class Parameter
{
	public enum ParamType
	{
		String,
		Int,
		Vector3,
	}

    public string Name;
	public ParamType Type = ParamType.String;
	public string Description = "";
    public bool Required = true;

	public Parameter(string name, ParamType type = ParamType.String, string description = "", bool required = true)
	{
		Name = name;
		Type = type;
		Description = description;
		Required = required;
	}

	public T Get<T>(TypedArgs args)
	{
		if(args.Values.ContainsKey(Name) && args.Values[Name] is T t)
		{
			return t;
		}

		Debug.LogError($"Failed to parse arg {Name} in {String.Join(',', args.Values.Keys)}");

		return default;
	}
}

public sealed class TypedArgs
{
	public Dictionary<string, object> Values = new(StringComparer.OrdinalIgnoreCase);
}

public abstract class UserToAiMsg { }

public class UserToAiMsgText : UserToAiMsg
{
	public string Text = null;

	public UserToAiMsgText(string text)
	{
		Text = text;
	}

	public override string ToString()
	{
		return Text;
	}
}

public class UserToAiMsgImage : UserToAiMsg
{
	public Texture2D Image = null;

	public UserToAiMsgImage(Texture2D image)
	{
		Image = image;
	}

	public override string ToString()
	{
		return Image?.name ?? "";
	}

	public byte[] GetImageBytes()
	{
		return Texture2DToJPGBinaryData(Image);
	}

	private static byte[] Texture2DToJPGBinaryData(Texture2D tex, bool makeReadableIfNeeded = true)
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

		return jpgBytes; // MIME is "image/jpg"
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

public interface ICommand
{
    public string CommandName { get; }
	public string CommandDescription { get; }

    public List<Parameter> Parameters { get; }

    public List<UserToAiMsg> ParseArgsAndExecute(TypedArgs args);
}

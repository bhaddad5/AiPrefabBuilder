using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureRenderer
{
	public static Dictionary<string, Texture2D> RenderAllSides(GameObject ob)
	{
		var cam = new GameObject("Test Camera").AddComponent<Camera>();
		cam.orthographic = true;

		var res = RenderSixViews(ob, cam);

		GameObject.DestroyImmediate(cam.gameObject);

		return res;
	}

	/// <param name="transparentBackground">Clear to transparent so background alpha = 0.</param>
	/// <param name="isolateTarget">Temporarily render only the target by isolating it to a layer.</param>
	/// <param name="isolateLayer">Layer index to use for isolation (defaults to 31).</param>
	private static Dictionary<string, Texture2D> RenderSixViews(
		GameObject target, Camera cam, int size = 128, float paddingPercent = 0.05f,
		bool transparentBackground = true, bool isolateTarget = true, int isolateLayer = 31)
	{
		if (!target) throw new System.ArgumentNullException(nameof(target));
		if (!cam) throw new System.ArgumentNullException(nameof(cam));

		var renderers = target.GetComponentsInChildren<Renderer>();
		if (renderers == null || renderers.Length == 0)
			throw new System.InvalidOperationException("Target has no renderers to capture.");

		// --- compute radius enclosing object around origin ---
		float radius = 0f;
		foreach (var r in renderers)
		{
			var c = r.bounds.center;
			var e = r.bounds.extents;
			float far = new Vector3(Mathf.Abs(c.x) + e.x, Mathf.Abs(c.y) + e.y, Mathf.Abs(c.z) + e.z).magnitude;
			if (far > radius) radius = far;
		}
		radius *= (1f + Mathf.Max(0, paddingPercent));

		// --- save camera state ---
		var originalPos = cam.transform.position;
		var originalRot = cam.transform.rotation;
		var originalTargetRT = cam.targetTexture;
		var originalFOV = cam.fieldOfView;
		var originalOrthoSize = cam.orthographicSize;
		var originalNear = cam.nearClipPlane;
		var originalFar = cam.farClipPlane;
		var originalAspect = cam.aspect;
		var originalOrtho = cam.orthographic;
		var originalClear = cam.clearFlags;
		var originalBG = cam.backgroundColor;
		var originalCulling = cam.cullingMask;

		// --- optionally isolate the target to a single layer ---
		List<(Transform t, int oldLayer)> layerChanges = null;
		if (isolateTarget)
		{
			layerChanges = new List<(Transform, int)>(64);
			foreach (var t in target.GetComponentsInChildren<Transform>(true))
			{
				if (t.gameObject.layer != isolateLayer)
				{
					layerChanges.Add((t, t.gameObject.layer));
					t.gameObject.layer = isolateLayer;
				}
			}
			cam.cullingMask = (1 << isolateLayer);
		}

		// --- configure camera for transparent background & square render ---
		cam.aspect = 1f;
		if (transparentBackground)
		{
			cam.clearFlags = CameraClearFlags.SolidColor;
			cam.backgroundColor = new Color(0f, 0f, 0f, 0f); // fully transparent
		}

		// --- compute consistent distance ---
		float distance;
		if (!cam.orthographic)
		{
			float fovRad = cam.fieldOfView * Mathf.Deg2Rad;
			distance = radius / Mathf.Sin(fovRad * 0.5f);
			cam.nearClipPlane = Mathf.Max(0.01f, distance - radius * 1.2f);
			cam.farClipPlane = distance + radius * 1.2f;
		}
		else
		{
			cam.orthographicSize = radius;
			distance = Mathf.Max(radius * 2f, cam.nearClipPlane + radius);
			cam.nearClipPlane = Mathf.Max(0.01f, distance - radius * 1.2f);
			cam.farClipPlane = distance + radius * 1.2f;
		}

		// --- render target ---
		var rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
		var results = new Dictionary<string, Texture2D>(6);

		void CaptureAt(Vector3 dir, Vector3 up, string key)
		{
			cam.transform.position = dir.normalized * distance;
			cam.transform.rotation = Quaternion.LookRotation(-dir, up);
			cam.targetTexture = rt;

			var prevActive = RenderTexture.active;
			RenderTexture.active = rt;

			cam.Render();

			var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
			tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
			tex.Apply(false, false);
			results[key] = tex;

			RenderTexture.active = prevActive;
			cam.targetTexture = null;
		}

		try
		{
			CaptureAt(Vector3.up, Vector3.forward, "Top");
			CaptureAt(Vector3.down, Vector3.forward, "Bottom");
			CaptureAt(Vector3.left, Vector3.up, "Left");
			CaptureAt(Vector3.right, Vector3.up, "Right");
			CaptureAt(Vector3.forward, Vector3.up, "Front");
			CaptureAt(Vector3.back, Vector3.up, "Back");
		}
		finally
		{
			// restore layers
			if (layerChanges != null)
				foreach (var (t, oldLayer) in layerChanges) t.gameObject.layer = oldLayer;

			// restore camera
			cam.transform.SetPositionAndRotation(originalPos, originalRot);
			cam.targetTexture = originalTargetRT;
			cam.fieldOfView = originalFOV;
			cam.orthographicSize = originalOrthoSize;
			cam.nearClipPlane = originalNear;
			cam.farClipPlane = originalFar;
			cam.aspect = originalAspect;
			cam.orthographic = originalOrtho;
			cam.clearFlags = originalClear;
			cam.backgroundColor = originalBG;
			cam.cullingMask = originalCulling;

			// cleanup
			rt.Release();
			Object.DestroyImmediate(rt);
		}

		return results;
	}
}
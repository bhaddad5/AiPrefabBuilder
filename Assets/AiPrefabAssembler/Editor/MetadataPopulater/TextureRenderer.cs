using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureRenderer
{
	/// <summary>
	/// Renders 6 square textures (Top, Bottom, Left, Right, Front, Back) of the target object at (0,0,0).
	/// Uses the supplied camera; restores the camera's state before returning.
	/// Returns a dictionary with keys: "Top","Bottom","Left","Right","Front","Back".
	/// </summary>
	public static Dictionary<string, Texture2D> RenderSixViews(GameObject target, Camera cam, int size = 128, float paddingPercent = 0.05f)
	{
		if (target == null) throw new System.ArgumentNullException(nameof(target));
		if (cam == null) throw new System.ArgumentNullException(nameof(cam));

		// Collect renderers; we need bounds to determine a safe distance.
		var renderers = target.GetComponentsInChildren<Renderer>();
		if (renderers == null || renderers.Length == 0)
			throw new System.InvalidOperationException("Target has no renderers to capture.");

		// Compute a radius that fully encloses the object *around the world origin* (0,0,0).
		// This works even if the mesh pivot isn't exactly at the origin.
		float radius = 0f;
		foreach (var r in renderers)
		{
			var c = r.bounds.center;
			var e = r.bounds.extents;
			// Farthest point of this AABB from the origin
			var far = new Vector3(Mathf.Abs(c.x) + e.x, Mathf.Abs(c.y) + e.y, Mathf.Abs(c.z) + e.z).magnitude;
			if (far > radius) radius = far;
		}
		radius *= (1f + Mathf.Max(0, paddingPercent)); // small padding margin

		// Save camera state
		var originalPos = cam.transform.position;
		var originalRot = cam.transform.rotation;
		var originalTargetRT = cam.targetTexture;
		var originalFOV = cam.fieldOfView;
		var originalOrthoSize = cam.orthographicSize;
		var originalNear = cam.nearClipPlane;
		var originalFar = cam.farClipPlane;
		var originalAspect = cam.aspect;
		var originalOrtho = cam.orthographic;

		// We will render square images.
		cam.aspect = 1f;

		// Compute distance so a sphere of radius 'radius' fits the view.
		float distance;
		if (!cam.orthographic)
		{
			float fovRad = cam.fieldOfView * Mathf.Deg2Rad;
			// Distance so the sphere fits vertically; aspect=1 means horizontal matches.
			distance = radius / Mathf.Sin(fovRad * 0.5f);
			// Keep clipping planes sensible around the shell
			cam.nearClipPlane = Mathf.Max(0.01f, distance - radius * 1.2f);
			cam.farClipPlane = distance + radius * 1.2f;
		}
		else
		{
			// Orthographic: size must cover the radius; distance can be arbitrary but consistent.
			cam.orthographicSize = radius;
			distance = Mathf.Max(radius * 2f, cam.nearClipPlane + radius);
			cam.nearClipPlane = Mathf.Max(0.01f, distance - radius * 1.2f);
			cam.farClipPlane = distance + radius * 1.2f;
		}

		// Prepare a reusable RenderTexture and capture helper
		var rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
		var results = new Dictionary<string, Texture2D>(6);

		void CaptureAt(Vector3 dir, Vector3 up, string key)
		{
			cam.transform.position = dir.normalized * distance;      // same distance for all shots
			cam.transform.rotation = Quaternion.LookRotation(-dir, up);
			cam.targetTexture = rt;

			var prevActive = RenderTexture.active;
			RenderTexture.active = rt;

			cam.Render();

			var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
			tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
			tex.Apply();

			results[key] = tex;

			RenderTexture.active = prevActive;
			cam.targetTexture = null;
		}

		try
		{
			// Define the six directions from the origin and capture.
			// Conventions:
			//  Front: +Z  (camera at +Z looking toward -Z)
			//  Back:  -Z
			//  Right: +X
			//  Left:  -X
			//  Top:   +Y  (up set to +Z for a stable image)
			//  Bottom:-Y  (up set to +Z to keep orientation consistent)
			CaptureAt(Vector3.up, Vector3.forward, "Top");
			CaptureAt(Vector3.down, Vector3.forward, "Bottom");
			CaptureAt(Vector3.left, Vector3.up, "Left");
			CaptureAt(Vector3.right, Vector3.up, "Right");
			CaptureAt(Vector3.forward, Vector3.up, "Front");
			CaptureAt(Vector3.back, Vector3.up, "Back");
		}
		finally
		{
			// Restore camera and clean up
			cam.transform.SetPositionAndRotation(originalPos, originalRot);
			cam.targetTexture = originalTargetRT;
			cam.fieldOfView = originalFOV;
			cam.orthographicSize = originalOrthoSize;
			cam.nearClipPlane = originalNear;
			cam.farClipPlane = originalFar;
			cam.aspect = originalAspect;
			cam.orthographic = originalOrtho;

			RenderTexture.ReleaseTemporary(null); // no-op safety
			rt.Release();
			Object.Destroy(rt);
		}

		return results;
	}
}
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class Helpers
{
	public static GameObject GetSelectedPrefab()
	{
		var obj = Selection.activeObject as GameObject;
		if (obj == null || PrefabUtility.GetPrefabAssetType(obj) == PrefabAssetType.NotAPrefab)
		{
			return null;
		}
		return obj;
	}

	public static List<GameObject> GetAllSelectedPrefabs()
	{
		var results = new List<GameObject>();
		var seenPaths = new HashSet<string>();


		// 1) Project selection prefab assets
		foreach (var obj in Selection.GetFiltered<GameObject>(SelectionMode.Assets))
		{
			var path = AssetDatabase.GetAssetPath(obj);
			if (string.IsNullOrEmpty(path)) continue;
			if (!path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase)) continue;

			var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			if (!go) continue;

			var type = PrefabUtility.GetPrefabAssetType(go);
			if (type == PrefabAssetType.NotAPrefab) continue; // safety

			if (seenPaths.Add(path))
				results.Add(go);
		}

		return results;
	}

	/// <summary>
	/// Returns a combined Bounds in 'root's local space (pivot-relative),
	/// covering all Renderers under 'root'.
	/// </summary>
	public static Bounds GetCombinedLocalBounds(Transform root, bool includeInactive = false)
	{
		var renderers = root.GetComponentsInChildren<Renderer>(includeInactive);
		bool hasAny = false;
		Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);

		foreach (var r in renderers)
		{
			// World-space AABB for this renderer
			Bounds wb = r.bounds;

			// Get its 8 corners and bring them into root local space
			Vector3[] corners = GetWorldAABBCorners(wb);
			for (int i = 0; i < corners.Length; i++)
				corners[i] = root.InverseTransformPoint(corners[i]);

			// Encapsulate into a local-space bounds
			if (!hasAny)
			{
				localBounds = new Bounds(corners[0], Vector3.zero);
				hasAny = true;
			}
			for (int i = 0; i < corners.Length; i++)
				localBounds.Encapsulate(corners[i]);
		}

		return localBounds; // localBounds.center and .extents are relative to root's pivot
	}

	private static Vector3[] GetWorldAABBCorners(Bounds b)
	{
		var c = b.center; var e = b.extents;
		return new Vector3[]
		{
			new Vector3(c.x - e.x, c.y - e.y, c.z - e.z),
			new Vector3(c.x + e.x, c.y - e.y, c.z - e.z),
			new Vector3(c.x - e.x, c.y + e.y, c.z - e.z),
			new Vector3(c.x + e.x, c.y + e.y, c.z - e.z),
			new Vector3(c.x - e.x, c.y - e.y, c.z + e.z),
			new Vector3(c.x + e.x, c.y - e.y, c.z + e.z),
			new Vector3(c.x - e.x, c.y + e.y, c.z + e.z),
			new Vector3(c.x + e.x, c.y + e.y, c.z + e.z),
		};
	}
}

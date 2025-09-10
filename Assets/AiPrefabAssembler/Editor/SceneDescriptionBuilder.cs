using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class SceneDescriptionBuilder
{
	public static string BuildPrefabSelectionString()
	{
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

			seenPaths.Add(path);
		}

		return string.Join(',', seenPaths);
	}

	public static string BuildSelectionString()
	{
		var direct = Selection.GetFiltered<GameObject>(SelectionMode.TopLevel | SelectionMode.Editable | SelectionMode.ExcludePrefab);

		string res = "";
		foreach(var selection in direct)
		{
			res += $"{selection.GetInstanceID().ToString()},";
		}
		if (res.EndsWith(','))
			res = res.Substring(0, res.Length - 1);

		return res;
	}

	public static string BuildGameObjectDescription(Transform t)
	{
		string guid = t.gameObject.GetInstanceID().ToString();

		string name = t.gameObject.name;

		string parentId = "";
		if (t.parent != null)
			parentId = t.parent.gameObject.GetInstanceID().ToString();

		// Local transforms (hierarchy-relative)
		var p = t.localPosition;
		var r = t.localEulerAngles;
		var s = t.localScale;

		// Format numbers with invariant culture and compact precision
		string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

		// Children list: direct children names
		var childGuids = new StringBuilder();
		for (int i = 0; i < t.childCount; i++)
		{
			if (i > 0) childGuids.Append(',');
			childGuids.Append(t.GetChild(i).gameObject.GetInstanceID().ToString());
		}

		var bounds = Helpers.GetCombinedLocalBounds(t);
		var mi = bounds.min;
		var ma = bounds.max;

		var line = $"[{guid},{name},localPos:({F(p.x)};{F(p.y)};{F(p.z)}),localEuler:({F(r.x)};{F(r.y)};{F(r.z)}),localScale({F(s.x)};{F(s.y)};{F(s.z)}),localExtentsMin({F(mi.x)};{F(mi.y)};{F(mi.z)}),localExtentsMax({F(ma.x)};{F(ma.y)};{F(ma.z)}),childrenUniqueIds({childGuids}),parentUniqueId({parentId})]";
		return line;
	}
}

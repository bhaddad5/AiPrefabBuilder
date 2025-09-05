using System.Globalization;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneDescriptionBuilder
{
	public static string BuildSceneDescription()
	{
		var scene = SceneManager.GetActiveScene();
		if (!scene.IsValid() || !scene.isLoaded)
		{
			Debug.LogError("No active scene loaded.");
			return "";
		}

		var sb = new StringBuilder();
		var roots = scene.GetRootGameObjects();
		System.Array.Sort(roots, (a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

		foreach (var root in roots)
			AppendNodeRecursive(root.transform, sb, 0);

		EditorGUIUtility.systemCopyBuffer = sb.ToString();

		return sb.ToString();
	}

	private static void AppendNodeRecursive(Transform t, StringBuilder sb, int indent)
	{
		sb.AppendLine(FormatNodeLine(t));

		// Children in deterministic order
		for (int i = 0; i < t.childCount; i++)
		{
			AppendNodeRecursive(t.GetChild(i), sb, indent + 1);
		}
	}

	private static string FormatNodeLine(Transform t)
	{
		string guid = t.gameObject.GetInstanceID().ToString();

		string name = t.gameObject.name;

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

		string selectedString = "";
		if (IsDirectlySelected(t.gameObject))
		{
			selectedString = "Selected,";
		}

		// Build the required line format
		// [assetUniqueGuid,assetName,pos:(x;y;z),euler:(x;y;z),scale(x,y,z),children([assetName...])]
		var line = $"[{selectedString}{guid},{name},pos:({F(p.x)};{F(p.y)};{F(p.z)}),euler:({F(r.x)};{F(r.y)};{F(r.z)}),scale({F(s.x)},{F(s.y)},{F(s.z)}),children({childGuids})]";
		return line;
	}

	private static bool IsDirectlySelected(GameObject go)
	{
		if (go == null) return false;
		var direct = Selection.GetFiltered<GameObject>(SelectionMode.TopLevel | SelectionMode.Editable | SelectionMode.ExcludePrefab);
		return direct.Any(s => s == go);
	}
}

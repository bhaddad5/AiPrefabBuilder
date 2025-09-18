using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class Testers
{
	[MenuItem("Forge of Realms - Tests/Test GameObject Description Builder", false, 1000)]
	public static void TestGameObjectDescriptionBuilder()
	{
		var direct = Selection.GetFiltered<GameObject>(SelectionMode.TopLevel | SelectionMode.Editable | SelectionMode.ExcludePrefab);

		if(direct.Length == 0)
		{
			Debug.LogError("No GameObject selected!");
			return;
		}

		string descr = SceneDescriptionBuilder.BuildGameObjectDescription(direct[0].transform);

		Debug.Log(descr);

		Debug.Log("Done!");
	}

	[MenuItem("Forge of Realms - Tests/Test Fetch Models", false, 1000)]
	public static void TestFetchModels()
	{
		var models = ModelFetcher.FetchAllAvailableModels();

		foreach (var model in models)
			Debug.Log(model.ToString());

		Debug.Log("Done!");
	}

	[MenuItem("Forge of Realms - Tests/Test Tag Matching", false, 1000)]
	public static void TestTagMatching()
	{
		ContextLookupTable table = new ContextLookupTable();

		var res = table.SearchPrefabTags(new List<string>() { "skeletal", "bone", "remains" }).Take(25);

		Debug.Log($"Found ({res.Count()}): ");

		foreach (var item in res)
		{
			Debug.Log(item);
		}

		Debug.Log("Done!");
	}

	[MenuItem("Forge of Realms - Tests/Test String Matching", false, 1000)]
	public static void TestStringMatching()
	{
		ContextLookupTable table = new ContextLookupTable();

		var res = table.SearchObjectNames("bone").Take(25);

		Debug.Log($"Found ({res.Count()}): ");

		foreach (var item in res)
		{
			var obj = EditorUtility.InstanceIDToObject(item) as GameObject;

			Debug.Log(obj?.name + ", " + item);
		}

		Debug.Log("Done!");
	}
}

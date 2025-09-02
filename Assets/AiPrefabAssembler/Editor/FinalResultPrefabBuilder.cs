using UnityEditor;
using UnityEngine;

public static class FinalResultPrefabBuilder
{
	public static GameObject BuildPrefabFromInstructions(string buildInstructions)
	{
		Debug.Log($"Building Prefab with Instructions: {buildInstructions}");

		GameObject root = new GameObject("Root");

		bool writing = false;
		string currInstructions = "";
		for (int i = 0; i < buildInstructions.Length; i++)
		{
			if (buildInstructions[i] == '[')
				writing = true;

			if (!writing)
				continue;

			currInstructions += buildInstructions[i];

			if (buildInstructions[i] == ']')
			{
				BuildPrefabFromInstruction(currInstructions, root.transform);

				currInstructions = "";
				writing = false;
			}
		}

		return root;
	}

	const string folder = "Assets/AiPrefabAssembler/Contextualized_Assets";

	private static void BuildPrefabFromInstruction(string instruction, Transform parent)
	{
		string resPrefabPath = "";
		Vector3 resPos = Vector3.zero;
		Vector3 resRot = Vector3.zero;

		instruction = instruction.Trim('[', ']');
		var split = instruction.Split(',');
		foreach (var chunk in split)
		{
			var trimmedChunk = chunk.Trim(' ');

			
			if (trimmedChunk.StartsWith("pos:"))
			{
				trimmedChunk = trimmedChunk.Replace("pos:", "");
				trimmedChunk = trimmedChunk.Trim('(', ')');
				var spl = trimmedChunk.Split(';');
				if (spl.Length != 3)
				{
					Debug.LogError($"Failed to parse instruction {instruction}");
					return;
				}
				float x;
				if (float.TryParse(spl[0], out x))
				{
					resPos.x = x;
				}
				float y;
				if (float.TryParse(spl[1], out y))
				{
					resPos.y = y;
				}
				float z;
				if (float.TryParse(spl[2], out z))
				{
					resPos.z = z;
				}
			}
			else if (trimmedChunk.StartsWith("euler:"))
			{
				trimmedChunk = trimmedChunk.Replace("euler:", "");
				trimmedChunk = trimmedChunk.Trim('(', ')');
				var spl = trimmedChunk.Split(';');
				if (spl.Length != 3)
				{
					Debug.LogError($"Failed to parse instruction {instruction}");
					return;
				}
				float x;
				if (float.TryParse(spl[0], out x))
				{
					resRot.x = x;
				}
				float y;
				if (float.TryParse(spl[1], out y))
				{
					resRot.y = y;
				}
				float z;
				if (float.TryParse(spl[2], out z))
				{
					resRot.z = z;
				}
			}
			else
			{
				resPrefabPath = $"{folder}/{trimmedChunk}.prefab";
			}
		}

		SpawnPrefab(resPrefabPath, resPos, resRot, parent);
	}

	private static void SpawnPrefab(string prefabPath, Vector3 pos, Vector3 rot, Transform parent)
	{
		var asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

		if(asset == null)
		{
			Debug.LogError($"Failed to find prefab {prefabPath}");
			return;
		}

		//Debug.Log($"Spawning Prefab {prefabPath} at {pos},{rot}");

		var ob = GameObject.Instantiate(asset, parent);
		ob.transform.position = pos;
		ob.transform.eulerAngles = rot;
	}
}

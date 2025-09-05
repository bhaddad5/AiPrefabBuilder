using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public static class ContextRequestParser
{
	public static string FunctionName = "RequestContext";

	public static string FunctionDescription = "You may request Context on prefabs and objects. " +
			$"Context can include a short description of the thing, it's current bounds, it's transform, etc." +
			$"Return a list of all the context requests you want. The types of context are: " +
			$"{nameof(ContextRequestImpl.GetPrefabContext)}[prefabPath]. " +
			$"{nameof(ContextRequestImpl.GetObjectContext)}[objectUniqueId]. ";

	public static string BuildContextString(string req)
    {
		List<string> commands = new List<string>() {
			$"{nameof(ContextRequestImpl.GetPrefabContext)}",
			$"{nameof(ContextRequestImpl.GetObjectContext)}"};

		List<string> parsedCommands = CommandParsingHelpers.ParseAllCommands(req, commands);

		List<string> contextStrings = new List<string>();

		foreach (var cmd in parsedCommands)
		{
			Debug.Log(cmd);

			var splitCmd = CommandParsingHelpers.SplitCommand(cmd);

			if (splitCmd.Count == 0)
				continue;

			if (splitCmd[0] == nameof(ContextRequestImpl.GetPrefabContext))
			{
				if (splitCmd.Count != 2)
				{
					Debug.LogError($"Incorrect number of arguments: {cmd}");
				}

				string prefabPath = splitCmd[1];

				var context = ContextRequestImpl.GetPrefabContext(prefabPath);
				if(!string.IsNullOrWhiteSpace(context))
					contextStrings.Add(context);
			}
			if (splitCmd[0] == nameof(ContextRequestImpl.GetObjectContext))
			{
				if (splitCmd.Count != 2)
				{
					Debug.LogError($"Incorrect number of arguments: {cmd}");
				}

				string objectUniqueId = splitCmd[1];

				var context = ContextRequestImpl.GetObjectContext(objectUniqueId);
				if (!string.IsNullOrWhiteSpace(context))
					contextStrings.Add(context);
			}
		}

		string res = "";
		foreach(var str in contextStrings)
		{
			res += str;
		}

		return res;
	}
}

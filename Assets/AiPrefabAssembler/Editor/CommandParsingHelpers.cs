using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CommandParsingHelpers
{
	public static List<string> ParseAllCommands(string allCommands, List<string> validCommands)
	{
		List<string> res = new List<string>();

		while (allCommands.Length > 0)
		{
			string cmd = "";

			bool startsWithCommand = false;
			foreach(var c in validCommands)
			{
				if(allCommands.StartsWith($"{c}["))
				{
					startsWithCommand = true;
					break;
				}
			}

			if (startsWithCommand)
			{
				cmd = GetFirstCommand(allCommands);

				if (cmd == "")
				{
					Debug.LogError($"Failed to parse command from: {allCommands}");
					return res;
				}

				allCommands = allCommands.Substring(cmd.Length);
			}
			else
			{
				allCommands = allCommands.Substring(1);
			}

			if (cmd != "")
				res.Add(cmd);
		}

		return res;
	}

	private static string GetFirstCommand(string commands)
	{
		if (!commands.Contains(']'))
			return "";

		return commands.Substring(0, commands.IndexOf(']') + 1);
	}

	public static List<string> SplitCommand(string command)
	{
		if (command.IndexOf('[') == -1 || command.IndexOf(']') == -1)
		{
			Debug.LogError($"Failed to parse Command: {command}");
			return new List<string>();
		}

		List<string> res = new List<string>();
		res.Add(command.Substring(0, command.IndexOf('[')));

		string args = command.Substring(command.IndexOf('[') + 1, command.Length - (command.IndexOf('[') + 2));

		foreach (var arg in args.Split(','))
		{
			res.Add(arg);
		}

		return res;
	}

	public static Vector3 ParseVec3(string vec3)
	{
		if (vec3.IndexOf('(') == -1 || vec3.IndexOf(')') == -1)
		{
			Debug.LogError($"Failed to parse Vector3: {vec3}");
			return Vector3.zero;
		}

		string vals = vec3.Substring(vec3.IndexOf('(') + 1, vec3.Length - (vec3.IndexOf('(') + 2));

		var split = vals.Split(';');

		if (split.Length != 3)
		{
			Debug.LogError($"Failed to parse Vector3: {vec3} - Incorrect number of values");
			return Vector3.zero;
		}

		float x = 0;
		float y = 0;
		float z = 0;
		if (!float.TryParse(split[0], out x))
			Debug.LogError($"Failed to parse float: {split[0]}");
		if (!float.TryParse(split[1], out y))
			Debug.LogError($"Failed to parse float: {split[1]}");
		if (!float.TryParse(split[2], out z))
			Debug.LogError($"Failed to parse float: {split[2]}");

		return new Vector3(x, y, z);
	}
}

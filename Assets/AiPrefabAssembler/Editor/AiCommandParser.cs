using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlasticPipe.PlasticProtocol.Messages.NegotiationCommand;

public static class AiCommandParser
{
    public static void TryExecuteCommands(string allCommands)
    {
        var impl = new AiCommandImpl();

        List<string> parsedCommands = ParseAllCommands(allCommands);

        foreach (var cmd in parsedCommands)
        {
            Debug.Log(cmd);

            var splitCmd = SplitCommand(cmd);

            if (splitCmd.Count == 0)
                continue;

            if (splitCmd[0] == nameof(AiCommandImpl.CreateObject))
            {
                if(splitCmd.Count != 8)
                {
                    Debug.LogError($"Incorrect number of arguments: {cmd}");
                }

                string creationId = splitCmd[1];
                string prefabPath = splitCmd[2];
                string newObjectName = splitCmd[3];
                Vector3 pos = ParseVec3(splitCmd[4]);
                Vector3 rot = ParseVec3(splitCmd[5]);
                Vector3 scl = ParseVec3(splitCmd[6]);
                string parentId = splitCmd[7];

                impl.CreateObject(creationId, prefabPath, newObjectName, pos, rot, scl, parentId);
			}
			if (splitCmd[0] == nameof(AiCommandImpl.DeleteObject))
			{
				if (splitCmd.Count != 2)
				{
					Debug.LogError($"Incorrect number of arguments: {cmd}");
				}

				string objectUniqueId = splitCmd[1];

				impl.DeleteObject(objectUniqueId);
			}
			if (splitCmd[0] == nameof(AiCommandImpl.SetObjectParent))
			{
				if (splitCmd.Count != 3)
				{
					Debug.LogError($"Incorrect number of arguments: {cmd}");
				}

				string objectUniqueId = splitCmd[1];
				string parentObjectUniqueId = splitCmd[2];

				impl.SetObjectParent(objectUniqueId, parentObjectUniqueId);
			}
			if (splitCmd[0] == nameof(AiCommandImpl.SetObjectTransform))
			{
				if (splitCmd.Count != 5)
				{
					Debug.LogError($"Incorrect number of arguments: {cmd}");
				}

				string objectUniqueId = splitCmd[1];
				Vector3 pos = ParseVec3(splitCmd[2]);
				Vector3 rot = ParseVec3(splitCmd[3]);
				Vector3 scl = ParseVec3(splitCmd[4]);

				impl.SetObjectTransform(objectUniqueId, pos, rot, scl);
			}
		}
    }

    private static List<string> ParseAllCommands(string allCommands)
    {
        List<string> res = new List<string>();

        while(allCommands.Length > 0)
        {
            string cmd = "";

            if(allCommands.StartsWith($"{nameof(AiCommandImpl.CreateObject)}[") ||
				allCommands.StartsWith($"{nameof(AiCommandImpl.DeleteObject)}[") ||
				allCommands.StartsWith($"{nameof(AiCommandImpl.SetObjectParent)}[") ||
				allCommands.StartsWith($"{nameof(AiCommandImpl.SetObjectTransform)}["))
            {
                cmd = GetFirstCommand(allCommands);

                if(cmd == "")
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

        return commands.Substring(0, commands.IndexOf(']')+1);
    }

    private static List<string> SplitCommand(string command)
    {
		if (command.IndexOf('[') == -1 || command.IndexOf(']') == -1)
		{
			Debug.LogError($"Failed to parse Command: {command}");
			return new List<string>();
		}

		List<string> res = new List<string>();
        res.Add(command.Substring(0, command.IndexOf('[')));

        string args = command.Substring(command.IndexOf('[') + 1, command.Length - (command.IndexOf('[') + 2));

        foreach(var arg in args.Split(','))
        {
            res.Add(arg);
        }

        return res;
	}
	private static Vector3 ParseVec3(string vec3)
    {
        if(vec3.IndexOf('(') == -1 || vec3.IndexOf(')') == -1)
        {
            Debug.LogError($"Failed to parse Vector3: {vec3}");
            return Vector3.zero;
        }

		string vals = vec3.Substring(vec3.IndexOf('(') + 1, vec3.Length - (vec3.IndexOf('(') + 2));

        var split = vals.Split(';');

        if(split.Length != 3)
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

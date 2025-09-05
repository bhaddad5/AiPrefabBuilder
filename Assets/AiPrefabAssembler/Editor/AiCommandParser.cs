using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlasticPipe.PlasticProtocol.Messages.NegotiationCommand;

public static class AiCommandParser
{
    public static void TryExecuteCommands(string allCommands)
    {
        var impl = new AiCommandImpl();

		List<string> commands = new List<string>() { 
			$"{nameof(AiCommandImpl.CreateObject)}", 
			$"{nameof(AiCommandImpl.DeleteObject)}", 
			$"{nameof(AiCommandImpl.SetObjectParent)}", 
			$"{nameof(AiCommandImpl.SetObjectTransform)}" };

        List<string> parsedCommands = CommandParsingHelpers.ParseAllCommands(allCommands, commands);

        foreach (var cmd in parsedCommands)
        {
            Debug.Log(cmd);

            var splitCmd = CommandParsingHelpers.SplitCommand(cmd);

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
                Vector3 pos = CommandParsingHelpers.ParseVec3(splitCmd[4]);
                Vector3 rot = CommandParsingHelpers.ParseVec3(splitCmd[5]);
                Vector3 scl = CommandParsingHelpers.ParseVec3(splitCmd[6]);
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
				Vector3 pos = CommandParsingHelpers.ParseVec3(splitCmd[2]);
				Vector3 rot = CommandParsingHelpers.ParseVec3(splitCmd[3]);
				Vector3 scl = CommandParsingHelpers.ParseVec3(splitCmd[4]);

				impl.SetObjectTransform(objectUniqueId, pos, rot, scl);
			}
		}
    }
}

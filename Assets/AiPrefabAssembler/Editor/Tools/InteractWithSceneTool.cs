using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractWithSceneTool : ITool
{
	public string FunctionName => "InteractWithScene";

	public string ActionProgressDescription => "Making Changes to Scene";

	public string FunctionDescription
	{
		get
		{
			return "Use a list of the provided Commands to interact with the scene and fulfil the user's request. ";
		}
	}

	public List<ICommand> AvailableCommands => new List<ICommand>() { new CreateObjectCommand(), new DeleteObjectCommand(), new SetObjectParentCommand(), new SetObjectTransformCommand() };

	public string Execute(string req)
	{
		return CommandHelpers.ExecuteAllCommands(req, AvailableCommands);
	}
}

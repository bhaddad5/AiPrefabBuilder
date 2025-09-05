using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContextRequestTool : ITool
{
	public string FunctionName => "ContextRequest";

	public string FunctionDescription { 
		get {
			return "You may request Context on prefabs and objects. " +
			$"Context can include a short description of the thing, it's current bounds, it's transform, etc." +
			$"Return a list of all the context requests you want. ";
		} }

	public List<ICommand> AvailableCommands => new List<ICommand>() { new GetPrefabContextCommand(), new GetObjectContextCommand() };

	public string Execute(string req)
	{
		return CommandHelpers.ExecuteAllCommands(req, AvailableCommands);
	}
}

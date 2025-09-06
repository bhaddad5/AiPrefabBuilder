using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Tools just expose a series of Commands.  The reason Commands are separate is because the AI will do a lot of them and we wanna batch them up.
public interface ITool
{
	public string FunctionName { get; }
	public string FunctionDescription { get; }
	public string ActionProgressDescription { get; }
	public List<ICommand> AvailableCommands { get; }

	public string Execute(string req);
}

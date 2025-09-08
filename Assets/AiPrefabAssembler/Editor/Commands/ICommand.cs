using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parameter
{
    public string Name;
    public string Description = "";
    public bool Required = true;

	public Parameter(string name, string description = "", bool required = true)
	{
		Name = name;
		Description = description;
		Required = required;
	}

	public string GetParameter(Dictionary<string, string> givenParams)
	{
		if (!givenParams.ContainsKey(Name.ToLowerInvariant()))
		{
			if (Required)
				Debug.LogError($"Required Parameter {Name} not present in Tool call.");
			return "";
		}
		return givenParams[Name];
	}

	public Vector3 GetParameterAsVec3(Dictionary<string, string> givenParams)
	{
		var res = GetParameter(givenParams);
		if (res == "")
			return Vector3.zero;
		return ParseVec3(res);
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

public interface ICommand
{
    public string CommandName { get; }
	public string CommandDescription { get; }

    public List<Parameter> Parameters { get; }

    public string ParseArgsAndExecute(Dictionary<string, string> args);
}

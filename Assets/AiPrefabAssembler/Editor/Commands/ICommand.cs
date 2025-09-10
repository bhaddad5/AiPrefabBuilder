using System;
using System.Collections.Generic;
using UnityEngine;

public class Parameter
{
	public enum ParamType
	{
		String,
		Int,
		Vector3,
	}

    public string Name;
	public ParamType Type = ParamType.String;
	public string Description = "";
    public bool Required = true;

	public Parameter(string name, ParamType type = ParamType.String, string description = "", bool required = true)
	{
		Name = name;
		Type = type;
		Description = description;
		Required = required;
	}

	public T Get<T>(TypedArgs args)
	{
		if(args.Values.ContainsKey(Name) && args.Values[Name] is T t)
		{
			return t;
		}

		Debug.LogError($"Failed to parse arg {Name} in {String.Join(',', args.Values.Keys)}");

		return default;
	}
}

public sealed class TypedArgs
{
	public Dictionary<string, object> Values = new(StringComparer.OrdinalIgnoreCase);
}

public interface ICommand
{
    public string CommandName { get; }
	public string CommandDescription { get; }

    public List<Parameter> Parameters { get; }

    public string ParseArgsAndExecute(TypedArgs args);
}

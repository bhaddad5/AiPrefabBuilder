using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICommand
{
    public string CommandName { get; }
    public string CommandFormattingString { get; }
    public int NumArgs { get; }
    public string ParseArgsAndExecute(List<string> args);
}

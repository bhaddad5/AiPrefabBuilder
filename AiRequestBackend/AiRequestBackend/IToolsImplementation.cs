using System;
using System.Collections.Generic;
using System.Text;

namespace AiRequestBackend
{
    public interface IToolsImplementation
    {
        string GetPartMetadata(string part);
		(string Info, Dictionary<string, BinaryData> Renders) BuildSubPrefab(string instructions);
		(string Info, Dictionary<string, BinaryData> Renders) AttemptFinalBuild(string instructions);
	}
}

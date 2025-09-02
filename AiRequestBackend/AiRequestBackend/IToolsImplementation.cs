using System;
using System.Collections.Generic;
using System.Text;

namespace AiRequestBackend
{
    public interface IToolsImplementation
    {
        string GetPartMetadata(string part);
		(string Info, Dictionary<string, BinaryData> Renders) AnalyzeInstructions(string instructions);
		string BuildSubPrefab(string instructions);
		string InformUserOfCurrentReasoning(string thinking);
	}
}

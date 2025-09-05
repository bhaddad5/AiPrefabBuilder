using System;
using System.Collections.Generic;
using System.Text;

namespace AiRequestBackend
{
    public static class Tools
    {
        public static string GetPrefabsMetadata(IToolsImplementation impl, List<string> req)
        {
            string res = "";
            foreach(var part in req)
            {
                res += impl.GetPartMetadata(part);
			}
            
			return res;
        }

		public static (string Info, Dictionary<string, BinaryData> Renders) AnalyzeInstructions(IToolsImplementation impl, string res)
		{
			return impl.AnalyzeInstructions(res);
		}

		public static string BuildPrefabSubAssembly(IToolsImplementation impl, string res)
        {
            return impl.BuildSubPrefab(res);
        }

		public static string InformUserOfCurrentReasoning(IToolsImplementation impl, string res)
		{
			return impl.InformUserOfCurrentReasoning(res);
		}
	}
}

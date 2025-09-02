using System;
using System.Collections.Generic;
using System.Text;

namespace AiRequestBackend
{
    public static class Tools
    {
        public static string GetPartsMetadata(IToolsImplementation impl, List<string> req)
        {
            string res = "";
            foreach(var part in req)
            {
                res += impl.GetPartMetadata(part);
			}
            
			return res;
        }

        public static (string Info, Dictionary<string, BinaryData> Renders) BuildPrefabSubAssembly(IToolsImplementation impl, string res)
        {
            return impl.BuildSubPrefab(res);
        }

	}
}

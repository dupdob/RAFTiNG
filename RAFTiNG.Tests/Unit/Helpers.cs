namespace RAFTiNG.Tests.Unit
{
    using System.Collections.Generic;

    static internal class Helpers
    {
        public static NodeSettings BuildNodeSettings(string nodeId, IEnumerable<string> nodes)
        {
            List<string> workNodes;
            if (nodes != null)
            {
                workNodes = new List<string>(nodes);
                if (workNodes.Contains(nodeId))
                {
                    workNodes.Remove(nodeId);
                }                
            }
            else
            {
                workNodes = new List<string>();
            }

            var settings = new NodeSettings
                               {
                                   NodeId = nodeId,
                                   TimeoutInMs = 10,
                                   OtherNodes = workNodes.ToArray()
                               };
            return settings;
        }
    }
}
using OPC_UA.Client.Common;

namespace OPC_UA.Client
{
    public class UaNode : Node
    {
        /// <summary>
        /// The UA Id of the node
        /// </summary>
        public string NodeId { get; private set; }

        /// <summary>
        /// Instantiates a UaNode class
        /// </summary>
        /// <param name="name">the name of the node</param>
        /// <param name="nodeId">The UA Id of the node</param>
        /// <param name="parent">The parent node</param>
        internal UaNode(string name, string nodeId, Node parent = null)
          : base(name, parent)
        {
            NodeId = nodeId;
        }
    }
}

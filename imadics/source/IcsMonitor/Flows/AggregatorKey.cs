using System.Net;
using Traffix.Core.Flows;

namespace IcsMonitor.Flows
{
    /// <summary>
    /// Represents a multiflow key used to aggregate records. This is the compound key.
    /// It aggreagtes flows to multiflow such that all flows in the bag have the same 
    /// protocol types, client address, server address and the server port.
    /// </summary>
    /// <param name="ProtocolType">The protocol type.</param>
    /// <param name="ClientIpAddress">The client address.</param>
    /// <param name="ServerIpAddress">The server address.</param>
    /// <param name="ServerPort">The server port.</param>
    public record MultiflowKey(System.Net.Sockets.ProtocolType ProtocolType, IPAddress ClientIpAddress, IPAddress ServerIpAddress, ushort ServerPort);
    /// <summary>
    /// Represents a biflow key used to aggregate the flow records. 
    /// It aggregates the flows of bidirectional conversations. 
    /// </summary>
    /// <param name="ProtocolType">the protocol type.</param>
    /// <param name="ClientIpAddress">The client address.</param>
    /// <param name="ClientPort">The client port.</param>
    /// <param name="ServerIpAddress">The server address.</param>
    /// <param name="ServerPort">The server port.</param>
    public record BiflowKey(System.Net.Sockets.ProtocolType ProtocolType, IPAddress ClientIpAddress, ushort ClientPort, IPAddress ServerIpAddress, ushort ServerPort);
    /// <summary>
    /// This static class provides different aggregation keys. 
    /// </summary>
    public static class AggregatorKey
    {
        /// <summary>
        /// This key aggregates all flows between a client endpoint (any client port) and the server socket endpoint.
        /// </summary>
        /// <param name="arg">The flow key.</param>
        /// <returns>The aggregation key.</returns>
        public static MultiflowKey Multiflow(FlowKey arg)
        {
            if (arg.SourcePort > arg.DestinationPort)  // client to server port
            {
                return new MultiflowKey(arg.ProtocolType, arg.SourceIpAddress, arg.DestinationIpAddress, arg.DestinationPort);
            }
            else   // server to client flow:
            {
                return new MultiflowKey(arg.ProtocolType, arg.DestinationIpAddress, arg.SourceIpAddress, arg.SourcePort);
            }
        }
        /// <summary>
        /// This key aggregates all flows between the client socket endpoint and the server socket endpoint. It corresponds the bidirectional flow (conversation).
        /// </summary>
        /// <param name="arg">The flow key.</param>
        /// <returns>The aggregation key.</returns>
        public static BiflowKey Biflow(FlowKey arg)
        {
            if (arg.SourcePort > arg.DestinationPort)  // client to server port
            {
                return new BiflowKey(arg.ProtocolType, arg.SourceIpAddress, arg.SourcePort, arg.DestinationIpAddress, arg.DestinationPort);
            }
            else   // server to client flow:
            {
                return new BiflowKey(arg.ProtocolType, arg.DestinationIpAddress, arg.DestinationPort, arg.SourceIpAddress, arg.SourcePort);
            }
        }
    }
}
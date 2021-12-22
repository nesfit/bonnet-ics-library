using System.Net;
using Traffix.Core.Flows;

namespace IcsMonitor.Flows
{
    public record MultiflowKey(System.Net.Sockets.ProtocolType ProtocolType, IPAddress ClientIpAddress, IPAddress ServerIpAddress, ushort ServerPort);
    public record BiflowKey(System.Net.Sockets.ProtocolType ProtocolType, IPAddress ClientIpAddress, ushort ClientPort, IPAddress ServerIpAddress, ushort ServerPort);
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
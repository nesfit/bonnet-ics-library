using PacketDotNet;
using SharpPcap;
using System;
using Traffix.Core.Flows;

namespace IcsMonitor.Flows
{
    /// <summary>
    /// Represents a single parsed packet.
    /// </summary>
    /// <param name="Ticks">Packet timestamp in ticks resolution.</param>
    /// <param name="Key">The flow key of the packet.</param>
    /// <param name="Packet">The packet data.</param>
    public record PacketRecord<TPacket>(long Ticks, string Label, FlowKey Key, TPacket Packet)
    {
        public static PacketRecord<Packet> FromFrame(RawCapture arg, string label)
        {
            try
            {
                var packet = arg.GetPacket(); // PacketDotNet.Packet.ParsePacket(arg.LinkLayerType, arg.Data);
                return new PacketRecord<Packet>(arg.Timeval.Date.Ticks, label, packet.GetFlowKey(), packet);
            }
            catch
            {
                return new PacketRecord<Packet>(arg.Timeval.Date.Ticks, label, NullFlowKey.Instance , new NullPacket(NullPacketType.IPv4));
            }
        }
        /// <summary>
        /// Gets the packet timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(Ticks);
    }
}

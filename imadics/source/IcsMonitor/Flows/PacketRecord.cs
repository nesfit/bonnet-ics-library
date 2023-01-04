using PacketDotNet;
using SharpPcap;
using System;
using Traffix.Core.Flows;
using Traffix.Providers.PcapFile;

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
        /// <summary>
        /// Creates a packet record from the raw capture. 
        /// </summary>
        /// <param name="rawCapture">The raw capture of the packet.</param>
        /// <param name="label">The label associated with the packet (if any).</param>
        /// <returns>The packet record for the capture.</returns>
        public static PacketRecord<Packet> FromFrame(RawCapture rawCapture, string label)
        {
            try
            {
                var packet = PacketDotNet.Packet.ParsePacket(rawCapture.LinkLayerType, rawCapture.Data);
                return new PacketRecord<Packet>(rawCapture.Timeval.Date.Ticks, label, packet.GetFlowKey(), packet);
            }
            catch
            {
                return new PacketRecord<Packet>(rawCapture.Timeval.Date.Ticks, label, NullFlowKey.Instance , new NullPacket(NullPacketType.IPv4));
            }
        }
        public static PacketRecord<Packet> FromFrame(RawFrame rawCapture, string label)
        {
            try
            {
                var packet = PacketDotNet.Packet.ParsePacket(rawCapture.LinkLayer, rawCapture.Data);
                return new PacketRecord<Packet>(rawCapture.Ticks, label, packet.GetFlowKey(), packet);
            }
            catch
            {
                return new PacketRecord<Packet>(rawCapture.Ticks, label, NullFlowKey.Instance, new NullPacket(NullPacketType.IPv4));
            }
        }
        /// <summary>
        /// Gets the packet timestamp as DateTime struct.
        /// </summary>
        public DateTime Timestamp => new DateTime(Ticks);
    }
}

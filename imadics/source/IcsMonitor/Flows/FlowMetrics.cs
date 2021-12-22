using System;

namespace IcsMonitor.Flows
{

    /// <summary>
    /// A record of basic flow metrics.
    /// </summary>
    /// <param name="Packets">Number of packet of the flow.</param>
    /// <param name="Octets">Number of octets of the flow.</param>
    /// <param name="FirstSeen">Timestamp of observed first packet of the flow.</param>
    /// <param name="LastSeen">Timestamp of observed last packet of the flow.</param>
    public record FlowMetrics(int Packets, long Octets, long FirstSeen, long LastSeen)
    {
        /// <summary>
        /// The start of the flow as <see cref="DateTime"/> value.
        /// </summary>
        public DateTime Start => new(FirstSeen);
        /// <summary>
        /// The duration of the flow as <see cref="TimeSpan"/> value.
        /// </summary>
        public TimeSpan Duration => new(LastSeen - FirstSeen);

        /// <summary>
        /// Aggregates two flow metrics. 
        /// </summary>
        /// <param name="x">The flow metrics.</param>
        /// <param name="y">The flow metrics.</param>
        /// <returns>Aggregated flow metrics.</returns>
        public static FlowMetrics Aggregate(FlowMetrics x, FlowMetrics y)
        {
            if (x == null) return y;
            if (y == null) return x;
            return new FlowMetrics(x.Packets + y.Packets, x.Octets + y.Octets, MinTicks(x.FirstSeen, y.FirstSeen), MaxTicks(x.LastSeen, y.LastSeen));
        }

        static long MinTicks(long x, long y)
        {
            if (x == 0) return y;
            if (y == 0) return x;
            return Math.Min(x, y);
        }
        static long MaxTicks(long x, long y)
        {
            if (x == 0) return y;
            if (y == 0) return x;
            return Math.Max(x, y);
        }
    }
}
using System;
using Traffix.Core.Flows;

namespace IcsMonitor.Flows
{
    /// <summary>
    /// Generic type of all flow records. It represents bidirectional flow.
    /// <para/>
    /// All flow records have the common properties and the user defined part. 
    /// </summary>
    /// <typeparam name="TData">The type of data of the flow record.</typeparam>
    public record FlowRecord<TKey, TData>
    {
        /// <summary>
        /// An identification of the window to which the flow record belongs.
        /// </summary>
        public string WindowLabel { get; set; }

        /// <summary>
        /// Start of the window to which this flow belongs.
        /// </summary>
        public DateTime WindowStart { get; set; }

        /// <summary>
        /// The duration of the window. 
        /// </summary>
        public TimeSpan WindowDuration { get; set; }

        /// <summary>
        /// User defined flow label. It is often used when we have annotated input data (packets)
        /// and want to label the flow based on this annotation.
        /// </summary>
        public string FlowLabel { get; set; }

        /// <summary>
        /// The flow key. 
        /// </summary>
        public TKey FlowKey { get; set; }
        /// <summary>
        /// The metrics of the client to server flow.
        /// </summary>
        public FlowMetrics ForwardMetrics { get; set; }
        /// <summary>
        /// The metrics of the server to client flow.
        /// </summary>
        public FlowMetrics ReverseMetrics { get; set; }
        /// <summary>
        /// The flow data. 
        /// </summary>
        public TData Data { get; set; }

        /// <summary>
        /// Aggregated metrhocs of bidirectional flow.
        /// </summary>
        public FlowMetrics Metrics => FlowMetrics.Aggregate(ForwardMetrics, ReverseMetrics);
    }
}
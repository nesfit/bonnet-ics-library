using Microsoft.ML.Data;
using System;

namespace IcsMonitor.Modbus
{
    /// <summary>
    /// Represents a flattened record used as a typed version for corresponding Dataviews. 
    /// <para/>
    /// This class is computed from <see cref="Flows.FlowRecord{ModbusCompact}"/> 
    /// and can be used for accesing dataview records. 
    /// </summary>
    public class ModbusDataViewRecord
    {
        /// <summary>
        /// Window label. The flow can be collect in the window. 
        /// </summary>
        [LoadColumn(0)]
        [ColumnName("WindowLabel")]
        public string WindowLabel { get; set; }
        /// Start of the window.
        /// </summary>
        [LoadColumn(1)]
        [ColumnName("WindowStart")]
        public DateTime WindowStart { get; set; }

        /// <summary>
        /// Duration of the window.
        /// </summary>
        [LoadColumn(2)]
        [ColumnName("WindowDuration")]
        public TimeSpan WindowDuration { get; set; }

        /// <summary>
        /// The label of the flow. Can be used for classification. 
        /// </summary>
        [LoadColumn(3)]
        [ColumnName("FlowLabel")]
        public string FlowLabel { get; set; }
        /// <summary>
        /// The flow key. This field is required by the profile.
        /// </summary>
        [LoadColumn(4)]
        [ColumnName("FlowKey")]
        public string FlowKey { get; set; }

        /// <summary>
        /// Duration of forward flow.
        /// </summary>
        [LoadColumn(5)]
        [ColumnName("ForwardMetricsDuration")]
        public float ForwardMetricsDuration { get; set; }

        /// <summary>
        /// Number of octets in the forward flow.
        /// </summary>
        [LoadColumn(6)]
        public float ForwardMetricsOctets { get; set; }

        /// <summary>
        /// Number of packets in the forward flow.
        /// </summary>
        [LoadColumn(7)]
        public float ForwardMetricsPackets { get; set; }

        /// <summary>
        /// Start time of the forward flow.
        /// </summary>
        [LoadColumn(8)]
        public long ForwardMetricsFirstSeen { get; set; }

        /// <summary>
        /// End time of the forward flow.
        /// </summary>
        [LoadColumn(9)]
        public long ForwardMetricsLastSeen { get; set; }

        /// <summary>
        /// Duration of the reverse flow.
        /// </summary>
        [LoadColumn(10)]
        public float ReverseMetricsDuration { get; set; }

        /// <summary>
        /// Number of octets in the reverse flow.
        /// </summary>
        [LoadColumn(11)]
        public float ReverseMetricsOctets { get; set; }

        /// <summary>
        /// Number of packets in the reverse flow.
        /// </summary>
        [LoadColumn(12)]
        public float ReverseMetricsPackets { get; set; }

        /// <summary>
        /// Start time of the reverse flow.
        /// </summary>
        [LoadColumn(13)]
        public long ReverseMetricsFirstSeen { get; set; }

        /// <summary>
        /// End time of the reverse flow.
        /// </summary>
        [LoadColumn(14)]
        public long ReverseMetricsLastSeen { get; set; }

        /// <summary>
        /// The data unit IT value.
        /// </summary>
        [LoadColumn(15)]
        public float DataUnitId { get; set; }

        /// <summary>
        /// Number of read requests.
        /// </summary>
        [LoadColumn(16)]
        public float DataReadRequests { get; set; }

        /// <summary>
        /// Number of write requests.
        /// </summary>
        [LoadColumn(17)]
        public float DataWriteRequests { get; set; }

        /// <summary>
        /// Number of diagnostic requests.
        /// </summary>
        [LoadColumn(18)]
        public float DataDiagnosticRequests { get; set; }

        /// <summary>
        /// Number of other requests.
        /// </summary>
        [LoadColumn(19)]
        public float DataOtherRequests { get; set; }

        /// <summary>
        /// Number of undefined requests.
        /// </summary>
        [LoadColumn(20)]
        public float DataUndefinedRequests { get; set; }

        /// <summary>
        /// Number of correct responses.
        /// </summary>
        [LoadColumn(21)]
        public float DataResponsesSuccess { get; set; }
        /// <summary>
        /// Number of response with error code.
        /// </summary>
        [LoadColumn(22)]
        public float DataResponsesError { get; set; }

        /// <summary>
        /// Number of malformed requests.
        /// </summary>
        [LoadColumn(23)]
        public float DataMalformedRequests { get; set; }


        /// <summary>
        /// Number of malformed responses.
        /// </summary>
        [LoadColumn(24)]
        public float DataMalformedResponses { get; set; }
    }
}

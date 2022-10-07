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

        // ForwardMetrics
        [LoadColumn(5)]
        [ColumnName("ForwardMetricsDuration")]
        public float ForwardMetricsDuration { get; set; }

        [LoadColumn(6)]
        public float ForwardMetricsOctets { get; set; }
        [LoadColumn(7)]
        public float ForwardMetricsPackets { get; set; }
        [LoadColumn(8)]
        public long ForwardMetricsFirstSeen { get; set; }
        [LoadColumn(9)]
        public long ForwardMetricsLastSeen { get; set; }

        // ReverseMetrics
        [LoadColumn(10)]
        public float ReverseMetricsDuration { get; set; }
        [LoadColumn(11)]
        public float ReverseMetricsOctets { get; set; }
        [LoadColumn(12)]
        public float ReverseMetricsPackets { get; set; }
        [LoadColumn(13)]
        public long ReverseMetricsFirstSeen { get; set; }
        [LoadColumn(14)]
        public long ReverseMetricsLastSeen { get; set; }

        // Modbus
        [LoadColumn(15)]
        public float DataUnitId { get; set; }
        [LoadColumn(16)]
        public float DataReadRequests { get; set; }
        [LoadColumn(17)]
        public float DataWriteRequests { get; set; }
        [LoadColumn(18)]
        public float DataDiagnosticRequests { get; set; }
        [LoadColumn(19)]
        public float DataOtherRequests { get; set; }
        [LoadColumn(20)]
        public float DataUndefinedRequests { get; set; }
        [LoadColumn(21)]
        public float DataResponsesSuccess { get; set; }
        [LoadColumn(22)]
        public float DataResponsesError { get; set; }
        [LoadColumn(23)]
        public float DataMalformedRequests { get; set; }
        [LoadColumn(24)]
        public float DataMalformedResponses { get; set; }
    }
}

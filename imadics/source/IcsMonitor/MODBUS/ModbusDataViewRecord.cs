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
        public string WindowLabel;
        /// <summary>
        /// Start of the window.
        /// </summary>
        [LoadColumn(1)]
        [ColumnName("WindowStart")]
        public DateTime WindowStart;

        /// <summary>
        /// Duration of the window.
        /// </summary>
        [LoadColumn(2)]
        [ColumnName("WindowDuration")]
        public TimeSpan WindowDuration;

        /// <summary>
        /// The labe of the flow. Can be used for classification. 
        /// </summary>
        [LoadColumn(3)]
        [ColumnName("FlowLabel")]
        public string FlowLabel;
        /// <summary>
        /// The flow key. This field is required by the profile.
        /// </summary>
        [LoadColumn(4)]
        [ColumnName("FlowKey")]
        public string FlowKey;

        // ForwardMetrics
        [LoadColumn(5)]
        [ColumnName("ForwardMetricsDuration")]
        public double ForwardMetricsDuration;

        [LoadColumn(6)]
        public long ForwardMetricsOctets;
        [LoadColumn(7)]
        public int ForwardMetricsPackets;
        [LoadColumn(8)]
        public long ForwardMetricsFirstSeen;
        [LoadColumn(9)]
        public long ForwardMetricsLastSeen;

        // ReverseMetrics
        [LoadColumn(10)]
        public double ReverseMetricsDuration;
        [LoadColumn(11)]
        public long ReverseMetricsOctets;
        [LoadColumn(12)]
        public int ReverseMetricsPackets;
        [LoadColumn(13)]
        public long ReverseMetricsFirstSeen;
        [LoadColumn(14)]
        public long ReverseMetricsLastSeen;

        // Modbus
        [LoadColumn(15)]
        public byte DataUnitId;
        [LoadColumn(16)]
        public int DataReadRequests;
        [LoadColumn(17)]
        public int DataWriteRequests;
        [LoadColumn(18)]
        public int DataDiagnosticRequests;
        [LoadColumn(19)]
        public int DataOtherRequests;
        [LoadColumn(20)]
        public int DataUndefinedRequests;
        [LoadColumn(21)]
        public int DataResponsesSuccess;
        [LoadColumn(22)]
        public int DataResponsesError;
        [LoadColumn(23)]
        public int DataMalformedRequests;
        [LoadColumn(24)]
        public int DataMalformedResponses;
    }
}

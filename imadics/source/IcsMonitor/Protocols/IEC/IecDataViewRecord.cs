using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using CsvHelper.TypeConversion;
using Microsoft.ML.Data;
using System;
using System.Linq;
using CsvName = CsvHelper.Configuration.Attributes.NameAttribute;
namespace IcsMonitor.Protocols
{
    /// <summary>
    /// This class represents the IEC record as used in ML's DataView. 
    /// </summary>
    /// <remarks>
    /// The Data View record enables to combine individual IEC records
    /// in a single structure suitable for feature extraction in AD methods. 
    /// Using this combination it is possible 
    /// to count a number of different operations occured in the IEC conversation.
    /// For aggregated record, there is <see cref="OperationTagVector"/>
    /// that contains a vector of operation tags. To represents statistics for different operations  
    /// there are two counter vectors <see cref="AsduNumberOfItemsVector"/> and <see cref="IecPacketLengthVector"/>.
    /// </remarks>
    public class IecDataViewRecord
    {

        /// <summary>
        /// Flow identifier.
        /// </summary>
        [ColumnName("FlowLabel")]
        public string FlowLabel { get; set; }

        /// <summary>
        /// Window label/identifier.
        /// </summary>
        [ColumnName("Window")]
        public string Window { get; set; }

        /// <summary>
        /// Start of the window.
        /// </summary>
        [ColumnName("WindowStart")]
        public DateTime WindowStart { get; set; }

        /// <summary>
        /// Duration of the window.
        /// </summary>
        [ColumnName("WindowDuration")]
        public TimeSpan WindowDuration { get; set; }

        /// <summary>
        /// The flow key.
        /// </summary>
        [ColumnName("FlowKey")]
        public string FlowKey => $"{SourceAddress}:{SourcePort}-{DestinationAddress}:{DestinationPort}";

        /// <summary>
        ///  Number of IEC flows aggregated by the record. 
        /// </summary>
        [ColumnName("FLOWS")]
        public int Flows { get; set; } = 1;

        /// <summary>
        /// Start time of the flow.
        /// </summary>
        [ColumnName("START_SEC")]
        public DateTime StartDateTime { get; set; }
        /// <summary>
        /// Source address of the flow.
        /// </summary>

        [ColumnName("L3_IPV4_SRC")]
        public string SourceAddress { get; set; }

        /// <summary>
        /// Destination address of the flow.
        /// </summary>

        [ColumnName("L3_IPV4_DST")]
        public string DestinationAddress { get; set; }

        /// <summary>
        /// Source port of the flow.
        /// </summary>

        [ColumnName("L4_PORT_SRC")]
        public int SourcePort { get; set; }
        /// <summary>
        /// Destination address of the flow.
        /// </summary>

        [ColumnName("L4_PORT_DST")]
        public int DestinationPort { get; set; }

        /// <summary>
        /// Total number of bytes of the flow.
        /// </summary>

        [ColumnName("BYTES")]
        public int Bytes { get; set; }

        /// <summary>
        /// Number of packets of the flow.
        /// </summary>

        [ColumnName("PACKETS")]
        public int Packets { get; set; }

        /// <summary>
        /// Length of IEC packets.
        /// </summary>

        [ColumnName("IEC104_PKT_LENGTH")]
        public int IecPacketLength { get; set; }
        /// <summary>
        /// IEC frame format.
        /// </summary>


        [ColumnName("IEC104_FRAME_FMT")]
        public string IecFrameFormat { get; set; }

        /// <summary>
        /// ASDU Type identifier of the IEC flow.
        /// </summary>


        [ColumnName("IEC104_ASDU_TYPE_IDENT")]
        public string AsduTypeIdentifier { get; set; }

        /// <summary>
        /// ASDU number of items in IEC flow.
        /// </summary>

        [ColumnName("IEC104_ASDU_NUM_ITEMS")]
        public int AsduNumberOfItems { get; set; }

        /// <summary>
        /// Cause of transmission value for the IEC flow.
        /// </summary>

        [ColumnName("IEC104_ASDU_COT")]
        public string CauseOfTransmission { get; set; }

        /// <summary>
        /// ASDU organization value.
        /// </summary>

        [ColumnName("IEC104_ASDU_ORG")]
        public string AsduOrg { get; set; }

        /// <summary>
        /// ASDU address value.
        /// </summary>

        [ColumnName("IEC104_ASDU_ADDRESS")]
        public int AsduAddress { get; set; }

        /// <summary>
        /// Computed operation tag. 
        /// </summary>

        [ColumnName("OPERATION_TAG")]
        public string OperationTag { get; set; }


        /// <summary>
        /// A vector of existing operation tags of the IEC aggregated record.
        /// </summary>

        [ColumnName("OPERATION_TAG_VECTOR")]
        [TypeConverter(typeof(ToStringArrayConverter))]
        public string[] OperationTagVector { get; set; }

        /// <summary>
        /// A vector of IEC packet lenghts of the current IEC aggregated record.
        /// </summary>

        [ColumnName("IEC104_PKT_LENGTH_VECTOR")]
        [TypeConverter(typeof(ToFloatArrayConverter))]
        public float[] IecPacketLengthVector { get; set; }

        /// <summary>
        /// A vector of number of items of the current IEC aggregated record. 
        /// </summary>

        [ColumnName("IEC104_ASDU_NUM_ITEMS_VECTOR")]
        [TypeConverter(typeof(ToFloatArrayConverter))]
        public float[] AsduNumberOfItemsVector { get; set; }

        /// <summary>
        /// Combines two view record in the resulting record.
        /// </summary>
        /// <param name="arg1">The first record.</param>
        /// <param name="arg2">The second record.</param>
        /// <returns>The new record which is a combination of the provided records.</returns>
        internal static IecDataViewRecord Combine(IecDataViewRecord arg1, IecDataViewRecord arg2)
        {
            return new IecDataViewRecord
            {
                FlowLabel = Math.Min(Int32.Parse(arg1.FlowLabel), Int32.Parse(arg2.FlowLabel)).ToString(),
                Flows = arg1.Flows + arg2.Flows,
                StartDateTime = new DateTime(Math.Min(arg1.StartDateTime.Ticks, arg2.StartDateTime.Ticks)),
                SourceAddress = arg1.SourceAddress,
                DestinationAddress = arg1.DestinationAddress,
                SourcePort = arg1.SourcePort,
                DestinationPort = arg1.DestinationPort,
                Bytes = arg1.Bytes + arg2.Bytes,
                Packets = arg1.Packets + arg2.Packets,
                IecPacketLength = arg1.IecPacketLength + arg2.IecPacketLength,
                AsduNumberOfItems = arg1.AsduNumberOfItems + arg2.AsduNumberOfItems,
                OperationTagVector = arg1.OperationTagVector,
                IecPacketLengthVector = SumArray(arg1.IecPacketLengthVector, arg2.IecPacketLengthVector),
                AsduNumberOfItemsVector = SumArray(arg1.AsduNumberOfItemsVector, arg2.AsduNumberOfItemsVector),

            };
        }

        /// <summary>
        /// Computes a pair-wise  sum of two arrays.
        /// </summary>
        /// <param name="x">The first array.</param>
        /// <param name="y">the second array.</param>
        /// <returns>The rusulting array which is a pair-wise sum of two input arrays.</returns>
        static float[] SumArray(float[] x, float[] y)
        {
            var result = new float[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                result[i] = x[i] + y[i];
            }
            return result;
        }

        /// <summary>
        /// A type convertor that convert float array to string and back.
        /// </summary>
        private class ToFloatArrayConverter : TypeConverter
        {
            /// <summary>
            /// Gets float array from its string representation.
            /// </summary>
            /// <param name="text">The input string.</param>
            /// <param name="row">The CSV row.</param>
            /// <param name="memberMapData">A helper object used for mapping.</param>
            /// <returns>The float array obejct.</returns>
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
            {
                if (text == "") return Array.Empty<float>();
                string[] allElements = text.TrimStart('[').TrimEnd(']').Split(',');
                return allElements.Select(s => float.Parse(s)).ToArray();
            }

            /// <summary>
            /// Converts the float array to its string representation.
            /// </summary>
            /// <param name="value">The float array,</param>
            /// <param name="row">The CSV row.</param>
            /// <param name="memberMapData">A helper object used for mapping.</param>
            /// <returns>The string representatin of the input array.</returns>
            public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
            {
                if (value == null) return String.Empty;
                return "[" + string.Join(",", (float[])value) + "]";
            }
        }

        /// <summary>
        /// A type convertor that converts string representation of array to string array and vice versa.
        /// </summary>
        private class ToStringArrayConverter : TypeConverter
        {
            /// <summary>
            /// Gets string array from its string representation.
            /// </summary>
            /// <param name="text">The input string.</param>
            /// <param name="row">The CSV row.</param>
            /// <param name="memberMapData">A helper object used for mapping.</param>
            /// <returns>The string array obejct.</returns>
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
            {
                if (text == "") return new string[0];
                return text.Split(',');
            }
            /// <summary>
            /// Converts the string array to its string representation.
            /// </summary>
            /// <param name="value">The string array,</param>
            /// <param name="row">The CSV row.</param>
            /// <param name="memberMapData">A helper object used for mapping.</param>
            /// <returns>The string representatin of the input array.</returns>
            public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
            {
                if (value == null) return String.Empty;
                return string.Join(",", (string[])value);
            }
        }
    }


    /// <summary>
    /// Represents IEC IPFIX record as produced by Wireshark IEC dissector.
    /// </summary>
    public class IecDataViewRecordWireshark
    {
        //TimeStamp,Relative Time, srcIP, dstIP, srcPort, dstPort, ipLen, len, fmt, uType, asduType, numix, cot, oa, addr, ioa
        
        /// <summary>
        /// Size of the packet in bytes.
        /// </summary>
        [CsvName("ipLen")]
        public int Bytes { get; set; }

        /// <summary>
        /// Number of packets representing the IEC packet/flow.
        /// </summary>
        public int Packets { get; set; } = 1;

        /// <summary>
        /// The timestamp of the packet.
        /// </summary>
        [CsvName("TimeStamp")]
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// the relative time of the packet.
        /// </summary>
        [CsvName("Relative Time")]
        public double RelativeTime { get; set; }
        /// <summary>
        /// The source address of the packet/flow.
        /// </summary>
        
        [CsvName("srcIP")]
        public string SourceAddress { get; set; }
        /// <summary>
        /// The destination address pacekt/flow.
        /// </summary>

        [CsvName("dstIP")]
        public string DestinationAddress { get; set; }
        /// <summary>
        /// The source port of the packet/flow.
        /// </summary>

        [CsvName("srcPort")]
        public string SourcePort { get; set; }
        /// <summary>
        /// The destination port of the packet/flow.
        /// </summary>


        [CsvName("dstPort")]
        public string DestinationPort { get; set; }

        /// <summary>
        /// The IEC packet length.
        /// </summary>

        [CsvName("len")]
        public string IecPacketLength { get; set; }

        /// <summary>
        /// The IEC packet format.
        /// </summary>
        [CsvName("fmt")]
        public string IecFrameFormat { get; set; }

        /// <summary>
        /// The ASDU type.
        /// </summary>

        [CsvName("asduType")]
        public string AsduTypeIdentifier { get; set; }

        /// <summary>
        /// A number of items in ASDU IEC packet.
        /// </summary>

        [CsvName("numix")]
        public string AsduNumberOfItems { get; set; }

        /// <summary>
        /// The cause of transission of the IEC packet.
        /// </summary>

        [CsvName("cot")]
        public string CauseOfTransmission { get; set; }

        /// <summary>
        /// Organization number of IEC packet.
        /// </summary>

        [CsvName("oa")]
        public string AsduOrg { get; set; }

        /// <summary>
        /// ASDU address of IEC packet.
        /// </summary>

        [CsvName("addr")]
        public string AsduAddress { get; set; }

    }
    /// <summary>
    /// Represents IEC IPFIX record as defined by Flowmon. It is loaded by CsvHelper
    /// and thus its properties need to be annotated with <seealso cref="CsvName"/> attribute.
    /// </summary>
    public class IecDataViewRecordFlowmon
    {
        /// <summary>
        /// Export counter generated by Flowmon appliance.
        /// </summary>
        [CsvName("EXPORT_COUNTER")]
        public int ExportCounter { get; set; }

        /// <summary>
        /// Number of bytes of the IEC flow.
        /// </summary>

        [CsvName("BYTES")]
        public int Bytes { get; set; }
        /// <summary>
        /// Number of packet of the IEC flow.
        /// </summary>

        [CsvName("PACKETS")]
        public int Packets { get; set; }
        /// <summary>
        /// the timestamp of the start of IEC flow.
        /// </summary>

        [CsvName("START_SEC")]
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// The timestamp of the end of IEC flow.
        /// </summary>

        [CsvName("END_SEC")]
        public DateTime EndDateTime { get; set; }

        /// <summary>
        /// IEC flow source address.
        /// </summary>

        [CsvName("L3_IPV4_SRC")]
        public string SourceAddress { get; set; }

        /// <summary>
        /// IECflow  destination address.
        /// </summary>

        [CsvName("L3_IPV4_DST")]
        public string DestinationAddress { get; set; }

        /// <summary>
        /// IEC flow source port.
        /// </summary>

        [CsvName("L4_PORT_SRC")]
        public string SourcePort { get; set; }

        /// <summary>
        /// IEC flow destination port.
        /// </summary>

        [CsvName("L4_PORT_DST")]
        public string DestinationPort { get; set; }

        /// <summary>
        /// IEC packet length aggregated for IEC flow.
        /// </summary>

        [CsvName("IEC104_PKT_LENGTH")]
        public string IecPacketLength { get; set; }


        /// <summary>
        /// IEC frame format.
        /// </summary>
        [CsvName("IEC104_FRAME_FMT")]
        public string IecFrameFormat { get; set; }

        /// <summary>
        /// IEC ASDU type.
        /// </summary>

        [CsvName("IEC104_ASDU_TYPE_IDENT")]
        public string AsduTypeIdentifier { get; set; }



        /// <summary>
        /// A number of items in the ASDU message.
        /// </summary>
        [CsvName("IEC104_ASDU_NUM_ITEMS")]
        public string AsduNumberOfItems { get; set; }


        /// <summary>
        /// The cause of transmission value. 
        /// </summary>

        [CsvName("IEC104_ASDU_COT")]
        public string CauseOfTransmission { get; set; }


        /// <summary>
        /// ASDU organization value in IEC message.
        /// </summary>

        [CsvName("IEC104_ASDU_ORG")]
        public string AsduOrg { get; set; }


        /// <summary>
        /// ASDU address as ocurred in IEC message.
        /// </summary>

        [CsvName("IEC104_ASDU_ADDRESS")]
        public string AsduAddress { get; set; }
    }
}
// https://docs.microsoft.com/en-us/dotnet/machine-learning/how-to-guides/prepare-data-ml-net
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

        [ColumnName("FlowKey")]
        public string FlowKey => $"{SourceAddress}:{SourcePort}-{DestinationAddress}:{DestinationPort}";

        [ColumnName("FLOWS")]
        public int Flows { get; set; } = 1;

        [ColumnName("START_SEC")]
        public DateTime StartDateTime { get; set; }

        [ColumnName("L3_IPV4_SRC")]
        public string SourceAddress { get; set; }

        [ColumnName("L3_IPV4_DST")]
        public string DestinationAddress { get; set; }


        [ColumnName("L4_PORT_SRC")]
        public int SourcePort { get; set; }

        [ColumnName("L4_PORT_DST")]
        public int DestinationPort { get; set; }


        [ColumnName("BYTES")]
        public int Bytes { get; set; }


        [ColumnName("PACKETS")]
        public int Packets { get; set; }


        [ColumnName("IEC104_PKT_LENGTH")]
        public int IecPacketLength { get; set; }


        [ColumnName("IEC104_FRAME_FMT")]
        public string IecFrameFormat { get; set; }


        [ColumnName("IEC104_ASDU_TYPE_IDENT")]
        public string AsduTypeIdentifier { get; set; }

        [ColumnName("IEC104_ASDU_NUM_ITEMS")]
        public int AsduNumberOfItems { get; set; }


        [ColumnName("IEC104_ASDU_COT")]
        public string CauseOfTransmission { get; set; }


        [ColumnName("IEC104_ASDU_ORG")]
        public string AsduOrg { get; set; }


        [ColumnName("IEC104_ASDU_ADDRESS")]
        public int AsduAddress { get; set; }

        [ColumnName("OPERATION_TAG")]
        public string OperationTag { get; set; }

        [ColumnName("OPERATION_TAG_VECTOR")]
        [TypeConverter(typeof(ToStringArrayConverter))]
        public string[] OperationTagVector { get; set; }


        [ColumnName("IEC104_PKT_LENGTH_VECTOR")]
        [TypeConverter(typeof(ToFloatArrayConverter))]
        public float[] IecPacketLengthVector { get; set; }

        [ColumnName("IEC104_ASDU_NUM_ITEMS_VECTOR")]
        [TypeConverter(typeof(ToFloatArrayConverter))]
        public float[] AsduNumberOfItemsVector { get; set; }

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
                IecPacketLengthVector = SumArray(arg1.IecPacketLengthVector, arg2.IecPacketLengthVector),
                AsduNumberOfItemsVector = SumArray(arg1.AsduNumberOfItemsVector, arg2.AsduNumberOfItemsVector),

            };
        }
        static float[] SumArray(float[] x, float[] y)
        {
            var result = new float[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                result[i] = x[i] + y[i];
            }
            return result;
        }

        private class ToFloatArrayConverter : TypeConverter
        {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
            {
                if (text == "") return new float[0];
                string[] allElements = text.Split(',');
                return allElements.Select(s => float.Parse(s)).ToArray();
            }

            public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
            {
                if (value == null) return String.Empty;
                return string.Join(",", (float[])value);
            }
        }

        private class ToStringArrayConverter : TypeConverter
        {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
            {
                if (text == "") return new string[0];
                return text.Split(',');
            }

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
        
        [CsvName("ipLen")]
        public int Bytes { get; set; }

        public int Packets { get; set; } = 1;

        [CsvName("TimeStamp")]
        public DateTime StartDateTime { get; set; }

        [CsvName("Relative Time")]
        public double RelativeTime { get; set; }
        
        [CsvName("srcIP")]
        public string SourceAddress { get; set; }

        [CsvName("dstIP")]
        public string DestinationAddress { get; set; }

        [CsvName("srcPort")]
        public string SourcePort { get; set; }


        [CsvName("dstPort")]
        public string DestinationPort { get; set; }


        [CsvName("len")]
        public string IecPacketLength { get; set; }


        [CsvName("fmt")]
        public string IecFrameFormat { get; set; }


        [CsvName("asduType")]
        public string AsduTypeIdentifier { get; set; }


        [CsvName("numix")]
        public string AsduNumberOfItems { get; set; }

        [CsvName("cot")]
        public string CauseOfTransmission { get; set; }


        [CsvName("oa")]
        public string AsduOrg { get; set; }


        [CsvName("addr")]
        public string AsduAddress { get; set; }

    }
    /// <summary>
    /// Represents IEC IPFIX record as defined by Flowmon. It is loaded by CsvHelper
    /// and thus its properties needs to be annotated with Name attribute.
    /// </summary>
    public class IecDataViewRecordFlowmon
    {
        [CsvName("EXPORT_COUNTER")]
        public int ExportCounter { get; set; }

        [CsvName("BYTES")]
        public int Bytes { get; set; }

        [CsvName("PACKETS")]
        public int Packets { get; set; }

        [CsvName("START_SEC")]
        public DateTime StartDateTime { get; set; }

        [CsvName("END_SEC")]
        public DateTime EndDateTime { get; set; }

        [CsvName("L3_IPV4_SRC")]
        public string SourceAddress { get; set; }

        [CsvName("L3_IPV4_DST")]
        public string DestinationAddress { get; set; }

        [CsvName("L4_PORT_SRC")]
        public string SourcePort { get; set; }


        [CsvName("L4_PORT_DST")]
        public string DestinationPort { get; set; }


        [CsvName("IEC104_PKT_LENGTH")]
        public string IecPacketLength { get; set; }


        [CsvName("IEC104_FRAME_FMT")]
        public string IecFrameFormat { get; set; }


        [CsvName("IEC104_ASDU_TYPE_IDENT")]
        public string AsduTypeIdentifier { get; set; }


        [CsvName("IEC104_ASDU_NUM_ITEMS")]
        public string AsduNumberOfItems { get; set; }

        [CsvName("IEC104_ASDU_COT")]
        public string CauseOfTransmission { get; set; }


        [CsvName("IEC104_ASDU_ORG")]
        public string AsduOrg { get; set; }


        [CsvName("IEC104_ASDU_ADDRESS")]
        public string AsduAddress { get; set; }
    }
}
// https://docs.microsoft.com/en-us/dotnet/machine-learning/how-to-guides/prepare-data-ml-net
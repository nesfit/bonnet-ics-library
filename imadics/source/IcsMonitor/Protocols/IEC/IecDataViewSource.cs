using CsvHelper;
using CsvHelper.Configuration;
using IcsMonitor.Flows;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;
using PacketDotNet;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Traffix.Core.Flows;

namespace IcsMonitor.Protocols
{
    /// <summary>
    /// Implements IEC data view source.
    /// </summary>
    [Serializable]
    internal class IecDataViewSource : FlowsDataViewSource<PacketRecord<Packet>, IecCompact>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public IecDataViewSource(IDictionary<string, string> configuration) : base(configuration)
        { 
        }

        /// <summary>
        /// A list of feature columns for the dataview.
        /// </summary>
        public override string[] FeatureColumns => new[] { "IEC104_PKT_LENGTH_VECTOR" }; //, "IEC104_ASDU_NUM_ITEMS_VECTOR" };

        /// <inheritdoc/>
        public override Task<IDataView> GetDataViewAsync<TKey>(MLContext ml, IObservable<FlowRecord<TKey, IecCompact>> observable)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override IObservable<System.Collections.Generic.List<FlowRecord<TKey, IecCompact>>> LoadDataFrom<TKey>(IObservable<PacketRecord<Packet>> source, TimeSpan windowSpan, Func<FlowKey, TKey> getKey)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Loads IPFIX records from Flowmon generated CSV file using IEC104 plugin. 
        /// </summary>
        /// <param name="mlContext"></param>
        /// <param name="file">The input file.</param>
        /// <returns>DataView that can be used for further analysis.</returns>
        public override IDataView LoadFromCsvFile(MLContext mlContext, string file)
        {
            switch (DetectInputFile(file, out var delimiter))
            {
                case CsvFileType.Flowmon: return LoadFromFlowmonCsvFile(mlContext, file, delimiter);
                case CsvFileType.Wireshark: return LoadFromWiresharkCsvFile(mlContext, file, delimiter);
                default:
                    throw new ArgumentException("Cannot read input file. The file structure was not recognized.");
            }
        }

        /// <summary>
        /// The type of CSV file to consume.
        /// </summary>
        enum CsvFileType { Flowmon, Wireshark, Unknown, Invalid }  
        /// <summary>
        /// Detects kind of input CSV file.
        /// </summary>
        /// <param name="file">The input file.</param>
        /// <param name="delimiter">The identified CSV delimiter. Each format can use its won delimiting character.</param>
        /// <returns>The kind of CSV file.</returns>
        CsvFileType DetectInputFile(string file, out char delimiter)
        {
            using var stream = File.OpenRead(file);
            using var reader = new StreamReader(stream);
            var firstLine = reader.ReadLine();
            var commaChars = firstLine.Count(x => x == ',');
            var semicChars = firstLine.Count(x => x == ';');
            delimiter = commaChars > semicChars ? ',' : ';';

            if (firstLine == null) return CsvFileType.Invalid;
            if (firstLine.Contains("L3_IPV4_SRC")) return CsvFileType.Flowmon;
            if (firstLine.Contains("srcIP")) return CsvFileType.Wireshark;
            return CsvFileType.Unknown;
        }

        /// <summary>
        /// Loads source data from Wireshark's CSV file.
        /// </summary>
        /// <param name="mlContext">THe ML context.</param>
        /// <param name="file">The source CSV file.</param>
        /// <param name="delimiter">The CSV delimiter.</param>
        /// <returns>A new data view object.</returns>
        public IDataView LoadFromWiresharkCsvFile(MLContext mlContext, string file, char delimiter)
        {
            var inputList = ReadRecordsFromWiresharkCsvFile(file, delimiter);
            return CreateDataView(mlContext, inputList, Path.ChangeExtension(file, "dump.csv"));
        }
        /// <summary>
        /// Loads source data from Flowmon's CSV file.
        /// </summary>
        /// <param name="mlContext">THe ML context.</param>
        /// <param name="file">The source CSV file.</param>
        /// <param name="delimiter">The CSV delimiter.</param>
        /// <returns>A new data view object.</returns>
        public IDataView LoadFromFlowmonCsvFile(MLContext mlContext, string file, char delimiter)
        {
            var inputList = ReadRecordsFromFlowmonCsvFile(file, delimiter);
            return CreateDataView(mlContext, inputList, Path.ChangeExtension(file, "dump.csv"));
        }

        /// <summary>
        /// Creats a data view based on the provided enumerable of <seealso cref="IecDataViewRecord"/> objects.
        /// </summary>
        /// <param name="mlContext">the ML context.</param>
        /// <param name="inputList">The input enumerable.</param>
        /// <param name="dumpFile">Optional dump file to be populated with aggregated flow records.</param>
        /// <returns>The new  <seealso cref="IDataView"/> object consisting of data from <paramref name="inputList"/>.</returns>
        private IDataView CreateDataView(MLContext mlContext, IEnumerable<IecDataViewRecord> inputList, string dumpFile = null)
        {
            var windowTimeSpan = TimeSpan.Parse(_configuration["window"]);

            // if the tags are already identified, we use them, otherwise the tags are learned from input data
            if (!_configuration.TryGetValue("tags", out var tagsString))
            {
                tagsString = String.Join(',', inputList.Select(x => x.OperationTag).Distinct().Prepend("*").ToArray());
                _configuration["tags"] = tagsString;
            }

            var tags = tagsString.Split(',');
            var encoder = new TagEncoder(tags);
            var encodedList = inputList.Select(encoder.Encode).GroupBy(x => (FlowKey: x.FlowKey, Window: GetWindow(x.StartDateTime, windowTimeSpan))).Select(Aggregate).ToList();
            // it is necessary to set the size of vector for ML.NET
            if (dumpFile!=null) WriteRecordsToCsvFile(dumpFile, encodedList);

            var endcodedDataSchema = SchemaDefinition.Create(typeof(IecDataViewRecord));
            endcodedDataSchema["IEC104_PKT_LENGTH_VECTOR"].ColumnType = new VectorDataViewType(NumberDataViewType.Single, tags.Length);
            endcodedDataSchema["IEC104_ASDU_NUM_ITEMS_VECTOR"].ColumnType = new VectorDataViewType(NumberDataViewType.Single, tags.Length);
            var encodedDataView = mlContext.Data.LoadFromEnumerable<IecDataViewRecord>(encodedList, endcodedDataSchema);
            return encodedDataView;
        }

        /// <summary>
        /// Computes the window start from the given flow start and time window span.
        /// </summary>
        /// <param name="startDateTime">Flow start.</param>
        /// <param name="windowTimeSpan">Window time span.</param>
        /// <returns>Window start in ticks.</returns>
        private (long,long) GetWindow(DateTime startDateTime, TimeSpan windowTimeSpan)
        {
            var windowStartDateTime = new DateTime((startDateTime.Ticks / windowTimeSpan.Ticks) * windowTimeSpan.Ticks);
            return (windowStartDateTime.Ticks, windowTimeSpan.Ticks);
        }

        /// <summary>
        /// Write all <paramref name="records"/> to the given <paramref name="file"/>.
        /// </summary>
        /// <param name="file">The output file.</param>
        /// <param name="records">An enumerable of records to write.</param>
        private static void WriteRecordsToCsvFile(string file, IEnumerable<IecDataViewRecord> records)
        {
            using (var writer = new StreamWriter(Path.ChangeExtension(file, "flows.csv")))
            using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csvWriter.WriteRecords(records);
            }
        }
        /// <summary>
        /// Read <see cref="IecDataViewRecord"/> objects from the given Flowmon's CSV file.
        /// </summary>
        /// <param name="file">The source file.</param>
        /// <param name="delimiter">A delimiter character.</param>
        /// <returns>The enumerbale of  <see cref="IecDataViewRecord"/> objects.</returns>
        private IEnumerable<IecDataViewRecord> ReadRecordsFromFlowmonCsvFile(string file, char delimiter)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter.ToString(),
                PrepareHeaderForMatch = args => args.Header.Trim(),
                MissingFieldFound = null 
            };
            using var reader = new StreamReader(file);
            using var csvReader = new CsvReader(reader, config);
            return csvReader.GetRecords<IecDataViewRecordFlowmon>().Select(FlowmonToNativeRowMapping).ToList();
        }
        /// <summary>
        /// Read <see cref="IecDataViewRecord"/> objects from the given Wireshark's CSV file.
        /// </summary>
        /// <param name="file">The source file.</param>
        /// <param name="delimiter">A delimiter character.</param>
        /// <returns>The enumerbale of  <see cref="IecDataViewRecord"/> objects.</returns>
        private IEnumerable<IecDataViewRecord> ReadRecordsFromWiresharkCsvFile(string file, char delimiter)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter.ToString(),
                PrepareHeaderForMatch = args => args.Header.Trim(),
                MissingFieldFound = null,
                HeaderValidated = null
            };
            using var reader = new StreamReader(file);
            using var csvReader = new CsvReader(reader, config);
            var records =  csvReader.GetRecords<IecDataViewRecordWireshark>().Select(WiresharkToNativeRowMapping).ToList();
            OriginDateTime = records.First().StartDateTime;
            return records;
        }

        /// <summary>
        /// Aggregates a group of <see cref="IecDataViewRecord"/>s in the single record.
        /// It uses <see cref="IecDataViewRecord.Combine(IecDataViewRecord, IecDataViewRecord)"/> method to 
        /// create a resulting aggregation.
        /// </summary>
        /// <param name="arg1">Group of <see cref="IecDataViewRecord"/>s. </param>
        /// <returns>A single <see cref="IecDataViewRecord"/>aggregating a collection of recrods.</returns>
        private IecDataViewRecord Aggregate(IGrouping<(string FlowKey, (long Start, long Duration) Window), IecDataViewRecord> arg1)
        {
            var result = arg1.Aggregate(IecDataViewRecord.Combine);
            result.Window = arg1.Key.Window.Start.ToString();
            result.WindowStart = new DateTime(arg1.Key.Window.Start);
            result.WindowDuration = new TimeSpan(arg1.Key.Window.Duration);
            return result;
        }

        /// <summary>
        /// Encoder for TAG values. It helps with creating a TAG array and 
        /// representing TAG-related counters.
        /// </summary>
        class TagEncoder 
        {
            /// <summary>
            /// An array of tags.
            /// </summary>
            string[] _tags;
            /// <summary>
            /// Maps tag string to its index in the Tag array.
            /// </summary>
            Dictionary<string, int> _tagMap;

            /// <summary>
            /// Creates a new <see cref="TagEncoder"/> based on the provided array of Tags.
            /// </summary>
            /// <param name="tags">An array of operation Tags.</param>
            public TagEncoder(string[] tags)
            {
                _tags = tags;
                _tagMap = tags.Select((x, i) => new KeyValuePair<string, int>(x, i)).ToDictionary(x => x.Key, x => x.Value);
            }

            /// <summary>
            /// Encode the TAG of the given <paramref name="record"/>.
            /// </summary>
            /// <param name="record">The input record.</param>
            /// <returns>The same instance as the input <paramref name="record"/> but with update TAG-related properties. </returns>
            public IecDataViewRecord Encode(IecDataViewRecord record)
            {
                var operationIndex  = _tagMap.TryGetValue(record.OperationTag, out var index) ? index : 0;
                var numOfItemsVector = new float[_tags.Length];               
                numOfItemsVector[operationIndex] = record.AsduNumberOfItems;
                record.AsduNumberOfItemsVector = numOfItemsVector;
                var packetLengthVector = new float[_tags.Length];
                packetLengthVector[operationIndex] = record.IecPacketLength;
                record.IecPacketLengthVector = packetLengthVector;
                record.OperationTagVector = _tags;
                
                return record;
            }
        }

        /// <summary>
        /// Converts Flowmon's IEC record to Data View record.
        /// </summary>
        /// <param name="input">The Flomwon's IEC record.</param>
        /// <returns><see cref="IecDataViewRecord"/> for the given Flowmon's <paramref name="input"/> record. </returns>
        private IecDataViewRecord FlowmonToNativeRowMapping(IecDataViewRecordFlowmon input)
        {
            var output = new IecDataViewRecord();
            output.AsduAddress = Int32.TryParse(input.AsduAddress, out var asduAddress)? asduAddress : 0;
            output.AsduNumberOfItems = Int32.TryParse(input.AsduNumberOfItems, out var asduNumberOfItems)? asduNumberOfItems : 0;
            output.AsduOrg = input.AsduOrg;
            output.AsduTypeIdentifier = input.AsduTypeIdentifier;
            output.Bytes = input.Bytes;
            output.CauseOfTransmission = input.CauseOfTransmission;
            output.DestinationAddress = input.DestinationAddress;
            output.DestinationPort = Int32.TryParse(input.DestinationPort, out var destinationPort) ? destinationPort : 0;
            output.FlowLabel = input.ExportCounter.ToString();
            output.IecFrameFormat = input.IecFrameFormat;
            output.IecPacketLength = Int32.TryParse(input.IecPacketLength, out var iecPacketLength)? iecPacketLength : 0;
            output.Packets = input.Packets;
            output.SourceAddress = input.SourceAddress;
            output.SourcePort = Int32.TryParse(input.SourcePort, out var sourcePort) ? sourcePort : 0;
            output.OperationTag = GetTagString(input);
            output.StartDateTime = input.StartDateTime;
            return output;
        }

        /// <summary>
        /// Extracts the TAG from the <paramref name="input"/> record.
        /// </summary>
        /// <param name="input">The input IEC record.</param>
        /// <returns>the TAG string for the <paramref name="input"/> record.</returns>
        private string GetTagString(IecDataViewRecordFlowmon input)
        {
            return $"{input.IecFrameFormat}.{input.CauseOfTransmission}";
        }
        /// <summary>
        /// Extracts the TAG from the <paramref name="input"/> record.
        /// </summary>
        /// <param name="input">The input IEC record.</param>
        /// <returns>the TAG string for the <paramref name="input"/> record.</returns>
        private string GetTagString(IecDataViewRecordWireshark input)
        {
            return $"{input.IecFrameFormat}.{input.CauseOfTransmission}";
        }


        private DateTime? OriginDateTime = null;
        /// <summary>
        /// Converts Wireshark's IEC record to Data View record.
        /// </summary>
        /// <param name="input">The Wireshark's IEC record.</param>
        /// <returns><see cref="IecDataViewRecord"/> for the given Wireshark's <paramref name="input"/> record. </returns>
        private IecDataViewRecord WiresharkToNativeRowMapping(IecDataViewRecordWireshark input, int recordIndex)
        {
            if (OriginDateTime == null) OriginDateTime = input.StartDateTime;

            var output = new IecDataViewRecord();
            output.AsduAddress = Int32.TryParse(input.AsduAddress, out var asduAddress) ? asduAddress : 0;
            output.AsduNumberOfItems = Int32.TryParse(input.AsduNumberOfItems, out var asduNumberOfItems) ? asduNumberOfItems : 0;
            output.AsduOrg = input.AsduOrg;
            output.AsduTypeIdentifier = input.AsduTypeIdentifier;
            output.Bytes = input.Bytes;
            output.CauseOfTransmission = input.CauseOfTransmission;
            output.DestinationAddress = input.DestinationAddress;
            output.DestinationPort = Int32.TryParse(input.DestinationPort, out var destinationPort) ? destinationPort : 0;
            output.FlowLabel = recordIndex.ToString();
            output.IecFrameFormat = input.IecFrameFormat;
            output.IecPacketLength = Int32.TryParse(input.IecPacketLength, out var iecPacketLength) ? iecPacketLength : 0;
            output.Packets = input.Packets;
            output.SourceAddress = input.SourceAddress;
            output.SourcePort = Int32.TryParse(input.SourcePort, out var sourcePort) ? sourcePort : 0;
            output.OperationTag = GetTagString(input);
            output.StartDateTime = (OriginDateTime ?? DateTime.Today) + TimeSpan.FromSeconds(input.RelativeTime);
            return output;
        }


        /// <inheritdoc/>
        public override IObservable<PacketRecord<Packet>> LoadFromDevice(ICaptureDevice captureDevice, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }


        /// <inheritdoc/>
        public override IObservable<PacketRecord<Packet>> LoadFromFile(string inputCaptureFile, string inputLabelFile, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
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
    [Serializable]
    internal class IecDataViewSource : FlowsDataViewSource<PacketRecord<Packet>, IecCompact>
    {
        public IecDataViewSource(IDictionary<string, string> configuration) : base(configuration)
        {
        }

        public override string[] FeatureColumns => new[] { "IEC104_PKT_LENGTH_VECTOR", "IEC104_ASDU_NUM_ITEMS_VECTOR" };
        public override Task<IDataView> GetDataViewAsync<TKey>(MLContext ml, IObservable<FlowRecord<TKey, IecCompact>> observable)
        {
            throw new NotImplementedException();
        }

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
            var inputList = ReadRecordsFromCsvFile(file);

            // if the tags are already identified, we use them, otherwise the tags are learned from input data
            if (!_configuration.TryGetValue("tags", out var tagsString))
            {
                tagsString = String.Join(',', inputList.Select(x => x.OperationTag).Distinct().ToArray());
                _configuration["tags"] = tagsString;
            }

            var tags = tagsString.Split(',');
            var encoder = new TagEncoder(tags);
            var encodedList = inputList.Select(encoder.Encode).GroupBy(x => (FlowKey: x.FlowKey, Window: x.FlowId / 20)).Select(Aggregate).ToList();

            WriteRecordsToCsvFile(file, encodedList);

            // it is necessary to set the size of vector for ML.NET
            var endcodedDataSchema = SchemaDefinition.Create(typeof(IecDataViewRecord));
            endcodedDataSchema["IEC104_PKT_LENGTH_VECTOR"].ColumnType = new VectorDataViewType(NumberDataViewType.Single, tags.Length);
            endcodedDataSchema["IEC104_ASDU_NUM_ITEMS_VECTOR"].ColumnType = new VectorDataViewType(NumberDataViewType.Single, tags.Length);
            var encodedDataView = mlContext.Data.LoadFromEnumerable<IecDataViewRecord>(encodedList, endcodedDataSchema);
            return encodedDataView;
        }

        private static void WriteRecordsToCsvFile(string file, IEnumerable<IecDataViewRecord> records)
        {
            using (var writer = new StreamWriter(Path.ChangeExtension(file, "flows.csv")))
            using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csvWriter.WriteRecords(records);
            }
        }

        private IEnumerable<IecDataViewRecord> ReadRecordsFromCsvFile(string file)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.Trim(),
            };
            using var reader = new StreamReader(file);
            using var csvReader = new CsvReader(reader, config);
            return csvReader.GetRecords<IecDataViewRecordFlowmon>().Select(FlowmonToNativeRowMapping).ToList();
        }

        private IecDataViewRecord Aggregate(IGrouping<(string FlowKey, int Window), IecDataViewRecord> arg1)
        {
            var result = arg1.Aggregate(IecDataViewRecord.Combine);
            result.Window = arg1.Key.Window.ToString();
            return result;
        }

        class TagEncoder 
        {
            string[] _tags;
            Dictionary<string, int> _tagMap;

            public TagEncoder(string[] tags)
            {
                _tags = tags;
                _tagMap = tags.Select((x, i) => new KeyValuePair<string, int>(x, i)).ToDictionary(x => x.Key, x => x.Value);
            }
            public IecDataViewRecord Encode(IecDataViewRecord record)
            {
                var numOfItemsVector = new float[_tags.Length];
                numOfItemsVector[_tagMap[record.OperationTag]] = record.AsduNumberOfItems;
                record.AsduNumberOfItemsVector = numOfItemsVector;
                var packetLengthVector = new float[_tags.Length];
                packetLengthVector[_tagMap[record.OperationTag]] = record.IecPacketLength;
                record.IecPacketLengthVector = packetLengthVector;
                record.OperationTagVector = _tags;
                return record;
            }
        }

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
            output.FlowId = input.ExportCounter;
            output.IecFrameFormat = input.IecFrameFormat;
            output.IecPacketLength = Int32.TryParse(input.IecPacketLength, out var iecPacketLength)? iecPacketLength : 0;
            output.Packets = input.Packets;
            output.SourceAddress = input.SourceAddress;
            output.SourcePort = Int32.TryParse(input.SourcePort, out var sourcePort) ? sourcePort : 0;
            output.OperationTag = $"Tag_{input.IecFrameFormat}_{input.CauseOfTransmission}";
            output.StartDateTime = input.StartDateTime;
            return output;
        }

        public override IObservable<PacketRecord<Packet>> LoadFromDevice(ICaptureDevice captureDevice, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override IObservable<PacketRecord<Packet>> LoadFromFile(string inputCaptureFile, string inputLabelFile, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
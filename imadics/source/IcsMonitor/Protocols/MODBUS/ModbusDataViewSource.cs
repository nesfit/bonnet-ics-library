using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using IcsMonitor.Flows;
using Microsoft.ML;
using PacketDotNet;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Traffix.Core.Flows;
using Traffix.Core.Observable;
using YamlDotNet.Core.Tokens;

namespace IcsMonitor.Modbus
{
    /// <summary>
    /// An implementation of data view source for MODBUS protocol.
    /// </summary>
    public class ModbusDataViewSource : FlowsDataViewSource<PacketRecord<Packet>, ModbusCompact>
    {
        const int ModbusPort = 502;

        public ModbusDataViewSource(IDictionary<string, string> configuration) : base(configuration)
        {

        }

        /// <inheritdoc/>
        public override IReadOnlyCollection<string> FeatureColumns => new[] {
                "ForwardMetricsDuration", "ForwardMetricsPackets", "ForwardMetricsOctets", "ReverseMetricsDuration", "ReverseMetricsPackets",
                "ReverseMetricsOctets", "DataReadRequests", "DataWriteRequests", "DataDiagnosticRequests", "DataOtherRequests",
                "DataUndefinedRequests", "DataMalformedRequests", "DataResponsesSuccess", "DataResponsesError", "DataMalformedResponses" };


        public override IObservable<List<FlowRecord<TKey,ModbusCompact>>> LoadDataFrom<TKey>(IObservable<PacketRecord<Packet>> source, TimeSpan windowSpan, Func<FlowKey, TKey> getKey)
        {
            var observable = source.Where(p => p.Key.SourcePort == ModbusPort || p.Key.DestinationPort == ModbusPort);
            var windows = observable.TimeSpanWindowGroup(t => t.Ticks, windowSpan);

            // apply modbus flow processor
            return windows.Select((packets, index) => (packets, index)).Select(async window =>
            {
                var totalPackets = 0;
                var flowProcessor = new ModbusFlowProcessor<TKey>(window.index.ToString("D4"), window.packets.Key.Start, window.packets.Key.Duration, getKey);
                await window.packets.Do(_ => totalPackets++).ForEachAsync(p => flowProcessor.OnNext(p));
                var aggregatedFlows = flowProcessor.GetConversations(getKey);
                return aggregatedFlows.Select(x => x.Value).ToList();
            }).Merge();
        }

        public override Task<IDataView> GetDataViewAsync<TKey>(MLContext ml, IObservable<FlowRecord<TKey,ModbusCompact>> observable)
        {
            var enumerable =  observable.ToEnumerable();
            var mapper = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<FlowRecord<TKey,ModbusCompact>, ModbusDataViewRecord>();
                cfg.CreateMap<TimeSpan, double>().ConvertUsing(ts => ts.TotalSeconds);
                cfg.CreateMap<TimeSpan, float>().ConvertUsing(ts => (float)ts.TotalSeconds);
            }).CreateMapper();

            var records = enumerable.Select(x => mapper.Map<FlowRecord<TKey, ModbusCompact>, ModbusDataViewRecord>(x)).ToList();
            var dataview = ml.Data.LoadFromEnumerable(records);
            return Task.FromResult(dataview);
        }

        public override IObservable<PacketRecord<Packet>> LoadFromFile(string inputCaptureFile, string inputLabelFile, CancellationToken cancellationToken) => LoadPacketsFromFile(inputCaptureFile, inputLabelFile, cancellationToken);

        public override IObservable<PacketRecord<Packet>> LoadFromDevice(ICaptureDevice captureDevice, CancellationToken cancellationToken) => LoadPacketsFromDevice(captureDevice, cancellationToken);

        public override IDataView LoadFromCsvFile(MLContext mlContext, string file)
        {
            //var dv =  mlContext.Data.LoadFromTextFile<ModbusDataViewRecord>(file, separatorChar: ',', hasHeader: true, allowQuoting: true, trimWhitespace: true);
            using var reader = new StreamReader(file);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { });
            var records = csv.GetRecords<dynamic>().ToArray();

            var mapper = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<string, string>().ConvertUsing(s => ConvertFromJson<string>(s));
                cfg.CreateMap<string, int>().ConvertUsing(s => ConvertFromJson<int>(s));
                cfg.CreateMap<string, long>().ConvertUsing(s => ConvertFromJson<long>(s));
                cfg.CreateMap<string, float>().ConvertUsing(s => ConvertFromJson<float>(s));
                cfg.CreateMap<string, float[]>().ConvertUsing(s => ConvertFromJson<float[]>(s));
                cfg.CreateMap<string, DateTime>().ConvertUsing(s => ConvertFromJson<DateTime>(s));
                cfg.CreateMap<string, TimeSpan>().ConvertUsing(s => ConvertFromJson<TimeSpan>(s));
                cfg.CreateMap<string, double>().ConvertUsing(s=> ConvertFromJson<double>(s));
            }).CreateMapper();
            var x = records.Select<dynamic, ModbusDataViewRecord>(r => mapper.Map<dynamic, ModbusDataViewRecord>(r));
            var dv = mlContext.Data.LoadFromEnumerable<ModbusDataViewRecord>(x);
            var prev = dv.Preview();
            return dv;
        }
        T ConvertFromJson<T>(String s)
        {
            var value = JsonSerializer.Deserialize<T>(s);
            return value;
        }
    }
}

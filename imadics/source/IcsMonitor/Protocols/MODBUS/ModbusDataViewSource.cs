using AutoMapper;
using IcsMonitor.Flows;
using Microsoft.ML;
using PacketDotNet;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Traffix.Core.Flows;
using Traffix.Core.Observable;

namespace IcsMonitor.Modbus
{
    /// <summary>
    /// An implementation of data view source for MODBUS protocol.
    /// </summary>
    public class ModbusDataViewSource : FlowsDataViewSource<PacketRecord<Packet>, ModbusCompact>
    {
        const int ModbusPort = 502;

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
            }).CreateMapper();

            var records = enumerable.Select(x => mapper.Map<FlowRecord<TKey, ModbusCompact>, ModbusDataViewRecord>(x)).ToList();
            var dataview = ml.Data.LoadFromEnumerable(records);
            return Task.FromResult(dataview);
        }

        public override IObservable<PacketRecord<Packet>> LoadFromFile(string inputCaptureFile, string inputLabelFile, CancellationToken cancellationToken) => LoadPacketsFromFile(inputCaptureFile, inputLabelFile, cancellationToken);

        public override IObservable<PacketRecord<Packet>> LoadFromDevice(ICaptureDevice captureDevice, CancellationToken cancellationToken) => LoadPacketsFromDevice(captureDevice, cancellationToken);

        public override IDataView LoadFromCsvFile(MLContext mlContext, string file)
        {
            return mlContext.Data.LoadFromTextFile<ModbusDataViewRecord>(file, separatorChar: ',', hasHeader: true, allowQuoting: true);
        }
    }
}

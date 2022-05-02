using IcsMonitor.Flows;
using Microsoft.ML;
using PacketDotNet;
using SharpPcap;
using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Traffix.Core.Flows;

namespace IcsMonitor.Protocols
{
    [Serializable]
    internal class IecDataViewSource : FlowsDataViewSource<PacketRecord<Packet>, IecCompact>
    {
        public IecDataViewSource()
        {
        }

        public override string[] FeatureColumns => throw new NotImplementedException();

        public override Task<IDataView> GetDataViewAsync<TKey>(MLContext ml, IObservable<FlowRecord<TKey, IecCompact>> observable)
        {
            throw new NotImplementedException();
        }

        public override IObservable<System.Collections.Generic.List<FlowRecord<TKey, IecCompact>>> LoadDataFrom<TKey>(IObservable<PacketRecord<Packet>> source, TimeSpan windowSpan, Func<FlowKey, TKey> getKey)
        {
            throw new NotImplementedException();
        }

        public override IDataView LoadFromCsvFile(MLContext mlContext, string file)
        {
            throw new NotImplementedException();
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
using IcsMonitor.AnomalyDetection;
using IcsMonitor.Modbus;
using IcsMonitor.Protocols;
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
using Traffix.Providers.PcapFile;

namespace IcsMonitor.Flows
{
    /// <summary>
    /// An abstract class that also provides a method to create specific flow sources for different supported protocols.
    /// </summary>
    public abstract class FlowsDataViewSource
    {
        /// <summary>
        /// The configuration collection.
        /// </summary>
        protected Dictionary<string, string> _configuration;

        /// <summary>
        /// Protected constructor for the data view source.
        /// </summary>
        /// <param name="configuration">The configuration of the data source.</param>
        protected FlowsDataViewSource(IDictionary<string, string> configuration)
        {

            this._configuration = configuration != null ? new Dictionary<string, string>(configuration) : new Dictionary<string,string>();
        }

        /// <summary>
        /// Factory method that gets the particular flow source for the given <paramref name="protocolType"/>.
        /// </summary>
        /// <param name="protocolType">The type of the protocol.</param>
        /// <returns>A flow source object for the specific <paramref name="protocolType"/>.</returns>
        /// <exception cref="NotImplementedException"></exception>
        public static FlowsDataViewSource GetSource(IndustrialProtocol protocolType, IDictionary<string, string> configuration = null)
        {
            return protocolType switch
            {
                IndustrialProtocol.Modbus => new ModbusDataViewSource(configuration),
                IndustrialProtocol.Iec => new IecDataViewSource(configuration),
                IndustrialProtocol.Goose => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),
            };
        }
        /// <summary>
        /// Collection of column names that are used to compute Features vector.
        /// </summary>
        public abstract IReadOnlyCollection<string> FeatureColumns { get; }

        /// <summary>
        /// Gets the configuration as the key to value mapping.
        /// </summary>
        public Dictionary<string, string> Configuration => _configuration;

        /// <summary>
        /// Loads packets and optionally labels from the input packet capture file and label file, respectively.
        /// <param name="inputCaptureFile">the name of packet capture file.</param>
        /// <param name="inputLabelFile">the name of label file. If null then labels are not read.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An observable of packets loaded from the input file.</returns>
        public static IObservable<PacketRecord<Packet>> LoadPacketsFromFile(string inputCaptureFile, string inputLabelFile, CancellationToken cancellationToken)
        {
            if (inputLabelFile == null)
            {
                var observable = SharpPcapReader.CreateObservable(inputCaptureFile).Select(p => PacketRecord<Packet>.FromFrame(p, null));
                return observable;
            }
            else
            {
                var labels = PacketAnnotationSourceFile.ReadLabels(inputLabelFile);
                var observable = SharpPcapReader.CreateObservable(inputCaptureFile).Zip(labels).Select(p => PacketRecord<Packet>.FromFrame(p.First, p.Second.PacketLabel.ToString()));
                return observable;
            }
        }

        /// <summary>
        /// Loads packets and optionally labels from the input packet capture device.
        /// <param name="inputCaptureFile">the name of packet capture file.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An observable of packets loaded from the input file.</returns>
        public static IObservable<PacketRecord<Packet>> LoadPacketsFromDevice(ICaptureDevice captureDevice, CancellationToken cancellationToken)
        {
            return Observable.Create<PacketRecord<Packet>>(observer => PacketDeviceSource.Subscribe(captureDevice, observer, cancellationToken));
        }

        /// <summary>
        /// Loads and Aggregates ICS traffic from the given source. 
        /// </summary>
        /// <typeparam name="TKey">The type of the flow key.</typeparam>
        /// <param name="mlContext">The ML context object required for some data view related operations.</param>
        /// <param name="inputCaptureFile">The input capture file name.</param>
        /// <param name="inputLabelFile">the input label file name.</param>
        /// <param name="windowTimeSpan">The size of the time-aggregation window.</param>
        /// <param name="getKey">The function used to get key from the flow record.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task that when the method completes provide loaded data view.</returns>
        public abstract Task<IDataView> LoadAndAggregateAsync<TKey>(MLContext mlContext, string inputCaptureFile, string inputLabelFile, TimeSpan windowTimeSpan, Func<FlowKey, TKey> getKey, CancellationToken cancellationToken);

        /// <summary>
        /// Reads and Aggregates ICS traffic from the given source.
        /// </summary>
        /// <typeparam name="TKey">The type of the flow key.</typeparam>
        /// <param name="mlContext">The ML context object required for some data view related operations.</param>
        /// <param name="captureDevice">The input capture device use to read the traffic from.</param>
        /// <param name="windowTimeSpan">The size of the time-aggregation window.</param>
        /// <param name="getKey">The function used to get key from the flow record.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The observable object providing data views of read and aggregated flow records.</returns>
        public abstract IObservable<IDataView> ReadAndAggregateAsync<TKey>(MLContext mlContext, ICaptureDevice captureDevice, TimeSpan windowTimeSpan, Func<FlowKey, TKey> getKey, CancellationToken cancellationToken);

        /// <summary>
        /// Reads and Aggregates ICS traffic from the given source.
        /// </summary>
        /// <typeparam name="TKey">The type of the flow key.</typeparam>
        /// <param name="mlContext">The ML context object required for some data view related operations.</param>
        /// <param name="captureDevice">The input capture device use to read the traffic from.</param>
        /// <param name="windowTimeSpan">The size of the time-aggregation window.</param>
        /// <param name="windowCount">The total count of windows to process.</param>
        /// <param name="getKey">The function used to get key from the flow record.</param>
        /// <param name="onNext">On next callback for observe new objects. This can be used, e.g., for progress reporting and logging.</param>
        /// <param name="cancellationToken">The canclellation token.</param>
        /// <returns></returns>
        public abstract Task<IDataView> ReadAllAndAggregateAsync<TKey>(MLContext mlContext, ICaptureDevice captureDevice, TimeSpan windowTimeSpan, int windowCount, Func<FlowKey, TKey> getKey, Action<IEnumerable<object>> onNext, CancellationToken cancellationToken);

        /// <summary>
        /// Loads the data view from CSV source file.
        /// </summary>
        /// <param name="mlContext">The ML context object.</param>
        /// <param name="file">The CSV file name.</param>
        /// <returns></returns>
        public abstract IDataView LoadFromCsvFile(MLContext mlContext, string file); 
    }

    /// <summary>
    /// Supports observable for the capture device.
    /// </summary>
    class PacketDeviceSource : IDisposable
    {
        private readonly ICaptureDevice _captureDevice;
        private readonly IObserver<PacketRecord<Packet>> _observer;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// Creates a new packet device source.
        /// </summary>
        /// <param name="captureDevice">The underlying capture device.</param>
        /// <param name="observer">The observer to consume the packets.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        internal PacketDeviceSource(ICaptureDevice captureDevice, IObserver<PacketRecord<Packet>> observer, CancellationToken cancellationToken)
        {
            _captureDevice = captureDevice;
            _observer = observer;
            _captureDevice.OnPacketArrival += _captureDevice_OnPacketArrival;
            _captureDevice.OnCaptureStopped += _captureDevice_OnCaptureStopped;
            _captureDevice.StartCapture();
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Callback.
        /// </summary>
        private void _captureDevice_OnCaptureStopped(object sender, CaptureStoppedEventStatus status)
        {
            _observer.OnCompleted();
        }
        
        /// <summary>
        /// Callback.
        /// </summary>
        private void _captureDevice_OnPacketArrival(object sender, CaptureEventArgs e)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _captureDevice.StopCapture();
                _observer.OnCompleted();
            }
            else 
            {
                PacketCount++;
                _observer.OnNext(PacketRecord<Packet>.FromFrame(e.Packet, null));
            }
        }

        /// <summary>
        /// Gets the number of packets provided so far.
        /// </summary>
        public int PacketCount { get; private set; }
        /// <inheritdoc/>
        public void Dispose()
        {
            if (_captureDevice.Started) Close();
        }
        /// <summary>
        /// Subsrcibes the <paramref name="observer"/> to the newly created packet source provider based on <paramref name="captureDevice"/>.
        /// </summary>
        /// <param name="captureDevice">The capture device.</param>
        /// <param name="observer">the observer object.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Disposable that can be used to unsubscribe from the observable.</returns>
        public static IDisposable Subscribe(ICaptureDevice captureDevice, IObserver<PacketRecord<Packet>> observer, CancellationToken cancellationToken)
        {
            var tf = new PacketDeviceSource(captureDevice, observer, cancellationToken);
            return tf;
        }
        /// <summary>
        /// Closes the packet source provider.
        /// </summary>
        public void Close()
        {
            if (_captureDevice.Started) 
                _captureDevice.StopCapture();
        }
    }

    /// <summary>
    /// A typed version of <see cref="FlowsDataViewSource"/> that define other abstract methods.
    /// </summary>
    /// <typeparam name="TRecord">The type of records to be provided by this data source.</typeparam>
    /// <typeparam name="TInput">The type of input object that this data source can process.</typeparam>
    public abstract class FlowsDataViewSource<TInput, TRecord> : FlowsDataViewSource
    {
        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        /// <param name="configuration"></param>
        protected FlowsDataViewSource(IDictionary<string, string> configuration) : base(configuration)
        {
        }

        /// <inheritdoc/>
        public override Task<IDataView> LoadAndAggregateAsync<TKey>(MLContext mlContext, string inputCaptureFile, string inputLabelFile, TimeSpan windowTimeSpan, Func<FlowKey, TKey> getKey, CancellationToken cancellationToken)
        {
            var observable = LoadDataFrom(LoadFromFile(inputCaptureFile, inputLabelFile, cancellationToken), windowTimeSpan, getKey);
            return GetDataViewAsync(mlContext, observable.SelectMany(x=>x));
        }

        /// <inheritdoc/>
        public override IObservable<IDataView> ReadAndAggregateAsync<TKey>(MLContext mlContext, ICaptureDevice captureDevice, TimeSpan windowTimeSpan, Func<FlowKey, TKey> getKey, CancellationToken cancellationToken)
        {
            var sourceObservable = LoadFromDevice(captureDevice, cancellationToken);
            var observable = LoadDataFrom(sourceObservable, windowTimeSpan, getKey);
            return observable.SelectMany(flows => GetDataViewAsync(mlContext, flows.ToObservable()));
        }

        /// <inheritdoc/>
        public override Task<IDataView> ReadAllAndAggregateAsync<TKey>(MLContext mlContext, ICaptureDevice captureDevice, TimeSpan windowTimeSpan, int windowCount, Func<FlowKey, TKey> getKey, Action<IEnumerable<object>> onNext, CancellationToken cancellationToken)
        {
            var sourceObservable = LoadFromDevice(captureDevice, cancellationToken);
            var observable = LoadDataFrom(sourceObservable, windowTimeSpan, getKey).Do(x=>onNext?.Invoke(x.Cast<object>())).Take(windowCount);
            return GetDataViewAsync(mlContext, observable.SelectMany(x => x));
        }

        /// <summary>
        /// Loads the input data from the capture devices and provides it in form of observable collection.
        /// </summary>
        /// <param name="captureDevice">The input capture device.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The observable collection of <typeparamref name="TInput"/> records.</returns>
        public abstract IObservable<TInput> LoadFromDevice(ICaptureDevice captureDevice, CancellationToken cancellationToken);
        /// <summary>
        ///  Loads the input data from the input capture fileand provides it in form of observable collection.
        /// </summary>
        /// <param name="inputCaptureFile">The input capture file.</param>
        /// <param name="inputLabelFile">The input label file.</param>
        /// <param name="cancellationToken">The cancellaiton token.</param>
        /// <returns></returns>
        public abstract IObservable<TInput> LoadFromFile(string inputCaptureFile, string inputLabelFile, CancellationToken cancellationToken);
        /// <summary>
        /// Loads data from the given source file and provides them in batches as observable sequence.
        /// </summary>
        /// <param name="inputCaptureFile">An input capture file.</param>
        /// <param name="windowSpan">Size of window for collecting packets in the batches.</param>
        /// <param name="getKey">The aggregation key used to compose the flow records.</param>
        /// <returns>Observable collection of batches of records. Each batch represents a single window.</returns>
        public abstract IObservable<List<FlowRecord<TKey,TRecord>>> LoadDataFrom<TKey>(IObservable<TInput> source, TimeSpan windowSpan, Func<FlowKey, TKey> getKey);

        /// <summary>
        /// Gets the dataview from the collection of records.
        /// <para/>
        /// This method implements the operation necessary to convert each record to the dataview row. As the record is 
        /// a complex strcuture it is necessary to convert it to simple flat structure for which the dataview can be generated.
        /// </summary>
        /// <param name="enumerable">An input enumerable of records to produce the data view. </param>
        /// <returns>A data view that represents the input observable.</returns>
        public abstract Task<IDataView> GetDataViewAsync<TKey>(MLContext ml, IObservable<FlowRecord<TKey,TRecord>> observable);
    }
    public static class FlowsDataViewSourceOperations
    {
        /// <summary>
        /// A special version of select many. 
        /// <para/>
        /// It makes a flat observation from the windowed observation of records.
        /// </summary>
        /// <typeparam name="TRecord">The type of records.</typeparam>
        /// <param name="observable">An inout observable.</param>
        /// <returns>Output observable that contains records  from the input observable of grouped records. </returns>
        public static IObservable<TOut> SelectMany<TIn,TOut>(this IObservable<List<TIn>> observable,Func<TIn,TOut> map)
        {
            return Observable.Create<TOut>(async o =>
                await observable.ForEachAsync(a => a.ForEach(x => o.OnNext(map(x))))
            );
        }
    }
}
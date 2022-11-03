using IcsMonitor.AnomalyDetection;
using IcsMonitor.Flows;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Traffix.DataView;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace IcsMonitor
{
    /// <summary>
    /// Provides a context of the traffic monitor that exposes public API.
    /// </summary>
    public class TrafficMonitorContext
    {
        private readonly ILogger _logger;
        private readonly MLContext _mlContext;

        /// <summary>
        /// Creates a new instance of the context.
        /// </summary>
        /// <param name="mLContext">A machine learning context instance.</param>
        /// <param name="logger">A logger instance.</param>
        public TrafficMonitorContext(MLContext mLContext, ILogger<TrafficMonitorContext> logger)
        {
            _logger = logger;
            _mlContext = mLContext;
        }

        /// <summary>
        /// Prints information about the learned profile.
        /// </summary>
        /// <param name="profileFile">The profile file object.</param>
        /// <param name="textWriter">The text writer use to print information.</param>
        public async Task PrintProfileAsync(TrafficProfile profile, TextWriter textWriter)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var yaml = serializer.Serialize(profile);
            await textWriter.WriteLineAsync(yaml);
        }

        /// <summary>
        /// Loads the profile form the file.
        /// </summary>
        /// <param name="profileFile">The profile file.</param>
        /// <returns>Traffic profile instance loaded form the specified file.</returns>
        public TrafficProfile LoadProfileFromFile(string profileFile)
        {
            return TrafficProfile.LoadFromFile(_mlContext, profileFile);
        }

        /// <summary>
        /// Monitors traffic on the specified capture device and checks flows against the learned profile.
        /// </summary>
        /// <param name="device">The capture device.</param>
        /// <param name="profile">The profile.</param>
        /// <param name="outputFormat">The required output format.</param>
        /// <param name="textWriter">The output writer.</param>
        public async Task WatchTrafficAsync(SharpPcap.ICaptureDevice device, TrafficProfile profile, OutputFormat outputFormat, TextWriter textWriter, CancellationToken cancellationToken)
        {
            var dataViewSource = profile.GetSource();

            _logger.LogInformation($"Open device {device.Name}.");
            device.Open();

            var observable = dataViewSource.ReadAndAggregateAsync(_mlContext, device, profile.WindowTimeSpan, AggregatorKey.Multiflow, cancellationToken);

            _logger.LogInformation($"Start collecting and processing windows.");

            IDataViewWriter outputFileWriter = null;

            var windows = 0;
            var totalFlows = 0;
            await observable.ForEachAsync(dataview =>
            {
                var scoring = profile.Transform(dataview);
                if (outputFileWriter == null)
                {
                    outputFileWriter = DataViewWriterFactory.CreateWriter(outputFormat, textWriter, scoring.Schema);
                    outputFileWriter.BeginDocument();
                }
                var flowCount = outputFileWriter.AppendDataView(scoring);
                totalFlows += flowCount;
                _logger.LogInformation($"Progress: {flowCount} flows identified and tested in the current window, {++windows} windows completed.");
            });
            _logger.LogInformation($"Input observable completed: {windows} windows collected, {totalFlows} flows tested.");
            outputFileWriter.EndDocument();
            device.Close();
        }

        /// <summary>
        /// Tests source flows using the given profile.
        /// </summary>
        /// <param name="flowsFile">The flow file. It must be a CSV file with the structure as specified by the profile protocol.</param>
        /// <param name="profile">The profile.</param>
        /// <param name="outputFormat">Required output format./param>
        /// <param name="textWriter">The output writer.</param>
        public Task TestFlowsAsync(string flowsFile, TrafficProfile profile, OutputFormat outputFormat, TextWriter textWriter, double thresholdValue, TextWriter detectedWriter)
        {
            return Task.Run(TestFlowsAction);
            void TestFlowsAction()
            {
                var dataViewSource = FlowsDataViewSource.GetSource(profile.ProtocolType, profile.Configuration);
                var dataview = dataViewSource.LoadFromCsvFile(_mlContext, flowsFile);
                var scoring = profile.Transform(dataview);

                var outputFileWriter = DataViewWriterFactory.CreateWriter(outputFormat, textWriter, scoring.Schema);
                outputFileWriter.BeginDocument();
                var flowCount = outputFileWriter.AppendDataView(scoring);
                outputFileWriter.EndDocument();

                var filteredData = _mlContext.Data.FilterRowsByColumn(scoring, columnName: "AverageScore", lowerBound: 0, upperBound: thresholdValue);

                var detectedFileWriter = DataViewWriterFactory.CreateWriter(outputFormat, detectedWriter, scoring.Schema);
                detectedFileWriter.BeginDocument();
                var flowCountDetected = detectedFileWriter.AppendDataView(filteredData);
                detectedFileWriter.EndDocument();


                _logger.LogInformation($"Test flow completed: {flowCount} flows tested, {flowCountDetected} anomaly flows detected (threshold={thresholdValue}).");
            }
        }

        /// <summary>
        /// Exports flows collected on the input device.
        /// </summary>
        /// <param name="captureDevice">The input capture device.</param>
        /// <param name="protocolType">The ICS Protocol of the selected flows.</param>
        /// <param name="windowTimeSpan">The size of the flow collection window. </param>
        /// <param name="textWriter">The output text writer.</param>
        /// <param name="outputFormat">The output format.</param>
        /// <param name="timeOut">The total time of processing.</param>
        /// <param name="cancellationToken">Cancel token to stop processing.</param>
        public async Task ExportFlowsAsync(
            ICaptureDevice captureDevice,
            IndustrialProtocol protocolType,
            TimeSpan windowTimeSpan,
            TextWriter textWriter,
            OutputFormat outputFormat,
            TimeSpan timeOut,
            CancellationToken cancellationToken)
        {
            captureDevice.Open();
            long windowCount = Math.Min((long)Math.Ceiling(timeOut / windowTimeSpan), Int32.MaxValue);
            var configuration = new Dictionary<string, string>()
                    {
                        { "window", windowTimeSpan.ToString()}
                    };
            var dataViewSource = FlowsDataViewSource.GetSource(protocolType, configuration);
            var dataview = await dataViewSource.ReadAllAndAggregateAsync(_mlContext, captureDevice, windowTimeSpan, (int)windowCount, AggregatorKey.Multiflow, null, cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;

            using var outputFileWriter = DataViewWriterFactory.CreateWriter(outputFormat, textWriter, dataview.Schema);

            outputFileWriter.BeginDocument();
            outputFileWriter.AppendDataView(dataview);
            outputFileWriter.EndDocument();
            captureDevice.Close();
        }

        /// <summary>
        /// Enumeration for possible feature transformaiton/dimension reduction.
        /// </summary>
        public enum FeatureTransformOperation { Pca, Direct, Average }
        /// <summary>
        /// Creates the profile from the source data and provided options.
        /// </summary>
        private TrafficProfile CreateProfile(string profileName, IndustrialProtocol protocolType, TimeSpan windowTimeSpan, MLContext mlContext, string[] featureColumns, IDataView sourceData, FeatureTransformOperation featureTransformOperation, Dictionary<string, string> configuration)
        {
            _logger.LogInformation($"Training profile on input data.");
            var trainer = new TrafficProfileTrainer(mlContext, PcaRank, protocolType, featureColumns, windowTimeSpan);

            var featureTransformer = (featureTransformOperation) switch
            {
                FeatureTransformOperation.Pca => trainer.PcaTransformer(3),
                FeatureTransformOperation.Direct => trainer.DirectTransformer,
                FeatureTransformOperation.Average => trainer.AverageTransformer,
                _ => throw new NotImplementedException()
            };


            var profile = trainer.Fit(Path.GetFileNameWithoutExtension(profileName), configuration, featureTransformer, Clusters, ModelCount, sourceData);
            return profile;
        }

        /// <summary>
        /// Deinfes the target PCS rank, if PCA feature spae transformation is in use.
        /// </summary>
        public int PcaRank { get; set; } = 3;

        /// <summary>
        /// A number of models that comprise the profile.
        /// </summary>
        public int ModelCount { get; set; } = 3;

        /// <summary>
        /// An array of values to be used for constructing model candidates.
        /// </summary>
        public int[] Clusters { get; set; } = new[] { 3, 4, 5, 6, 7, 8 };


        /// <summary>
        /// Captures the traffic on the given network device and creates a profile based on the observed industrial flows.  
        /// </summary>
        /// <param name="device">An input device containtin source packets.</param>
        /// <param name="protocolType">The typw of the protocol.</param>
        /// <param name="windowTimeSpan">A size of window.</param>
        /// <param name="windowCount">A number of windows to collect.</param>
        /// <param name="customFeatureColumns">If null the features as defined by the corresponding protocol type provider will be used. Using this option it is possible to use a custom vector of features.</param>
        /// <param name="outputProfileFile">A name of file for saving the profile.</param>
        /// <param name="cancellationToken">The cancellation token used to stop processing the input. If the token is activated the profile is computed for the input collected so far.</param>
        /// <returns>The task that signalize the completion of the profile computation.</returns>
        public async Task BuildProfileAsync(ICaptureDevice device, IndustrialProtocol protocolType, TimeSpan windowTimeSpan, int windowCount, FeatureTransformOperation fto, string[] customFeatureColumns, string outputProfileFile, CancellationToken cancellationToken)
        {
            try
            {
                var windowFound = 0;
                void OnNextWindow(IEnumerable<object> obj)
                {
                    windowFound++;
                    _logger.LogInformation($"Progress: {windowFound} of {windowCount} ({windowFound * 100 / windowCount}%)");
                }

                _logger.LogInformation($"Creating profile for {device.Name}.");
                var configuration = new Dictionary<string, string>()
                    {
                        { "window", windowTimeSpan.ToString()}
                    };
                var dataViewSource = FlowsDataViewSource.GetSource(protocolType, configuration);

                device.Open();
                var dataview = await dataViewSource.ReadAllAndAggregateAsync(_mlContext, device, windowTimeSpan, windowCount, AggregatorKey.Multiflow, OnNextWindow, cancellationToken);
                device.Close();

                var featureColumns = customFeatureColumns ?? dataViewSource.FeatureColumns;

                _logger.LogInformation($"Computing profile.");
                var profile = CreateProfile($"{protocolType}", protocolType, windowTimeSpan, _mlContext, featureColumns.ToArray(), dataview, fto, null);
                _logger.LogInformation($"Saving profile to {outputProfileFile}.");
                profile.SaveToFile(outputProfileFile);
                _logger.LogInformation($"Done.");
            }
            catch (Exception e)
            {
                _logger.LogError($"{e.Message}");
            }
        }
        /// <summary>
        /// Builds traffic profile from the provided CSV file that contains normal flows.
        /// </summary>
        /// <param name="flowFilePath">Path to CSV flow file.</param>
        /// <param name="protocolType">The type of industrial protocol.</param>
        /// <param name="windowTimeSpan">Window span used to aggregate flow information.</param>
        /// <param name="customFeatureColumns">Custom features. Ca be null to use the default features.</param>
        /// <param name="outputProfileFile">The profile file name.</param>
        /// <returns>The task that completes after a profile is built and written to an output file.</returns>
        public Task BuildProfileAsync(string flowFilePath, IndustrialProtocol protocolType, TimeSpan windowTimeSpan, FeatureTransformOperation fto, string[] customFeatureColumns, string outputProfileFile)
        {
            return Task.Run(BuildProfileAction);

            void BuildProfileAction()
            {
                try
                {
                    _logger.LogInformation($"Loading flows from {flowFilePath}.");
                    var configuration = new Dictionary<string, string>()
                    {
                        { "window", windowTimeSpan.ToString()}
                    };
                    var dataViewSource = FlowsDataViewSource.GetSource(protocolType, configuration);
                    var dataview = dataViewSource.LoadFromCsvFile(_mlContext, flowFilePath);

                    var featureColumns = customFeatureColumns ?? dataViewSource.FeatureColumns;

                    _logger.LogInformation($"Computing profile.");
                    var profile = CreateProfile($"{protocolType}", protocolType, windowTimeSpan, _mlContext, featureColumns.ToArray(), dataview, fto, dataViewSource.Configuration);
                    _logger.LogInformation($"Saving profile to {outputProfileFile}.");
                    profile.SaveToFile(outputProfileFile);
                    _logger.LogInformation($"Done.");
                }
                catch (Exception e)
                {
                    _logger.LogError($"{e.Message}");
                }
            }
        }
    }
}

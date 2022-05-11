using IcsMonitor.Flows;
using IcsMonitor.Utils;
using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace IcsMonitor.AnomalyDetection
{
    /// <summary>
    /// Represents traffic profile thar consists of a collection of models. 
    /// The profile is used for detection of unusuall traffic patterns. 
    /// </summary>
    public class TrafficProfile
    {
        const string indexYamlFile = "index.yaml";
        const string inputDataTransformerEntryName = "InputData.transform";

        /// <summary>
        /// Represents a configuration of the profile. 
        /// The structure corresponds to the index.yaml file in the stored profile. 
        /// </summary>
        internal struct Settings
        {
            public string ProfileName { get; set; }
            public IndustrialProtocol ProtocolType { get; set; }
            public TimeSpan WindowTimeSpan { get; set; }
            public int ModelCount { get; set; }
            public Dictionary<string, string> Configuration { get; set; }
        }


        private readonly MLContext _ml;
        private readonly ClusterModel[] _models;
        private readonly ITransformer _transformer;
        private readonly DataViewSchema _inputSchema;
        private readonly Settings _settings;

        public IDictionary<string, string> Configuration => _settings.Configuration;
            

        /// <summary>
        /// Creates a new profile from the given parameters.
        /// </summary>
        /// <param name="ml">The ML.NET context.</param>
        /// <param name="models">An array fo models.</param>
        /// <param name="inputSchema">The input schema.</param>
        /// <param name="inputTransformer">The input data transformer, which is applied to input data before the classifier.</param>
        /// <param name="settings">The settings.</param>
        internal TrafficProfile(MLContext ml, ClusterModel[] models, DataViewSchema inputSchema, ITransformer inputTransformer, Settings settings)
        {
            _ml = ml;
            _models = models;
            _transformer = inputTransformer;
            _inputSchema = inputSchema;
            _settings = settings;
            _settings.ModelCount = models.Length;
            
        }
        public FlowsDataViewSource GetSource()
        {
            return FlowsDataViewSource.GetSource(ProtocolType);
        }

        /// <summary>
        /// Performs analysis of the input data and generates an enumerable with predicted/classified output. 
        /// The given <paramref name="testData"/> are first preprocessed using <see cref="InputTransformer"/>
        /// and then all models are applied. <see cref="FlowScore"/> object is generated for each input record.
        /// </summary>
        /// <param name="testData">The input test data.</param>
        /// <returns>A collection of  <see cref="FlowScore"/> objects. </returns>
        public IEnumerable<FlowScore> Predict(IDataView testData)
        {
            var inputData = _transformer.Transform(testData);
            var transformed = _models.Select(model => model.Transform(_ml, inputData).ToArray()).ToArray();
            for (int i = 0; i < transformed.First().Length; i++)
            {
                var t = transformed[0][i];
                var scores = new double[_models.Length];
                var distances = new double[_models.Length];
                for (int j = 0; j < _models.Length; j++)
                {
                    var point = transformed[j][i];
                    distances[j] = point.Distance;
                    scores[j] = 1 - Math.Min(point.Distance / (6 * Math.Sqrt(point.Variance)), 1);
                }
                var flowScore = new FlowScore(t.FlowKey, t.WindowStart, t.WindowDuration, t.FlowLabel, t.Features, distances, scores);
                yield return flowScore;
            }
        }

        /// <summary>
        /// Transform the input data view and produces a set of <see cref="FlowScore"/> represented as <see cref="IDataView"/>. 
        /// </summary>
        /// <param name="testData"></param>
        /// <returns>A dataview consisting of the results of application of the profile to <paramref name="testData"/>.</returns>
        public IDataView Transform(IDataView testData)
        {
            var resultView = Predict(testData);
            return _ml.Data.LoadFromEnumerable(resultView);
        }

        /// <summary>
        /// Loads the profile from the given file.
        /// </summary>
        /// <param name="mlContext">The ML.NET context.</param>
        /// <param name="path">Path to the profile file.</param>
        /// <returns>Profile loaded from the specifed file.</returns>
        public static TrafficProfile LoadFromFile(MLContext mlContext, string path)
        {
            using var modelArchive = ZipFile.Open(path, ZipArchiveMode.Read);
            var indexEntry = modelArchive.GetEntry(indexYamlFile);
            var profileIndex = indexEntry.ReadYaml<Settings>();
            var transformerEntry = modelArchive.GetEntry(inputDataTransformerEntryName);
            using var transformerEntryStream = transformerEntry.Open();
            var inputTransformer = mlContext.Model.Load(transformerEntryStream, out var schema);

            var models = new List<ClusterModel>();
            // load models:
            for (int i = 0; i < profileIndex.ModelCount; i++)
            {
                var model = ClusterModel.Load(mlContext, modelArchive, $"{i:D2}/");
                models.Add(model);
            }
            return new TrafficProfile(mlContext, models.ToArray(), schema, inputTransformer, profileIndex);
        }

        /// <summary>
        /// The protocol name for which this profile was created.
        /// </summary>
        public IndustrialProtocol ProtocolType => _settings.ProtocolType;

        /// <summary>
        /// The size of time window used for creating the profile.
        /// </summary>
        public TimeSpan WindowTimeSpan => _settings.WindowTimeSpan;

        /// <summary>
        /// Gets the input data transformer. It takes input data and performs 
        /// several transformation to prepare them for evaluation by models.
        /// </summary>
        public ITransformer InputTransformer => _transformer;

        /// <summary>
        /// Gets models of the current profile.
        /// </summary>
        public ClusterModel[] Models => _models;

        /// <summary>
        /// Gets the schema required for the input dataview.
        /// </summary>
        public DataViewSchema InputSchema => _inputSchema;

        public string ProfileName => _settings.ProfileName;

        /// <summary>
        /// Stores the profile to the file.
        /// </summary>
        /// <param name="path">Path to file to store the profile.</param>
        public void SaveToFile(string path)
        {
            if (File.Exists(path)) File.Delete(path);
            using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
            var indexEntry = archive.CreateEntry("index.yaml");
            indexEntry.WriteYaml(_settings);

            for (int i = 0; i < _models.Length; i++)
            {
                _models[i].Save(_ml, archive, $"{i:D2}/");
            }
            var modelEntry = archive.CreateEntry(inputDataTransformerEntryName);
            using var stream = modelEntry.Open();
            _ml.Model.Save(this._transformer, _inputSchema, stream);
        }
    }
}

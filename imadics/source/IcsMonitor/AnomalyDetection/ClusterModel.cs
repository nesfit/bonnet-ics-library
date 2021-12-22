using IcsMonitor.Utils;
using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace IcsMonitor.AnomalyDetection
{
    /// <summary>
    /// Represents the K-means-based anomaly detection model.
    /// <para/>
    /// The model consists of a set of clusters each complemented with its variance.Each cluster thus 
    /// represents a sphere in the n-dimensional space.If a communication pattern characterized by 
    /// a point in the space belongs to some sphere, it is marked as normal. Otherwise, it is anomalous.
    /// </summary>
    public class ClusterModel : IAnomalyDetectionModel<ClusterModel.Output>
    {
        private readonly ITransformer _transformer;
        private readonly DataViewSchema _inputSchema;
        private readonly float[] _variances;
        private readonly string[] _featureColumnNames;
        private readonly float[][] _centroids;
        private readonly Dictionary<string, int> _featureMap;

        /// <summary>
        /// Creates the anomaly detection model. 
        /// </summary>
        /// <param name="transformer">The transformer chain of <see cref="ClusteringPredictionTransformer"/> type.</param>
        /// <param name="inputSchema">The schema of input data view.</param>
        /// <param name="featureNames">The name of source columns for feature vector definition.</param>
        /// <param name="variances">The variance vector. It is a value computed for each dimension.</param>
        /// <param name="centroids">An array of centroids of clustes.</param>
        /// <param name="featuresColumnName">A name of feature vector used for classification.</param>
        internal ClusterModel(ITransformer transformer, DataViewSchema inputSchema, string[] featureNames, float[][] centroids, float[] variances)
        {
            var schemaLookup = inputSchema.ToLookup(x => x.Name);
            // test input schema:
            if (!schemaLookup.Contains("Features")) throw new ArgumentException("inputSchema must contain 'Features' column");
            if (!schemaLookup.Contains("FlowKey")) throw new ArgumentException("inputSchema must contain 'FlowKey' column");

            _transformer = transformer ?? throw new ArgumentNullException(nameof(transformer));
            _inputSchema = inputSchema ?? throw new ArgumentNullException(nameof(inputSchema));
            _centroids = centroids ?? throw new ArgumentNullException(nameof(centroids));
            _variances = variances ?? throw new ArgumentNullException(nameof(variances));
            _featureColumnNames = featureNames ?? throw new ArgumentNullException(nameof(featureNames));
            _featureMap = featureNames.Select((name, index) => (name, index)).ToDictionary(x => x.name, x => x.index);
        }
        /// <summary>
        /// Represents the cluster prediction data type. This is the output type from the prediction. 
        /// <para/>
        /// See tutorial on K-Means clustering for more details:
        /// https://docs.microsoft.com/en-us/dotnet/machine-learning/tutorials/iris-clustering
        /// </summary>
        public class Output
        {
            /// <summary>
            /// Get the window label.
            /// </summary>
            public string WindowLabel;
            /// <summary>
            /// Gets the timestamp of the start of the window.
            /// </summary>
            public DateTime WindowStart;
            /// <summary>
            /// Gets the window duration.
            /// </summary>
            public TimeSpan WindowDuration;
            /// <summary>
            /// The label of the flow.
            /// </summary>
            public string FlowLabel;
            /// <summary>
            /// The key of the putput record.
            /// </summary>
            [ColumnName("FlowKey")]
            public string FlowKey;
            /// <summary>
            /// Contains the ID of the predicted cluster.
            /// <para/>
            /// The underlying algorithm requires that predicted cluster id column has name 'PredictedLabel'.
            /// </summary>
            [ColumnName("PredictedLabel")]
            public uint ClusterId { get; set; }
            /// <summary>
            /// Gets the distance to the predicted cluster centroid. 
            /// </summary>
            public float Distance => Distances[ClusterId - 1];
            /// <summary>
            /// Contains an array with squared Euclidean distances to the cluster centroids. The array length is equal to the number of clusters.
            /// <para/>
            /// The underlying algorithm requires that predicted cluster id column has name 'Score'. 
            /// </summary>
            [ColumnName("Score")]
            public float[] Distances { get; set; }
            /// <summary>
            /// The computed variance for the predicted cluster. 
            /// <para/>
            /// Compare this value to the distance for decision of whether to accept the point or not. 
            /// </summary>
            [ColumnName("Variance")]
            public float Variance { get; set; }
            /// <summary>
            /// Collection of features used as an input for the prediction algorithm.
            /// <para/>
            /// The underlying algorithm requires that this column has name 'Features'.
            /// </summary>
            [ColumnName("Features")]
            public float[] Features { get; set; }
        }

        /// <summary>
        /// Defines the options for creating the model.
        /// </summary>
        public class Options
        {
            /// <summary>
            /// The exact number of clusters.
            /// </summary>
            public int NumberOfClusters { get; set; }
        }
        /// <summary>
        /// Internal representation of the model configuration.
        /// </summary>
        internal class ModelConfiguration
        {
            /// <summary>
            /// The method name.
            /// </summary>
            public string Method { get; internal set; }
            /// <summary>
            /// The feature names vector.
            /// </summary>
            public string[] FeatureNames { get; internal set; }
            /// <summary>
            /// The variance vector.
            /// </summary>
            public float[] VarianceVector { get; internal set; }

            /// <summary>
            /// Get coordinates of centroids. 
            /// </summary>
            public float[][] Centroids { get; internal set; }

        }

        /// <inheritdoc/>
        public void SaveToFile(MLContext mlContext, string path)
        {
            if (File.Exists(path)) File.Delete(path);
            using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
            Save(mlContext, archive, String.Empty);
        }

        /// <summary>
        /// Saves the model to the given Zip archive.
        /// </summary>
        /// <param name="mlContext">The ML.NET context.</param>
        /// <param name="archive">The Zip archive to save the model to.</param>
        /// <param name="prefix">The prefix used for naming the entries in the Zip archives.</param>
        public void Save(MLContext mlContext, ZipArchive archive, string prefix)
        {
            var modelEntry = archive.CreateEntry($"{prefix}AnomalyDetection.model");
            using (var stream = modelEntry.Open())
            {
                mlContext.Model.Save(this._transformer, this._inputSchema, stream);
            }

            var entry = archive.CreateEntry($"{prefix}ModelConfig.yaml");
            var configuration = new ModelConfiguration
            {
                Method = nameof(ClusterModel),
                FeatureNames = _featureColumnNames,
                VarianceVector = _variances,
                Centroids = _centroids
            };
            using var writer = new StreamWriter(entry.Open());
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var yaml = serializer.Serialize(configuration);
            writer.WriteLine(yaml);
        }

        /// <summary>
        /// Loads the model from the given file.
        /// </summary>
        /// <param name="mlContext">The ML context.</param>
        /// <param name="path">Path to the model file.</param>
        /// <returns>The new anomaly detection model.</returns>
        public static ClusterModel LoadFromFile(MLContext mlContext, string path)
        {
            using var modelArchive = ZipFile.Open(path, ZipArchiveMode.Read);
            return Load(mlContext, modelArchive, String.Empty);
        }

        /// <summary>
        /// Loads the model from the given archive. 
        /// </summary>
        /// <param name="mlContext">The ML.NET context.</param>
        /// <param name="modelArchive">Zip archive to read data from.</param>
        /// <param name="prefix">The prefix of entries in the ZIP archive.</param>
        /// <returns>The loaded model.</returns>
        public static ClusterModel Load(MLContext mlContext, ZipArchive modelArchive, string prefix)
        {
            var modelEntry = modelArchive.GetEntry($"{prefix}AnomalyDetection.model");
            using var stream = modelEntry.Open();

            var transformer = mlContext.Model.Load(stream, out var schema);

            var entry = modelArchive.GetEntry($"{prefix}ModelConfig.yaml");
            var configuration = entry.ReadYaml<ModelConfiguration>();
            return new ClusterModel(transformer, schema, configuration.FeatureNames, configuration.Centroids, configuration.VarianceVector);
        }

        /// <inheritdoc/>
        public IEnumerable<Output> Transform(MLContext mlContext, IDataView source)
        {
            var target = _transformer.Transform(source);
            var output = mlContext.Data.CreateEnumerable<Output>(target, reuseRowObject: false, ignoreMissingColumns: true);
            var predictions = output.Select(Decide);
            return predictions;

            Output Decide(Output arg)
            {
                var variance = _variances[arg.ClusterId - 1];
                arg.Variance = variance;
                return arg;
            }
        }

        /// <summary>
        /// Gets  coordinates of centroids.
        /// </summary>
        public float[][] Centroids => _centroids;
        /// <summary>
        /// Gets variances of clusters.
        /// </summary>
        public float[] Variances => _variances;

        /// <summary>
        /// Computes metrics by evaluating the model for the given input data.
        /// </summary>
        /// <param name="mlContext">The ML.NET context.</param>
        /// <param name="testData">Test data used for metrics computation.</param>
        /// <returns></returns>
        public ClusteringMetrics Evaluate(MLContext mlContext, IDataView testData)
        {
            var transformedTestData = _transformer.Transform(testData);
            var metrics = mlContext.Clustering.Evaluate(transformedTestData, "PredictedLabel", "Score", "Features");
            return metrics;
        }
    }
}

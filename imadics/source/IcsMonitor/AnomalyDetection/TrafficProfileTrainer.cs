using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;
using System;
using System.Collections.Generic;
using System.Linq;
using Traffix.DataView;

namespace IcsMonitor.AnomalyDetection
{
    /// <summary>
    /// Trainer for creating a profile based on the provided dataview. 
    /// </summary>
    public class TrafficProfileTrainer
    {
        private readonly MLContext _ml;
        private readonly string[] _featureAxes;
        private readonly IndustrialProtocol _protocolType;
        private readonly string[] _featureColumns;
        private readonly TimeSpan _windowTimeSpan;
        private readonly string[] _tags;
        /// <summary>
        /// Creates a new instance of the trainer.
        /// </summary>
        /// <param name="ml">The ML.NET context.</param>
        /// <param name="pcaRank">The rank of the PCA space. If set to 0, then PCA is not used.</param>
        /// <param name="protocolType">The target protocol type.</param>
        /// <param name="featureColumns">Defines columns that contains features used in the model.</param>
        /// <param name="windowTimeSpan">The size of time window used for aggregating the input data.</param>
        /// <param name="tags">For some protocol, the model contains dynamic tags.</param>
        public TrafficProfileTrainer(MLContext ml, int pcaRank, IndustrialProtocol protocolType, string[] featureColumns, TimeSpan windowTimeSpan, string[] tags = null)
        {
            _ml = ml;
            _featureAxes = Enumerable.Range(1, pcaRank).Select(i => $"pca{i}").ToArray();
            _protocolType = protocolType;
            _featureColumns = featureColumns;
            _windowTimeSpan = windowTimeSpan;
            _tags = tags;
        }

        /// <summary>
        /// Gets the input data transformer fitted to the provided Dataview.
        /// <para/>
        /// The input data transformer creates features vector based on the fields as specified for the protocol, 
        /// normalizes the input data using min-max method and reduces the data dimensions using PCA method.
        /// This transformation can be used to prepare data for the profile trainer.
        /// </summary>
        /// <param name="dataview">The data view used to fit the input data transformer.</param>
        /// <param name="featureTransformer">The transformer used to compute actual input features from the candidate features.</param>
        /// <returns>The transformer for input data transformation fitted to the provided Dataview.</returns>
        public ITransformer GetTransformer(IDataView dataview, Func<IEstimator<ITransformer>, IEstimator<ITransformer>> featureTransformer)
        {
            // test that _featureColumns contains only Single values...
            foreach (var col in _featureColumns)
            {
                var colType = dataview.Schema.Last(x => x.Name == col).Type;
                if (colType == NumberDataViewType.Single) continue;
                if (colType is VectorDataViewType) continue;

                throw new ArgumentException($"Invalid feature type: Feature column '{col}' must have type Single or Vector<Single> in {nameof(dataview)}.");
            }
            var trainer = featureTransformer(_ml.Transforms.Concatenate("PreFeatures", _featureColumns));
            var transform = trainer.Fit(dataview);
            return transform;
        }

        /// <summary>
        /// Uses PCA method to compute features from pre-features.
        /// </summary>
        /// <param name="rank">The rank of the resulting PCA. </param>
        /// <returns>The input to output estimator function.</returns>
        public Func<IEstimator<ITransformer>, IEstimator<ITransformer>> PcaTransformer(int rank) =>
            (IEstimator<ITransformer> estimator) => estimator.Append(_ml.Transforms.NormalizeMinMax("PreFeatures", fixZero: true))
                                                             .Append(_ml.Transforms.ProjectToPrincipalComponents("Features", "PreFeatures", rank: rank));

        /// <summary>
        /// The direct transformation. It just uses the input features as output features.
        /// </summary>
        /// <param name="estimator">The input estimator.</param>
        /// <returns>A new estimator with a direct transformer applied.</returns>
        public IEstimator<ITransformer> DirectTransformer(IEstimator<ITransformer> estimator)
        {
            return estimator.Append(_ml.Transforms.NormalizeMinMax("PreFeatures", fixZero: true))
                            .Append(_ml.Transforms.CopyColumns("Features", "PreFeatures"));
        }

        /// <summary>
        /// Computes MIN,MAX,AVG,STDEV from the prefeatures. 
        /// </summary>
        /// <param name="estimator">The input estimator.</param>
        /// <returns>The output estimator with transformer applied.</returns>
        public IEstimator<ITransformer> AverageTransformer(IEstimator<ITransformer> estimator)
        {
            return estimator.Append(_ml.Transforms.CustomMapping(new AverageTransformerCustomAction().GetMapping(), contractName: "AverageTransformer"));
        }
        /// <summary>
        /// Creates a profile for the source <paramref name="dataview"/>.
        /// <para/>
        /// The profile consists of <paramref name="maxModelCount"/> models which are selected 
        /// form models computed for clusters in range between <paramref name="minClusters"/> and <paramref name="maxClusters"/>.
        /// </summary>
        /// <param name="profileName">The profile name.</param>
        /// <param name="dataview">An input data view with training data.</param>
        /// <param name="clusterCountVector">An array of cluster count values.</param>
        /// <param name="maxModelCount">The required number of models in the profile.</param>
        /// <returns>The profile for the traffic.</returns>
        public TrafficProfile Fit(string profileName, Dictionary<string, string> configuration, Func<IEstimator<ITransformer>, IEstimator<ITransformer>> featureTransformer, int[] clusterCountVector, int maxModelCount, IDataView dataview)
        {
            var transform = GetTransformer(dataview, featureTransformer);
            var trainData = transform.Transform(dataview);
            var trainer = new ModelTrainer(_ml);
            var models = clusterCountVector.Select(n => GetModel(trainer, trainData, n)).Where(x => x != null);
            var modelMetrics = models.Select(m => m.Evaluate(_ml, trainData));
            var bestModels = models.OrderBy(m => m.Evaluate(_ml, trainData).DaviesBouldinIndex).Take(maxModelCount).ToArray();
            return new TrafficProfile(_ml, bestModels, dataview.Schema, transform, new TrafficProfile.Settings { ProtocolType = _protocolType, WindowTimeSpan = _windowTimeSpan, ProfileName = profileName, Configuration = configuration });
        }
        /// <summary>
        /// Computes the <see cref="ClusterModel"/> using the given trainer and source data. 
        /// </summary>
        /// <param name="trainer">The trainer used for creating model.</param>
        /// <param name="trainData">The source data.</param>
        /// <param name="numberOfClusters">Target number of clusters.</param>
        /// <returns>A new model computed for the given arguments.</returns>
        private ClusterModel GetModel(ModelTrainer trainer, IDataView trainData, int numberOfClusters)
        {
            try
            {
                return trainer.TrainKMeansAnomalyDetector(trainData, new ClusterModel.Options { NumberOfClusters = numberOfClusters }, "Features", _featureAxes);
            }
            catch (InvalidOperationException ex)
            {
                Console.Error.WriteLine($"ERROR: Cannot create model: {ex.Message}");
                Console.Error.WriteLine($"       {ex.InnerException?.Message}");
                return null;
            }
        }

        /// <summary>
        /// The class implements a custom transformer that performs min/max/avg computation for input features.
        /// </summary>
        [CustomMappingFactoryAttribute("AverageTransformer")]
        private class AverageTransformerCustomAction : CustomMappingFactory<InputFeatureData, OutputFeatureData>
        {
            // We define the custom mapping between input and output rows that will
            // be applied by the transformation.
            /// <summary>
            /// Custom mapping between <paramref name="input"/> and <paramref name="output"/> data.
            /// </summary>
            /// <param name="input">The input feature data.</param>
            /// <param name="output">the output feature data.</param>
            public static void CustomAction(InputFeatureData input, OutputFeatureData output)
            {
                output.Features = new float[]
                {
                    input.PreFeatures.Min(),
                    input.PreFeatures.Max(),
                    input.PreFeatures.Average(),
                    StandardDeviation(input.PreFeatures)
                };
            }
            /// <summary>
            /// Gets the mapping object.
            /// </summary>
            /// <returns>the mmaping object of this custom transformation.</returns>
            public override Action<InputFeatureData, OutputFeatureData> GetMapping()
                => CustomAction;
        }

        /// <summary>
        /// Reepresents input feature data.
        /// </summary>
        public class InputFeatureData
        {
            /// <summary>
            /// Features are represented as an array of float values.
            /// </summary>
            [ColumnName("PreFeatures")]
            public float[] PreFeatures { get; set; }
        }

        /// <summary>
        /// Represents output feature data.
        /// </summary>
        public class OutputFeatureData
        {
            /// <summary>
            /// Features are represented as an array of float values.
            /// </summary>
            [ColumnName("Features")]
            [VectorType(4)]
            public float[] Features { get; set; }
        }
        /// <summary>
        /// Computes a standard deviation for the given sequence of values.
        /// </summary>
        /// <param name="sequence">The input sequence of float values.</param>
        /// <returns>the computed standard deviation.</returns>
        static float StandardDeviation(IEnumerable<float> sequence)
        { 
            if (sequence.Any())
            {
                var average = sequence.Average();
                var sum = sequence.Sum(d => Math.Pow(d - average, 2));
                return (float)Math.Sqrt((sum) / sequence.Count());
            }
            else
                return 0;
        }
    }
}

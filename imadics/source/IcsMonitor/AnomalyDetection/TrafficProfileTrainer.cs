using Microsoft.ML;
using System;
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
        private readonly string[] _pcaAxes;
        private readonly IndustrialProtocol _protocolType;
        private readonly string[] _featureColumns;
        private readonly TimeSpan _windowTimeSpan;

        /// <summary>
        /// Creates a new instance of the trainer.
        /// </summary>
        /// <param name="ml">The ML.NET context.</param>
        /// <param name="pcaRank">The rank of the PCA space.</param>
        /// <param name="protocolType">The target protocol type.</param>
        /// <param name="windowTimeSpan">The size of time window used for aggregating the input data.</param>
        public TrafficProfileTrainer(MLContext ml, int pcaRank, IndustrialProtocol protocolType, string[] featureColumns, TimeSpan windowTimeSpan)
        {
            _ml = ml;
            _pcaAxes = Enumerable.Range(1, pcaRank).Select(i => $"pca{i}").ToArray();
            _protocolType = protocolType;
            _featureColumns = featureColumns;
            _windowTimeSpan = windowTimeSpan;
        }
        /// <summary>
        /// Gets the input data transformer fitted to the provided Dataview.
        /// <para/>
        /// The input data transformer creates features vector based on the fields as specified for the protocol, 
        /// normalizes the input data using min-max method and reduces the data dimensions using PCA method.
        /// This transformation can be used to prepare data for the profile trainer.
        /// </summary>
        /// <param name="dataview">The data view used to fit the input data transformer.</param>
        /// <returns>The transformer for input data transformation fitted to the provided Dataview.</returns>
        public ITransformer GetTransformer(IDataView dataview)
        {
            var trainer = _ml.Transforms.CreateFeatureVector("PreFeatures", _featureColumns)
                            .Append(_ml.Transforms.NormalizeMinMax("PreFeatures", fixZero: true))
                            .Append(_ml.Transforms.ProjectToPrincipalComponents("Features", "PreFeatures", rank: _pcaAxes.Length));
            var transform = trainer.Fit(dataview);
            return transform;
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
        public TrafficProfile Fit(string profileName, IDataView dataview, int[] clusterCountVector, int maxModelCount)
        {
            var transform = GetTransformer(dataview);
            var trainData = transform.Transform(dataview);
            var trainer = new ModelTrainer(_ml);
            var models = clusterCountVector.Select(n => GetModel(trainer, trainData, n)).Where(x => x != null);
            var modelMetrics = models.Select(m => m.Evaluate(_ml, trainData));
            var bestModels = models.OrderBy(m => m.Evaluate(_ml, trainData).DaviesBouldinIndex).Take(maxModelCount);
            return new TrafficProfile(_ml, bestModels.ToArray(), dataview.Schema, transform, new TrafficProfile.Settings { ProtocolType = _protocolType, WindowTimeSpan = _windowTimeSpan, ProfileName = profileName });
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
                return trainer.TrainKMeansAnomalyDetector(trainData, new ClusterModel.Options { NumberOfClusters = numberOfClusters }, "Features", _pcaAxes);
            }
            catch (InvalidOperationException ex)
            {
                Console.Error.WriteLine($"ERROR: Cannot create model: {ex.Message}");
                return null;
            }
        }
    }
}

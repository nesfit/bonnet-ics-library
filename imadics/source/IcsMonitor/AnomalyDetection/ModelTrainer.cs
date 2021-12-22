using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IcsMonitor.AnomalyDetection
{
    /// <summary>
    /// Represents anomaly detection model trainer.
    /// <para/>
    /// It provides methods for training different anomaly detection methods.
    /// </summary>
    public class ModelTrainer
    {
        private readonly MLContext _mlContext;

        /// <summary>
        /// Creates a new trainer in the given context.
        /// </summary>
        public ModelTrainer(MLContext mlContext)
        {
            _mlContext = mlContext;
        }

        /// <summary>
        /// Variance is computed as v = 1/n \sum_{i=1}^{n}(x_i - x_{mean})^2 .
        /// <para/>
        /// We want to avoid zero variance. If such situation occurs we used <see cref="float.Epsilon"/> instead.
        /// </summary>
        /// <param name="p">The collection of predictions.</param>
        /// <returns>The variance value.</returns>
        static float ComputeVariance(IEnumerable<ClusterModel.Output> p)
        {
            var values = p.ToList();
            if (values.Count > 1)
            {
                var variance = (float)(values.Sum(s => Math.Pow(s.Distance, 2)) / values.Count);
                return Math.Max(variance, float.Epsilon);
            }
            else
                return float.Epsilon;
        }

        /// <summary>
        /// Creates a single K-Means model of network traffic for the given input and configuration.
        /// </summary>
        /// <param name="trainingDataView">The training data.</param>
        /// <param name="options">The method options.</param>
        /// <param name="featuresColumnName">A name of the features column.</param>
        /// <param name="slotNames">An array of slot names of the features vector.</param>
        /// <returns>New <see cref="ClusterModel"/> model created from the given training data.</returns>
        public ClusterModel TrainKMeansAnomalyDetector(IDataView trainingDataView, ClusterModel.Options options, string featuresColumnName, params string[] slotNames)
        {
            var kmeansOptions = new KMeansTrainer.Options
            {
                InitializationAlgorithm = KMeansTrainer.InitializationAlgorithm.KMeansPlusPlus,
                NumberOfClusters = options.NumberOfClusters,
                OptimizationTolerance = 1e-6f,
                NumberOfThreads = 1,
                FeatureColumnName = featuresColumnName
            };
            var trainer = _mlContext.Clustering.Trainers.KMeans(kmeansOptions);
            var model = trainer.Fit(trainingDataView);

            VBuffer<float>[] centroidsBuffer = default;
            model.Model.GetClusterCentroids(ref centroidsBuffer, out var k);
            var centroids = centroidsBuffer.Select(c => c.DenseValues().ToArray()).ToArray();

            var output = _mlContext.Data.CreateEnumerable<ClusterModel.Output>(model.Transform(trainingDataView), reuseRowObject: false, ignoreMissingColumns: true).ToList();
            var varianceVector = output
                .GroupBy(x => x.ClusterId)
                .Select(p => (p.Key, Variance: ComputeVariance(p)))
                .OrderBy(p => p.Key)
                .Select(p => p.Variance).ToArray();

            var anomalyDetectionModel = new ClusterModel(model, trainingDataView.Schema, slotNames, centroids, varianceVector);

            return anomalyDetectionModel;
        }
    }
}

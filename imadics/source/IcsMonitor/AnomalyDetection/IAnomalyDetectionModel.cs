using Microsoft.ML;
using Microsoft.ML.Data;
using System.Collections.Generic;

namespace IcsMonitor.AnomalyDetection
{
    public interface IAnomalyDetectionModel<TOutput>
    {
        /// <summary>
        /// Stores the model in the file on the given <see cref="path"/>.
        /// </summary>
        /// <param name="mlContext">The ML.NET context.</param>
        /// <param name="path">The path of output file.</param>
        void SaveToFile(MLContext mlContext, string path);

        /// <summary>
        /// Transforms the <paramref name="source"/> dataview using the anomaly detection model.
        /// </summary>
        /// <param name="mlContext">The ML.NET context.</param>
        /// <param name="source">The source dataview which rows are to be evaluated.</param>
        /// <returns>The output for each input row.</returns>
        public IEnumerable<TOutput> Transform(MLContext mlContext, IDataView source);

        /// <summary>
        /// Evaluates the model using <paramref name="testData"/> to produce <see cref="ClusteringMetrics"/>.
        /// </summary>
        /// <param name="mlContext">The ML.NET context.</param>
        /// <param name="testData">The test data used to evaluate the model.</param>
        /// <returns>The metrics of the computed model.</returns>
        public ClusteringMetrics Evaluate(MLContext mlContext, IDataView testData);
    }
}
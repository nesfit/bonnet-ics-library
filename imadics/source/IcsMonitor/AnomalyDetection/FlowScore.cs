using System;
using System.Linq;

namespace IcsMonitor.AnomalyDetection
{

    /// <summary>
    /// Represents the score of the each flow as computed by the profile.
    /// </summary>
    /// <param name="FlowKey">The flow key.</param>
    /// <param name="Features">Values of the computed features.</param>
    /// <param name="Distances">An array of distances to the closes centroids for all models.</param>
    /// <param name="Scores">An array of scores computed for each model.</param>
    public record FlowScore(string FlowKey, DateTime WindowStart, TimeSpan WindowDuration, string FlowLabel, float[] Features, double[] Distances, double[] Scores)
    {
        /// <summary>
        /// Gets the maximum score.
        /// </summary>
        public double MaxScore => Scores.Max();
        /// <summary>
        /// Gets the minimum score.
        /// </summary>
        public double MinScore => Scores.Min();
        /// <summary>
        /// Gets the averegae score.
        /// </summary>
        public double AverageScore => Scores.Average();
        /// <summary>
        /// Gets the index of the best model, i.e., a model having the best score.
        /// </summary>
        public int BestModel => Array.IndexOf(Scores, MaxScore);
    }
}

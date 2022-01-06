using System;
using System.Linq;

namespace SampleScenes
{
    /// <summary>
    /// The class provides some useful information related to running time, cycle time statistics, etc.
    /// </summary>
    public class ElapsedTimeObserver
    {
        long _totalRunningTime;
        int[] _elapsedDatapoints;
        int _headIndexToElapsedDatapoints;

        public ElapsedTimeObserver(int numberOfPoints)
        {
            _elapsedDatapoints = new int[numberOfPoints];
        }

        public TimeSpan RunningTime  => TimeSpan.FromMilliseconds(_totalRunningTime);
        public TimeSpan MovingAvg => TimeSpan.FromMilliseconds(_elapsedDatapoints.Average());

        public TimeSpan Max => TimeSpan.FromMilliseconds(_elapsedDatapoints.Max());
        public TimeSpan Min => TimeSpan.FromMilliseconds(_elapsedDatapoints.Min());
        public void Add(int elapsedMilliseconds)
        {
            _totalRunningTime += elapsedMilliseconds;
            _elapsedDatapoints[_headIndexToElapsedDatapoints] = elapsedMilliseconds;
            _headIndexToElapsedDatapoints = (_headIndexToElapsedDatapoints+1) % _elapsedDatapoints.Length;
        }
    }

}

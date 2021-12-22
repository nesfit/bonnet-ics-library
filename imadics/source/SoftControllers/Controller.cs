using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;

namespace SoftControllers
{
    /// <summary>
    /// This is base controller inspired by Factory I/O SDK implementation.
    /// </summary>
    public abstract class Controller : IDisposable
    {
        /// <summary>
        /// Cycle time in milliseconds.
        /// </summary>
        public int CycleTime { get; set; } = 50;

        /// <summary>
        /// Execute a single cycle. This method should be implemented by controllers.
        /// </summary>
        /// <param name="cycle"></param>
        /// <param name="elapsedMilliseconds"></param>
        protected abstract Task ExecuteAsync(int cycle, int elapsedMilliseconds, CancellationToken cancel);

        /// <summary>
        /// Call when the programs is to be started. It enables the initialization 
        /// RTU connections and set up initial values for actuators, registers, etc.
        /// </summary>
        /// <param name="cancel">The cancellation token.</param>
        /// <returns>Completion task object.</returns>
        protected abstract Task InitializeAsync(CancellationToken cancel);

        /// <summary>
        /// Used to finalize the controller.
        /// </summary>
        /// <returns></returns>
        protected abstract Task FinalizeAsync();

        private Stopwatch _runningWatch = new Stopwatch();

        private CsvWriter _logWriter;
                private DataPoint [] _datapoints;
        
        /// <summary>
        /// Used to register datapoints in the base class for vairous purposes, e.g., logging, visualization, etc.
        /// </summary>
        /// <param name="datapoints">The collection of datapoints.</param>
        protected void RegisterDatapoints(params DataPoint[] datapoints)
        {
            _datapoints = datapoints;
        }

        protected int AutoRegisterDatapoints(object obj)
        {
            if (obj is null) throw new ArgumentNullException(nameof(obj));
            var objType = obj.GetType();
            List<DataPoint> datapoints = new List<DataPoint>();
            foreach(var f in obj.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Where(f=> f.FieldType.IsSubclassOf(typeof(DataPoint))))
            {
                datapoints.Add(f.GetValue(obj) as DataPoint);   
            }
            _datapoints = datapoints.ToArray();
            return _datapoints.Length;
        }
        void AddDataPoints(dynamic record, params DataPoint[] registers)
        {
            var dict = record as IDictionary<string,object>;
            foreach(var reg in registers)
            {
                dict[reg.Tag] = reg.GetLastValue();
            }
        }
        void LogDataPoints()
        {
            if (_logWriter is not null)
            {
                dynamic record = new ExpandoObject();            
                record.Time = _runningWatch.ElapsedMilliseconds;
                AddDataPoints(record,_datapoints);
                _logWriter.WriteRecord<dynamic>(record);
                _logWriter.NextRecord();
            }
        }
        /// <summary>
        /// Gets the information of whether the Factory simulation is running or not.
        /// The control loop can be terminated by provided <paramref name="cancellationToken"/>.
        /// </summary>
        public async Task Run(CancellationToken cancellationToken)
        {
            var stopwatch = new Stopwatch();
            int cycle = 0;
            await InitializeAsync(cancellationToken);
            _runningWatch.Restart();
            while (!cancellationToken.IsCancellationRequested)
            {
                var elapsed = stopwatch.ElapsedMilliseconds;
                stopwatch.Restart();
                await ExecuteAsync(++cycle, (int)elapsed, cancellationToken);
                _ = Task.Run(() => LogDataPoints());
                var delay = CycleTime - (int)stopwatch.ElapsedMilliseconds;
                if (delay > 0)
                    await Task.Delay(delay);
            }
            await FinalizeAsync();
        }
        private bool _disposed = false;

        protected Controller(TextWriter logWriter = null)
        {
            _logWriter = logWriter is not null ? new CsvWriter(logWriter, CultureInfo.InvariantCulture) : null;
        }

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                _logWriter?.Dispose();
                return;
            }
            _disposed = true;
        }
    }
}

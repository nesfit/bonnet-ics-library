using System;
using System.Threading.Tasks;
namespace SoftControllers
{
    /// <summary>
    /// The factory manager checks provides the access to some simulation parameters.
    /// </summary>
    public abstract class FactoryManager : IDisposable
    {
        /// <summary>
        /// Sets or gets the interval when the manager reads the simulation value. 
        /// </summary>
        public int CheckingInterval { get; set; } = 500;

        /// <summary>
        /// Test if the simulation is running.
        /// </summary>
        public abstract bool IsRunning { get; }

        /// <summary>
        /// Returns Task that complets when the Factory simulation starts (is in running state).
        /// </summary>
        /// <returns>Task that complets when the Factory simulation starts.</returns>
        public async Task WaitForStart()
        {
            bool isrunning = false;
            while(!isrunning)
            {
                await Task.Delay(CheckingInterval).ContinueWith( t => isrunning = IsRunning );
            }
        }

        #region IDisposable implementation
        private bool _disposed = false;
        public void Dispose() => Dispose(true);
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // Dispose managed state (managed objects).
            }

            _disposed = true;
        }
        #endregion
    }
}

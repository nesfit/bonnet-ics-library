using CsvHelper;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.Net.NetworkInformation;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace IcsMonitor
{
    internal class CaptureFileReplayDevice : ICaptureDevice
    {
        private float _replaySpeed;
        private CaptureFileReaderDevice _captureFileReaderDevice;

        public CaptureFileReplayDevice(string captureFileName, float replaySpeed)
        {
            this._replaySpeed = replaySpeed;
            this._captureFileReaderDevice = new CaptureFileReaderDevice(captureFileName);
        }

        /// <summary>
        /// Gets the replay speed for the current device. 
        /// If the replay speed equals 1 then the packets are replay in the intervals as given by their timestamps.
        /// For replay speeds greater than 1 the replay is faster and for less then 1 replay is slower.
        /// </summary>
        public float ReplaySpeed => _replaySpeed;

        public string Name => _captureFileReaderDevice.Name;

        public string Description => _captureFileReaderDevice.Description;

        public string LastError => _captureFileReaderDevice.LastError;

        public string Filter { get => _captureFileReaderDevice.Filter; set => _captureFileReaderDevice.Filter = value; }

        public ICaptureStatistics Statistics => _captureFileReaderDevice.Statistics;

        public PhysicalAddress MacAddress => _captureFileReaderDevice.MacAddress;

        public bool Started => _captureFileReaderDevice.Started;

        public TimeSpan StopCaptureTimeout { get => _captureFileReaderDevice.StopCaptureTimeout; set => _captureFileReaderDevice.StopCaptureTimeout = value; }

        public LinkLayers LinkType => _captureFileReaderDevice.LinkType;

        public event PacketArrivalEventHandler OnPacketArrival;
        public event CaptureStoppedEventHandler OnCaptureStopped;

        public void Capture()
        {
            _captureFileReaderDevice.Capture();
        }

        public void Close()
        {
            _captureFileReaderDevice.Close();
        }

        public RawCapture GetNextPacket()
        {
            return _captureFileReaderDevice.GetNextPacket();
        }

        public int GetNextPacketPointers(ref IntPtr header, ref IntPtr data)
        {
            return _captureFileReaderDevice.GetNextPacketPointers(ref header, ref data);
        }

        public void Open()
        {
            _captureFileReaderDevice.Open();
        }

        public void Open(DeviceMode mode)
        {
            _captureFileReaderDevice.Open(mode);
        }

        public void Open(DeviceMode mode, int read_timeout)
        {
            _captureFileReaderDevice.Open(mode, read_timeout);
        }

        public void Open(DeviceMode mode, int read_timeout, uint kernel_buffer_size)
        {
            _captureFileReaderDevice.Open(mode, read_timeout, kernel_buffer_size);
        }

        public void Open(DeviceMode mode, int read_timeout, MonitorMode monitor_mode)
        {
            _captureFileReaderDevice.Open(mode, read_timeout, monitor_mode);
        }

        public void Open(DeviceMode mode, int read_timeout, MonitorMode monitor_mode, uint kernel_buffer_size)
        {
            _captureFileReaderDevice.Open(mode, read_timeout, monitor_mode, kernel_buffer_size);
        }

        public void SendPacket(Packet p)
        {
            _captureFileReaderDevice.SendPacket(p);
        }

        public void SendPacket(Packet p, int size)
        {
            _captureFileReaderDevice.SendPacket(p,size);
        }

        public void SendPacket(byte[] p)
        {
            _captureFileReaderDevice.SendPacket(p);
        }

        public void SendPacket(byte[] p, int size)
        {
            _captureFileReaderDevice.SendPacket(p,size);
        }

        public void SendPacket(ReadOnlySpan<byte> p)
        {
            _captureFileReaderDevice.SendPacket(p);
        }

        /// <summary>
        /// Called before the reader wants to receive the packets.
        /// </summary>
        public void StartCapture()
        {
            if (!Started)
            {
                if (!_captureFileReaderDevice.Opened)
                    throw new InvalidOperationException("Can't start capture, the pcap device is not opened.");

                if (OnPacketArrival == null)
                    throw new InvalidOperationException("No delegates assigned to OnPacketArrival, no where for captured packets to go.");

                var cancellationToken = threadCancellationTokenSource.Token;
                captureThread = Task.Run(() => CaptureThreadAsync(cancellationToken), cancellationToken);
            }

        }

        /// <summary>
        /// Thread that is performing the background packet capture.
        /// </summary>
        protected Task captureThread;

        /// <summary>
        /// Flag that indicates that a capture thread should stop.
        /// </summary>
        protected CancellationTokenSource threadCancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Stops the capture process
        /// <para/>
        /// Throws an exception if the stop capture timeout is exceeded and the
        /// capture thread was aborted
        /// </summary>
        public void StopCapture()
        {
            if (Started)
            {
                threadCancellationTokenSource.Cancel();
                threadCancellationTokenSource = new CancellationTokenSource();
                Task.WaitAny(new[] { captureThread }, StopCaptureTimeout);
                SendCaptureStoppedEvent(CaptureStoppedEventStatus.CompletedWithoutError);
                captureThread = null;
            };
        }

        /// <summary>
        /// Notify the OnPacketArrival delegates about a newly captured packet
        /// </summary>
        /// <param name="header"></param>
        /// <param name="data"></param>
        protected void SendPacketArrivalEvent(RawCapture rawCapture)
        {
            OnPacketArrival?.Invoke(this, new CaptureEventArgs(rawCapture, this));
        }
        protected void SendCaptureStoppedEvent(CaptureStoppedEventStatus status)
        {
            OnCaptureStopped?.Invoke(this, status);
        }


        protected async Task CaptureThreadAsync(CancellationToken cancellationToken)
        {
            if (!_captureFileReaderDevice.Opened)
                throw new InvalidOperationException("Capture called before PcapDevice.Open()");

            // Get the first capture and initializes necessary values:
            var rawCapture = this.GetNextPacket();
            if (rawCapture == null)
            {
                SendCaptureStoppedEvent(CaptureStoppedEventStatus.CompletedWithoutError);
                return;
            }
            var virtualTimeOrigin = rawCapture.Timeval.Date.Ticks;
            var realTimeOrigin = DateTime.Now.Ticks;
            SendPacketArrivalEvent(rawCapture);

            while (!cancellationToken.IsCancellationRequested)
            {
                rawCapture = this.GetNextPacket();
                if (rawCapture == null)
                {
                    SendCaptureStoppedEvent(CaptureStoppedEventStatus.CompletedWithoutError);
                    return;
                }
                var virtualOffsetTicks = rawCapture.Timeval.Date.Ticks - virtualTimeOrigin;
                var realOffsetTicks = DateTime.Now.Ticks - realTimeOrigin;
                var expectedRealOffsetTicks = (long)(virtualOffsetTicks / _replaySpeed);
                var ticksToWait = expectedRealOffsetTicks - realOffsetTicks;
                if (ticksToWait > 0)
                {
                    await Task.Delay(new TimeSpan(ticksToWait));
                }
                SendPacketArrivalEvent(rawCapture);
            }
        }
    }
}
using System.Net;
using System.Net.Sockets;
using Modbus.Device;
using System.Linq;
using System;

namespace SoftControllers
{
    public class ModbusServerFactoryManager : FactoryManager
    {
        private readonly ModbusDevice _device;
        private readonly DiscreteInput _factoryRunningSensor;
        private TcpClient _tcpclient;
        public ModbusServerFactoryManager(IPEndPoint endPoint, byte deviceId, RegisterMap rmap)
        {
            _tcpclient = new TcpClient ();
            _tcpclient.Connect (endPoint.Address, endPoint.Port);
            var modbus = ModbusIpMaster.CreateIp (_tcpclient); 
            _device = new ModbusDevice(modbus, endPoint, deviceId);
            _factoryRunningSensor = rmap.GetDiscreteInput(_device, "FACTORY I/O (Running)");
        }

        public override bool IsRunning => _factoryRunningSensor.Value;

        private bool _disposed = false;
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
            // Dispose managed state (managed objects).
                 _device?.Dispose(); 
                _tcpclient?.Dispose();
            }

            _disposed = true;

            // Call base class implementation.
            base.Dispose(disposing);
        }
    }
}

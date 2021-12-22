using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Modbus.Device;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using CsvHelper;
using System.Dynamic;

namespace SoftControllers
{
    /// <summary>
    /// A base class of controllers that communicats with RTU using Modbus. 
    /// <para>
    /// The types of registers in Modbus devices include the following:
    /// • Coil (Discrete Output), single read-write bit  
    /// • Discrete Input (or Status Input), single read-only bit
    /// • Input Register - ushort read-only value
    /// • Holding Register - ushort read-write value
    /// </para>
    /// </summary>
    public abstract class ModbusController : Controller
    {
        private TcpClient _tcpclient;
        private readonly ModbusDevice _device;

        public ModbusDevice Device => _device;

        
        protected ModbusController(IPEndPoint endpoint, byte deviceId, TextWriter logWriter = null) : base(logWriter)
        {
            _tcpclient = new TcpClient ();
            _tcpclient.Connect(endpoint.Address, endpoint.Port);
            var modbus = ModbusIpMaster.CreateIp (_tcpclient);
            _device = new ModbusDevice(modbus, endpoint, deviceId);
        }

        #region Disposable overrides
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
                this._device?.Dispose();
                this._tcpclient?.Dispose();
            }

            _disposed = true;
            // Call base class implementation.
            base.Dispose(disposing);
        }
        #endregion
    }
    

    public class HoldingRegister : DataPoint<ushort>
    {
        public HoldingRegister(ModbusDevice device, string registerTag, ushort registerAddress) : base(device, registerTag, registerAddress)
        {}

        protected override ushort GetValue()
        {
            return _device.Modbus.ReadHoldingRegisters(_device.DeviceId,_address, 1).First();
        }

        protected override void SetValue(ushort value)
        {
            _device.Modbus.WriteSingleRegister(_device.DeviceId,_address, value);
        }

        public float FloatValue
        {
            get
            {
                return ((float)this.Value) / Scale; 
            }
            set
            {
                Value = (ushort)(value * Scale);
            }
        }
    }
    public class InputRegister : DataPoint<ushort>
    {
        public InputRegister(ModbusDevice device, string registerTag, ushort registerAddress) : base (device, registerTag, registerAddress) {}

        protected override ushort GetValue()
        {
            return _device.Modbus.ReadInputRegisters(_device.DeviceId,_address, 1).First();
        }

        protected override void SetValue(ushort value)
        {
            throw new NotSupportedException();
        }
        public float FloatValue
        {
            get
            {
                return ((float)this.Value) / Scale; 
            }
        }

    }
    public class DiscreteInput : DataPoint<bool>
    {
        public DiscreteInput(ModbusDevice device, string registerTag, ushort registerAddress) : base(device, registerTag, registerAddress) {}

        protected override bool GetValue()
        {
            return _device.Modbus.ReadInputs(_device.DeviceId,_address, 1)?.FirstOrDefault() ?? false;
        }

        protected override void SetValue(bool value)
        {
            throw new NotSupportedException();
        }
    }
    public class Coil : DataPoint<bool>
    {
        public Coil(ModbusDevice device, string registerTag, ushort registerAddress) : base(device, registerTag, registerAddress) {}
        protected override bool GetValue()
        {
            return _device.Modbus.ReadCoils(_device.DeviceId, _address, 1)?.FirstOrDefault() ?? false;
        }

        protected override void SetValue(bool value)
        {
            _device.Modbus.WriteSingleCoil(_device.DeviceId, _address, value);
        }
    }

    public record ModbusDevice(ModbusIpMaster Modbus, IPEndPoint EndPoint, byte DeviceId) : IDisposable
    {
        public void Dispose()
        {
            Modbus?.Dispose();
        }
    }
}

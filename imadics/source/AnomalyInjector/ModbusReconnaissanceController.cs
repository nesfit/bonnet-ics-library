using Modbus.Device;
using Modbus.Message;
using SoftControllers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AnomalyInjector
{
    public enum CheckStatus { OkResponse, ErrorResponse, Timeout, NotSupported }
    public record ModbusSlaveRecord(byte Address, CheckStatus Status);
    public record ModbusFunctionAvailability(byte FunctionCode, CheckStatus Status);

    internal class ModbusReconnaissanceController : IDisposable
    {
        private IPEndPoint _endpoint;
        private ModbusIpMaster _master = null;
        private TcpClient _tcpclient;


        void ConnectMaster()
        {
            _tcpclient = new TcpClient();
            _tcpclient.Connect(_endpoint.Address, _endpoint.Port);
            _master = ModbusIpMaster.CreateIp(_tcpclient);
            _master.Transport.ReadTimeout = 500;
            _master.Transport.Retries = 0;
        }
        private ModbusMaster GetConnectedMaster()
        {
            if (_tcpclient?.Connected == true && ReuseConnection) return _master;
            _tcpclient?.Dispose();
            _master?.Dispose();
            ConnectMaster();
            return _master;
        }

        public ModbusReconnaissanceController(IPEndPoint endpoint)
        {
            _endpoint = endpoint;
        }
        public byte SlaveAddress { get; set; } = 1;
        public TimeSpan Pause { get; set; } = TimeSpan.Zero;

        bool ReuseConnection { get; set; } = true;

        public void Dispose()
        {
            _tcpclient.Close();
            ((IDisposable)_master).Dispose();
            ((IDisposable)_tcpclient).Dispose();
        }

        public async IAsyncEnumerable<ModbusSlaveRecord> SlaveAddressScanAsync(IEnumerable<int> addresses)
        {
            foreach (var address in addresses)
            {
                var master = GetConnectedMaster();

                ModbusSlaveRecord record;
                try
                {
                    var request = new ReadCoilsInputsRequest(2, (byte)address, 0, 1);
                    var response = master.ExecuteCustomMessage<ReadCoilsInputsResponse>(request);
                    
                    record = (response.FunctionCode == request.FunctionCode) ?
                         new ModbusSlaveRecord((byte)address, CheckStatus.OkResponse) :
                            new ModbusSlaveRecord((byte)address, CheckStatus.ErrorResponse);
                }
                catch (IOException)
                {
                    record = new ModbusSlaveRecord((byte)address, CheckStatus.Timeout);
                }
                yield return record;
                await Task.Delay(Pause);
            }
        }

        public async IAsyncEnumerable<ModbusFunctionAvailability> FunctionCodeScanAsync(IEnumerable<int> functionCodes)
        {
            foreach (var functionCode in functionCodes)
            {
                var master = GetConnectedMaster();

                ModbusFunctionAvailability functionAvailability;
                try
                {
                    // create custom message: 
                    var request = ModbusMessageFactory.CreateModbusMessage<CustomModbusMessage>(new byte[] { 
                        /*
                        0x0, 0x1,               // transaction identifier 
                        0x0, 0x0,               // protocol identifier
                        0x0, 0x6,               // message length */
                        SlaveAddress,           // slave device adress
                        (byte)functionCode,     // function code
                        0x0, 0x00, 0x00, 0x00   // data
                    });
                    var response = master.ExecuteCustomMessage<CustomModbusMessage>(request);
                    functionAvailability = new ModbusFunctionAvailability((byte)functionCode, CheckStatus.OkResponse);
                }
                catch(Modbus.SlaveException ex)
                {
                    switch(ex.SlaveExceptionCode)
                    {
                        case 1: functionAvailability = new ModbusFunctionAvailability((byte)functionCode, CheckStatus.NotSupported); break;
                        default: functionAvailability = new ModbusFunctionAvailability((byte)functionCode, CheckStatus.ErrorResponse); break;
                    }
                }
                catch (IOException)
                {
                    functionAvailability = new ModbusFunctionAvailability((byte)functionCode, CheckStatus.Timeout);
                }
                yield return functionAvailability;
                await Task.Delay(Pause);
            }
        }
    }
}

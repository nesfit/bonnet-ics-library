using Modbus.Data;
using Modbus.Device;
using Modbus.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AnomalyInjector
{
    /// <summary>
    /// Implements proxy-based attacks. It creates 
    /// a new connection to MODBUS device and forwards modified messages
    /// between client and server.
    /// </summary>
    internal class ModbusProxyController : IDisposable
    {
        private IPEndPoint _endpoint;
        private ModbusIpMaster _modbusMaster;
        private TcpClient _tcpclient;

        public ModbusProxyController(IPEndPoint endpoint, byte deviceAddress)
        {
            _endpoint = endpoint;
            SlaveAddress = deviceAddress;
        }
        public byte SlaveAddress { get; set; } = 1;
        public TimeSpan Pause { get; set; } = TimeSpan.Zero;

        void ConnectMaster()
        {
            _tcpclient = new TcpClient();
            _tcpclient.Connect(_endpoint.Address, _endpoint.Port);
            _modbusMaster = ModbusIpMaster.CreateIp(_tcpclient);
            _modbusMaster.Transport.ReadTimeout = 500;
            _modbusMaster.Transport.Retries = 0;
        }
        private ModbusMaster GetConnectedMaster()
        {
            if (_tcpclient?.Connected == true && ReuseConnection) return _modbusMaster;
            _tcpclient?.Dispose();
            _modbusMaster?.Dispose();
            ConnectMaster();
            return _modbusMaster;
        }
        bool ReuseConnection { get; set; } = true;

        public void Dispose()
        {
            _tcpclient.Close();
            ((IDisposable)_modbusMaster).Dispose();
            ((IDisposable)_tcpclient).Dispose();
        }

        public async Task RunProxy(CancellationToken cancellationToken)
        {

            byte slaveId = SlaveAddress;
            int port = 502;

            var slaveTcpListener = new TcpListener(IPAddress.Loopback, port);
            slaveTcpListener.Start();

            var slave = ModbusTcpSlave.CreateTcp(slaveId, slaveTcpListener);
            slave.ModbusSlaveRequestReceived += Slave_ModbusSlaveRequestReceived;
            slave.DataStore = DataStoreFactory.CreateDefaultDataStore(32,32,32,32);
            //slave.DataStore.DataStoreReadFrom += Datastore_DataStoreReadFrom;
            slave.DataStore.DataStoreWrittenTo += Datastore_DataStoreWrittenTo;
            

            await slave.ListenAsync();

            void Datastore_DataStoreWrittenTo(object sender, DataStoreEventArgs e)
            {
                Console.WriteLine($"DataWrite: type={e.ModbusDataType}, address={e.StartAddress}, values={GetDataValues(e.Data)}");
                var master = GetConnectedMaster();
                switch (e.ModbusDataType)
                {
                    case ModbusDataType.HoldingRegister:
                        {
                            if (e.Data.B.Count == 1)
                            {
                                master.WriteSingleRegister(SlaveAddress, e.StartAddress, e.Data.B[0]);
                            }
                            else
                            {
                                master.WriteMultipleRegisters(SlaveAddress, e.StartAddress, e.Data.B.ToArray());
                            }
                            break;
                        }
                    case ModbusDataType.Coil:
                        {
                            if (e.Data.A.Count == 1)
                            {
                                master.WriteSingleCoil(SlaveAddress, e.StartAddress, e.Data.A[0]);
                            }
                            else
                            {
                                master.WriteMultipleCoils(SlaveAddress, e.StartAddress, e.Data.A.ToArray());
                            }
                            break;
                        }
                }
            }

            void Datastore_DataStoreReadFrom(object sender, DataStoreEventArgs e)
            {
                var datastore = (DataStore)sender;
                Console.WriteLine($"DataRead: type={e.ModbusDataType}, address={e.StartAddress}, values={GetDataValues(e.Data)}");
                var master = GetConnectedMaster();
                switch (e.ModbusDataType)
                {
                    case ModbusDataType.HoldingRegister:
                        {
                            var values = master.ReadHoldingRegisters(SlaveAddress, e.StartAddress, (ushort)e.Data.B.Count);
                            for (int i = 0; i < values.Length; i++)
                            {
                                datastore.HoldingRegisters[e.StartAddress + i + 1] = values[i];
                                Console.WriteLine($"Update Holding Register: address={e.StartAddress + i}, value={values[i]}");
                            }
                            break;
                        }
                    case ModbusDataType.InputRegister:
                        {
                            var values = master.ReadInputRegisters(SlaveAddress, e.StartAddress, (ushort)e.Data.B.Count);
                            for (int i = 0; i < values.Length; i++)
                            {
                                datastore.InputRegisters[e.StartAddress + i + 1] = values[i];
                                Console.WriteLine($"Update Input Register: address={e.StartAddress + i}, value={values[i]}");
                            }
                            break;
                        }
                    case ModbusDataType.Coil:
                        {
                            var values = master.ReadCoils(SlaveAddress, e.StartAddress, (ushort)e.Data.A.Count);
                            for (int i = 0; i < values.Length; i++)
                            {
                                datastore.CoilDiscretes[e.StartAddress + i + 1] = values[i];
                                Console.WriteLine($"Update Coil: address={e.StartAddress + i}, value={values[i]}");
                            }
                            break;
                        }
                    case ModbusDataType.Input:
                        {
                            var values = master.ReadInputs(SlaveAddress, e.StartAddress, (ushort)e.Data.A.Count);
                            for (int i = 0; i < values.Length; i++)
                            {
                                datastore.InputDiscretes[e.StartAddress + i + 1] = values[i];
                                Console.WriteLine($"Update Input: address={e.StartAddress + i}, value={values[i]}");
                            }
                            break;
                        }
                }
            }
        }

        private void Slave_ModbusSlaveRequestReceived(object sender, ModbusSlaveRequestEventArgs e)
        {
            var datastore = ((ModbusSlave)sender).DataStore;
            var master = GetConnectedMaster();
            switch (e.Message)
            {
                case Modbus.Message.ReadCoilsInputsRequest r when r.FunctionCode == 1:
                    
                    var coils = master.ReadCoils(r.SlaveAddress, r.StartAddress, r.NumberOfPoints);
                    for (int i = 0; i < coils.Length; i++)
                    {
                        datastore.CoilDiscretes[r.StartAddress + i + 1] = coils[i];
                    }
                    break;
                case Modbus.Message.ReadCoilsInputsRequest r when r.FunctionCode == 2:
                    var inputs = master.ReadInputs(r.SlaveAddress, r.StartAddress, r.NumberOfPoints);
                    for (int i = 0; i < inputs.Length; i++)
                    {
                        datastore.InputDiscretes[r.StartAddress + i + 1] = inputs[i];
                    }
                    break;
                case Modbus.Message.ReadHoldingInputRegistersRequest r  when r.FunctionCode == 3:
                    var values = master.ReadHoldingRegisters(SlaveAddress, r.StartAddress, r.NumberOfPoints);
                    for (int i = 0; i < values.Length; i++)
                    {
                        datastore.HoldingRegisters[r.StartAddress + i + 1] = values[i];
                    }
                    break;
                case Modbus.Message.ReadHoldingInputRegistersRequest r when r.FunctionCode == 4:
                    var registers = master.ReadInputRegisters(SlaveAddress, r.StartAddress, r.NumberOfPoints);
                    for (int i = 0; i < registers.Length; i++)
                    {
                        datastore.InputRegisters[r.StartAddress + i + 1] = registers[i];
                    }
                    break;
                default:
                    break;
            }
        }

        private string GetDataValues(DiscriminatedUnion<ReadOnlyCollection<bool>, ReadOnlyCollection<ushort>> data)
        {
            switch(data.Option)
            {
                case DiscriminatedUnionOption.A:
                    return String.Join(',', data.A);
                case DiscriminatedUnionOption.B:
                    return String.Join(',', data.B);
            }
            return String.Empty;
        }
    }
}

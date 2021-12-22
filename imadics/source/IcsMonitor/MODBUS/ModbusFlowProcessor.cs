using IcsMonitor.Flows;
using Kaitai;
using PacketDotNet;
using System;
using Traffix.Core.Flows;
using Traffix.Core.Observable;
using Traffix.Extensions.Decoders.Industrial;

namespace IcsMonitor.Modbus
{


    /// <summary>
    /// Flow processor for extracting MODBUS related information from bidirectional flows.
    /// </summary>
    class ModbusFlowProcessor<TKey> : FlowProcessor<PacketRecord<Packet>, FlowKey, FlowRecord<TKey, ModbusCompact>>
    {
        private readonly Func<FlowKey, TKey> getKey;

        /// <summary>
        /// Creates the flow processor of the given index.
        /// </summary>
        /// <param name="index">The identification of the flow processor.</param>
        public ModbusFlowProcessor(string label, DateTime start, TimeSpan duration, Func<FlowKey,TKey> getKey)
        {
            Label = label;
            Start = start;
            Duration = duration;
            this.getKey = getKey;
        }

        /// <summary>
        /// A label assigned to the processor. Can be used to uniquely identify the processor among a list of processors.. 
        /// </summary>
        public string Label { get; }
        /// <summary>
        /// Defines the timestamp for the first samples processes by the processor.
        /// </summary>
        public DateTime Start { get; }
        /// <summary>
        /// Defines the duration/interval for which the processor accepts the samples.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <inheritdoc/>
        protected override FlowRecord<TKey, ModbusCompact> Aggregate(FlowRecord<TKey,ModbusCompact> arg1, FlowRecord<TKey,ModbusCompact> arg2)
        {
            var newRecord = new FlowRecord<TKey, ModbusCompact>
            {
                WindowLabel = Label,
                WindowStart = Start,
                WindowDuration = Duration,
                FlowLabel = AggregateFlowLabel(arg1.FlowLabel, arg2.FlowLabel),
                FlowKey = arg1.FlowKey,
                ForwardMetrics = FlowMetrics.Aggregate(arg1.ForwardMetrics, arg2.ForwardMetrics),
                ReverseMetrics = FlowMetrics.Aggregate(arg1.ReverseMetrics, arg2.ReverseMetrics),
                Data = ModbusCompact.Aggregate(arg1.Data, arg2.Data)
            };
            return newRecord;
        }

        /// <inheritdoc/>
        protected override void Update(FlowRecord<TKey,ModbusCompact> record, PacketRecord<Packet> packet)
        { 
            var flowMetricsFromPacket = new FlowMetrics(1, packet.Packet.TotalPacketLength, packet.Ticks, packet.Ticks);
            var modbus = new ModbusRawData();
            if (packet.Key.SourcePort > packet.Key.DestinationPort)
            {

                UpdateRequest(ref modbus, packet);
                record.ForwardMetrics = FlowMetrics.Aggregate(record.ForwardMetrics, flowMetricsFromPacket);
            }
            else
            {
                UpdateResponse(ref modbus, packet);
                record.ReverseMetrics = FlowMetrics.Aggregate(record.ReverseMetrics, flowMetricsFromPacket);
            }
            record.Data = ModbusCompact.Aggregate(record.Data, new ModbusCompact(ref modbus));
        }

        /// <inheritdoc/>
        protected override FlowRecord<TKey,ModbusCompact> Create(PacketRecord<Packet> arg)
        {

            var modbus = new ModbusRawData();
            FlowMetrics forwardMetrics = null;
            FlowMetrics reverseMetrics = null;
            var flowMetricsFromPacket = new FlowMetrics(1, arg.Packet.TotalPacketLength, arg.Ticks, arg.Ticks);
            if (arg.Key.SourcePort > arg.Key.DestinationPort)
            {
                UpdateRequest(ref modbus, arg);
                forwardMetrics = flowMetricsFromPacket;
            }
            else
            {
                UpdateResponse(ref modbus, arg);
                reverseMetrics = flowMetricsFromPacket;
            }
            var record = new FlowRecord<TKey,ModbusCompact>
            {
                WindowLabel = Label.ToString(),
                FlowLabel = arg.Label,
                FlowKey = getKey(arg.Key),
                ForwardMetrics = forwardMetrics,
                ReverseMetrics = reverseMetrics,
                Data = new ModbusCompact(ref modbus)
            };
            return record;
        }

        private static string AggregateFlowLabel(string x, string y)
        {
            Int32.TryParse(x, out var xn);
            Int32.TryParse(y, out var yn);
            return (xn + yn).ToString();
        }
        #region Packet update methods
        private static void UpdateRequest(ref ModbusRawData request, PacketRecord<Packet> arg)
        {
            var tcpPacket = arg.Packet.Extract<TcpPacket>();
            if (tcpPacket?.PayloadData?.Length >= 8)
            {
                var stream = new KaitaiStream(tcpPacket.PayloadData);
                if (TryParseModbusRequestPacket(stream, out var modbusPacket, out _))
                {
                    switch (modbusPacket.Function)
                    {
                        case ModbusRequestPacket.ModbusFunctionCode.Diagnostic:
                            request.DiagnosticFunctionsRequests++;
                            break;
                        case ModbusRequestPacket.ModbusFunctionCode.GetComEventCounter:
                            request.OtherFunctionsRequests++;
                            break;
                        case ModbusRequestPacket.ModbusFunctionCode.GetComEventLog:
                            request.OtherFunctionsRequests++;
                            break;
                        case ModbusRequestPacket.ModbusFunctionCode.ReadDeviceIdentification:
                            request.OtherFunctionsRequests++;
                            break;
                        case ModbusRequestPacket.ModbusFunctionCode.MaskWriteRegister:
                            request.MaskWriteRegisterRequests++;
                            break;
                        case ModbusRequestPacket.ModbusFunctionCode.ReadCoilStatus:
                            request.ReadCoilsRequests++;
                            request.ReadCoilsBits += (modbusPacket.Data as ModbusRequestPacket.ReadCoilStatusFunction)?.QuantityOfCoils ?? 0;
                            break;
                        case ModbusRequestPacket.ModbusFunctionCode.ReadExceptionStatus:
                            request.OtherFunctionsRequests++;
                            break;
                        case ModbusRequestPacket.ModbusFunctionCode.ReadFifoQueue:
                            request.ReadFifoRequests++;
                            break;
                        case ModbusRequestPacket.ModbusFunctionCode.ReadFileRecord:
                            request.ReadFileRecordRequests++;
                            request.ReadFileRecordCount += (modbusPacket.Data as ModbusRequestPacket.ReadFileRecordFunction)?.SubRequests.Count ?? 0;
                            break;
                        case ModbusRequestPacket.ModbusFunctionCode.ReadHoldingRegister:
                            request.ReadHoldingRegistersRequests++;
                            request.ReadHoldingRegistersWords += (modbusPacket.Data as ModbusRequestPacket.ReadHoldingRegistersFunction)?.QuantityOfInputs ?? 0;
                            break;
                        case ModbusRequestPacket.ModbusFunctionCode.ReadInputRegisters:
                            request.ReadInputRegistersRequests++;
                            request.ReadInputRegistersWords += (modbusPacket.Data as ModbusRequestPacket.ReadInputRegistersFunction)?.QuantityOfInputs ?? 0;
                            break;
                        case ModbusRequestPacket.ModbusFunctionCode.ReadInputStatus:
                            request.ReadDiscreteInputsRequests++;
                            request.ReadDiscreteInputsBits += (modbusPacket.Data as ModbusRequestPacket.ReadInputStatusFunction)?.QuantityOfInputs ?? 0;
                            break;
                        case ModbusRequestPacket.ModbusFunctionCode.ReadWriteMultiupleRegisters:
                            request.ReadWriteMultRegistersRequests++;
                            request.ReadWriteMultRegistersReadWords += (modbusPacket.Data as ModbusRequestPacket.ReadWriteMultiupleRegistersFunction)?.QuantityToRead ?? 0;
                            request.ReadWriteMultRegistersWriteWords += (modbusPacket.Data as ModbusRequestPacket.ReadWriteMultiupleRegistersFunction)?.QunatityToWrite ?? 0;
                            break;
                        case ModbusRequestPacket.ModbusFunctionCode.ReportSlaveId:
                            request.OtherFunctionsRequests++;
                            break;
                        case ModbusRequestPacket.ModbusFunctionCode.WriteFileRecord:
                            request.WriteFileRecordRequests++;
                            request.WriteFileRecordCount += (modbusPacket.Data as ModbusRequestPacket.WriteFileRecordFunction)?.SubRequests.Count ?? 0;
                            break;
                        case ModbusRequestPacket.ModbusFunctionCode.WriteMultipleCoils:
                            request.WriteMultCoilsRequests++;
                            request.WriteMultCoilsBits += (modbusPacket.Data as ModbusRequestPacket.WriteMultipleCoilsFunction)?.QuantityOfOutputs ?? 0;
                            break;
                        case ModbusRequestPacket.ModbusFunctionCode.WriteMultipleRegisters:
                            request.WriteMultRegistersRequests++;
                            request.WriteMultRegistersWords += (modbusPacket.Data as ModbusRequestPacket.WriteMultipleRegistersFunction)?.QuantityOfRegisters ?? 0;
                            break;
                        case ModbusRequestPacket.ModbusFunctionCode.WriteSingleCoil:
                            request.WriteSingleCoilRequests++;
                            break;
                        case ModbusRequestPacket.ModbusFunctionCode.WriteSingleRegister:
                            request.WriteSingleRegisterRequests++;
                            break;
                        default:
                            request.UndefinedFunctionsRequests++;
                            break;
                    }
                }
                else
                {
                    request.MalformedRequests++;
                }
            }
        }
        private static void UpdateResponse(ref ModbusRawData response, PacketRecord<Packet> arg)
        {
            var tcpPacket = arg.Packet.Extract<TcpPacket>();
            if (tcpPacket?.PayloadData?.Length >= 8)
            {
                var stream = new KaitaiStream(tcpPacket.PayloadData);
                if (TryParseModbusResponsePacket(stream, out var modbusPacket, out _))
                {
                    if (modbusPacket.Status == ModbusResponsePacket.ModbusStatusCode.Success)
                    {
                        switch (modbusPacket.Function)
                        {
                            case ModbusResponsePacket.ModbusFunctionCode.Diagnostic:
                                response.DiagnosticFunctionsResponsesSuccess++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.GetComEventCounter:
                                response.OtherFunctionsResponsesSuccess++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.GetComEventLog:
                                response.OtherFunctionsResponsesSuccess++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.MaskWriteRegister:
                                response.MaskWriteRegisterResponsesSuccess++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.ReadCoilStatus:
                                response.ReadCoilsResponsesSuccess++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.ReadDeviceIdentification:
                                response.OtherFunctionsResponsesSuccess++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.ReadExceptionStatus:
                                response.OtherFunctionsResponsesSuccess++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.ReadFifoQueue:
                                response.ReadFifoResponsesSuccess++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.ReadFileRecord:
                                response.ReadFileRecordResponsesSuccess++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.ReadHoldingRegister:
                                response.ReadHoldingRegistersResponsesSuccess++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.ReadInputRegisters:
                                response.ReadInputRegistersResponsesSuccess++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.ReadInputStatus:
                                response.ReadDiscreteInputsResponsesSuccess++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.ReadWriteMultiupleRegisters:
                                response.ReadWriteMultRegistersResponsesSuccess++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.ReportSlaveId:
                                response.OtherFunctionsResponsesSuccess++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.WriteFileRecord:
                                response.WriteFileRecordResponsesSuccess++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.WriteMultipleCoils:
                                response.WriteMultCoilsResponsesSuccess++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.WriteMultipleRegisters:
                                response.WriteMultRegistersResponsesSuccess++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.WriteSingleCoil:
                                response.WriteSingleCoilResponsesSuccess++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.WriteSingleRegister:
                                response.WriteSingleRegisterResponsesSuccess++;
                                break;
                            default:
                                response.UndefinedFunctionsResponsesSuccess++;
                                break;
                        }
                    }
                    else
                    {
                        switch (modbusPacket.Function)
                        {
                            case ModbusResponsePacket.ModbusFunctionCode.Diagnostic:
                                response.DiagnosticFunctionsResponsesError++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.GetComEventCounter:
                                response.OtherFunctionsResponsesError++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.GetComEventLog:
                                response.OtherFunctionsResponsesError++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.MaskWriteRegister:
                                response.MaskWriteRegisterResponsesError++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.ReadCoilStatus:
                                response.ReadCoilsResponsesError++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.ReadDeviceIdentification:
                                response.OtherFunctionsResponsesError++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.ReadExceptionStatus:
                                response.OtherFunctionsResponsesError++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.ReadFifoQueue:
                                response.ReadFifoResponsesError++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.ReadFileRecord:
                                response.ReadFileRecordResponsesError++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.ReadHoldingRegister:
                                response.ReadHoldingRegistersResponsesError++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.ReadInputRegisters:
                                response.ReadInputRegistersResponsesError++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.ReadInputStatus:
                                response.ReadDiscreteInputsResponsesError++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.ReadWriteMultiupleRegisters:
                                response.ReadWriteMultRegistersResponsesError++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.ReportSlaveId:
                                response.OtherFunctionsResponsesError++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.WriteFileRecord:
                                response.WriteFileRecordResponsesError++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.WriteMultipleCoils:
                                response.WriteMultCoilsResponsesError++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.WriteMultipleRegisters:
                                response.WriteMultRegistersResponsesError++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.WriteSingleCoil:
                                response.WriteSingleCoilResponsesError++;
                                break;
                            case ModbusResponsePacket.ModbusFunctionCode.WriteSingleRegister:
                                response.WriteSingleRegisterResponsesError++;
                                break;
                            default:
                                response.UndefinedFunctionsResponsesError++;
                                break;
                        }
                    }
                }
            }
        }
        private static bool TryParseModbusRequestPacket(KaitaiStream stream, out ModbusRequestPacket packet, out Exception exception)
        {
            try
            {
                packet = new ModbusRequestPacket(stream);
                exception = null;
                return true;
            }
            catch (Exception e)
            {
                packet = null;
                exception = e;
                return false;
            }
        }
        private static bool TryParseModbusResponsePacket(KaitaiStream stream, out ModbusResponsePacket packet, out Exception exception)
        {
            try
            {
                packet = new ModbusResponsePacket(stream);
                exception = null;
                return true;
            }
            catch (Exception e)
            {
                packet = null;
                exception = e;
                return false;
            }
        }
        #endregion

        protected override FlowKey GetFlowKey(PacketRecord<Packet> source)
        {
            return source.Key;
        }
    }
}

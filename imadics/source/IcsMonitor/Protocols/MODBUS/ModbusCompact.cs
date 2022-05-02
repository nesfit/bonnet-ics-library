using MessagePack;

namespace IcsMonitor.Modbus
{
    /// <summary>
    /// A compact version that only counts number of operations of each operation type.
    /// </summary>
    public class ModbusCompact
    {
        ModbusRawData _data;
        /// <summary>
        /// Creates a new instance based in raw <paramref name="data"/>.
        /// </summary>
        /// <param name="data">Raw modbus data record.</param>
        public ModbusCompact(ref ModbusRawData data)
        {
            _data = data;
        }
        /// <summary>
        /// Aggregates two object into a new one.
        /// </summary>
        /// <param name="x">The first object.</param>
        /// <param name="y">The second object.</param>
        /// <returns>Aggregated modbus object.</returns>
        public static ModbusCompact Aggregate(ModbusCompact x, ModbusCompact y)
        {
            var aggregated = new ModbusRawData();
            ModbusRawData.Aggregate(ref x._data, ref y._data, ref aggregated);
            return new ModbusCompact(ref aggregated);
        }

        [Key("MODBUS_UNIT_ID")]
        public byte UnitId => _data.UnitId;

        [Key("MODBUS_READ_REQUESTS")]
        public int ReadRequests =>
              _data.ReadCoilsRequests
            + _data.ReadDiscreteInputsRequests
            + _data.ReadFifoRequests
            + _data.ReadFileRecordRequests
            + _data.ReadHoldingRegistersRequests
            + _data.ReadInputRegistersRequests;

        [Key("MODBUS_WRITE_REQUESTS")]
        public int WriteRequests =>
              _data.WriteFileRecordRequests
            + _data.WriteMultCoilsRequests
            + _data.WriteMultRegistersRequests
            + _data.WriteSingleCoilRequests
            + _data.WriteSingleRegisterRequests
            + _data.MaskWriteRegisterRequests
            + _data.ReadWriteMultRegistersRequests;

        [Key("MODBUS_DIAGNOSTIC_REQUESTS")]
        public int DiagnosticRequests => _data.DiagnosticFunctionsRequests;

        [Key("MODBUS_OTHER_REQUESTS")]
        public int OtherRequests => _data.OtherFunctionsRequests;

        [Key("MODBUS_UNDEFINED_REQUESTS")]
        public int UndefinedRequests => _data.UndefinedFunctionsRequests;

        [Key("MODBUS_RESPONSES_SUCCESS")]
        public int ResponsesSuccess =>
               _data.DiagnosticFunctionsResponsesSuccess
            + _data.MaskWriteRegisterResponsesSuccess
            + _data.OtherFunctionsResponsesSuccess
            + _data.ReadCoilsResponsesSuccess
            + _data.ReadDiscreteInputsResponsesSuccess
            + _data.ReadFifoResponsesSuccess
            + _data.ReadFileRecordResponsesSuccess
            + _data.ReadHoldingRegistersResponsesSuccess
            + _data.ReadInputRegistersResponsesSuccess
            + _data.ReadWriteMultRegistersResponsesSuccess
            + _data.UndefinedFunctionsResponsesSuccess
            + _data.WriteFileRecordResponsesSuccess
            + _data.WriteMultCoilsResponsesSuccess
            + _data.WriteMultRegistersResponsesSuccess
            + _data.WriteSingleCoilResponsesSuccess
            + _data.WriteSingleRegisterResponsesSuccess;

        [Key("MODBUS_RESPONSES_ERROR")]
        public int ResponsesError =>
              _data.DiagnosticFunctionsResponsesError
            + _data.MaskWriteRegisterResponsesError
            + _data.OtherFunctionsResponsesError
            + _data.ReadCoilsResponsesError
            + _data.ReadDiscreteInputsResponsesError
            + _data.ReadFifoResponsesError
            + _data.ReadFileRecordResponsesError
            + _data.ReadHoldingRegistersResponsesError
            + _data.ReadInputRegistersResponsesError
            + _data.ReadWriteMultRegistersResponsesError
            + _data.UndefinedFunctionsResponsesError
            + _data.WriteFileRecordResponsesError
            + _data.WriteMultCoilsResponsesError
            + _data.WriteMultRegistersResponsesError
            + _data.WriteSingleCoilResponsesError
            + _data.WriteSingleRegisterResponsesError;

        [Key("MODBUS_MALFORMED_REQUESTS")]
        public int MalformedRequests => _data.MalformedRequests;

        [Key("MODBUS_MALFORMED_RESPONSES")]
        public int MalformedResponses => _data.MalformedResponses;
    }
}
using MessagePack;

namespace IcsMonitor.Modbus
{
    /// <summary>
    /// A full version of the MODBUS flow record.
    /// </summary>
    [MessagePackObject]
    public struct ModbusRawData
    {
        #region REQUESTS
        [Key("MODBUS_UNIT_ID")]
        public byte UnitId;
        [Key("MODBUS_READ_COILS_REQUESTS")]
        public int ReadCoilsRequests;
        [Key("MODBUS_READ_COILS_BITS")]
        public int ReadCoilsBits;
        [Key("MODBUS_READ_DISCRETE_INPUTS_REQUESTS")]
        public int ReadDiscreteInputsRequests;
        [Key("MODBUS_READ_DISCRETE_INPUTS_BITS")]
        public int ReadDiscreteInputsBits;
        [Key("MODBUS_READ_INPUT_REGISTERS_REQUESTS")]
        public int ReadInputRegistersRequests;
        [Key("MODBUS_READ_INPUT_REGISTERS_WORDS")]
        public int ReadInputRegistersWords;
        [Key("MODBUS_READ_HOLDING_REGISTERS_REQUESTS")]
        public int ReadHoldingRegistersRequests;
        [Key("MODBUS_READ_HOLDING_REGISTERS_WORDS")]
        public int ReadHoldingRegistersWords;
        [Key("MODBUS_WRITE_SINGLE_COIL_REQUESTS")]
        public int WriteSingleCoilRequests;
        [Key("MODBUS_WRITE_SINGLE_REGISTER_REQUESTS")]
        public int WriteSingleRegisterRequests;
        [Key("MODBUS_WRITE_MULT_COILS_REQUESTS")]
        public int WriteMultCoilsRequests;
        [Key("MODBUS_WRITE_MULT_COILS_BITS")]
        public int WriteMultCoilsBits;
        [Key("MODBUS_WRITE_MULT_REGISTERS_REQUESTS")]
        public int WriteMultRegistersRequests;
        [Key("MODBUS_WRITE_MULT_REGISTERS_WORDS")]
        public int WriteMultRegistersWords;
        [Key("MODBUS_READ_FILE_RECORD_REQUESTS")]
        public int ReadFileRecordRequests;
        [Key("MODBUS_READ_FILE_RECORD_COUNT")]
        public int ReadFileRecordCount;
        [Key("MODBUS_WRITE_FILE_RECORD_REQUESTS")]
        public int WriteFileRecordRequests;
        [Key("MODBUS_WRTIE_FILE_RECORD_COUNT")]
        public int WriteFileRecordCount;
        [Key("MODBUS_MASK_WRITE_REGISTER_REQUESTS")]
        public int MaskWriteRegisterRequests;
        [Key("MODBUS_READ_WRITE_MULT_REGISTERS_REQUESTS")]
        public int ReadWriteMultRegistersRequests;
        [Key("MODBUS_READ_WRITE_MULT_REGISTERS_READ_WORDS")]
        public int ReadWriteMultRegistersReadWords;
        [Key("MODBUS_READ_WRITE_MULT_REGISTERS_WRITE_WORDS")]
        public int ReadWriteMultRegistersWriteWords;
        [Key("MODBUS_READ_FIFO_REQUESTS")]
        public int ReadFifoRequests;
        [Key("MODBUS_DIAGNOSTIC_REQUESTS")]
        public int DiagnosticFunctionsRequests;
        [Key("MODBUS_OTHER_REQUESTS")]
        public int OtherFunctionsRequests;
        [Key("MODBUS_UNDEFINED_REQUESTS")]
        public int UndefinedFunctionsRequests;
        #endregion

        #region RESPONSES - SUCCESS
        [Key("MODBUS_READ_COILS_RESPONSES_SUCCESS")]
        public int ReadCoilsResponsesSuccess;
        [Key("MODBUS_READ_DISCRETE_INPUTS_RESPONSES_SUCCESS")]
        public int ReadDiscreteInputsResponsesSuccess;
        [Key("MODBUS_READ_INPUT_REGISTERS_RESPONSES_SUCCESS")]
        public int ReadInputRegistersResponsesSuccess;
        [Key("MODBUS_READ_HOLDING_REGISTERS_RESPONSES_SUCCESS")]
        public int ReadHoldingRegistersResponsesSuccess;
        [Key("MODBUS_WRITE_SINGLE_COIL_RESPONSES_SUCCESS")]
        public int WriteSingleCoilResponsesSuccess;
        [Key("MODBUS_WRITE_SINGLE_REGISTER_RESPONSES_SUCCESS")]
        public int WriteSingleRegisterResponsesSuccess;
        [Key("MODBUS_WRITE_MULT_COILS_RESPONSES_SUCCESS")]
        public int WriteMultCoilsResponsesSuccess;
        [Key("MODBUS_WRITE_MULT_REGISTERS_RESPONSES_SUCCESS")]
        public int WriteMultRegistersResponsesSuccess;
        [Key("MODBUS_READ_FILE_RECORD_RESPONSES_SUCCESS")]
        public int ReadFileRecordResponsesSuccess;
        [Key("MODBUS_WRITE_FILE_RECORD_RESPONSES_SUCCESS")]
        public int WriteFileRecordResponsesSuccess;
        [Key("MODBUS_MASK_WRITE_REGISTER_RESPONSES_SUCCESS")]
        public int MaskWriteRegisterResponsesSuccess;
        [Key("MODBUS_READ_WRITE_MULT_REGISTERS_RESPONSES_SUCCESS")]
        public int ReadWriteMultRegistersResponsesSuccess;
        [Key("MODBUS_READ_FIFO_RESPONSES_SUCCESS")]
        public int ReadFifoResponsesSuccess;
        [Key("MODBUS_DIAGNOSTIC_RESPONSES_SUCCESS")]
        public int DiagnosticFunctionsResponsesSuccess;
        [Key("MODBUS_OTHER_RESPONSES_SUCCESS")]
        public int OtherFunctionsResponsesSuccess;
        [Key("MODBUS_UNDEFINED_RESPONSES_SUCCESS")]
        public int UndefinedFunctionsResponsesSuccess;

        #endregion

        #region RESPONSES - ERROR

        [Key("MODBUS_READ_COILS_RESPONSES_ERROR")]
        public int ReadCoilsResponsesError;
        [Key("MODBUS_READ_DISCRETE_INPUTS_RESPONSES_ERROR")]
        public int ReadDiscreteInputsResponsesError;
        [Key("MODBUS_READ_INPUT_REGISTERS_RESPONSES_ERROR")]
        public int ReadInputRegistersResponsesError;
        [Key("MODBUS_READ_HOLDING_REGISTERS_RESPONSES_ERROR")]
        public int ReadHoldingRegistersResponsesError;
        [Key("MODBUS_WRITE_SINGLE_COIL_RESPONSES_ERROR")]
        public int WriteSingleCoilResponsesError;
        [Key("MODBUS_WRITE_SINGLE_REGISTER_RESPONSES_ERROR")]
        public int WriteSingleRegisterResponsesError;

        [Key("MODBUS_WRITE_MULT_COILS_RESPONSES_ERROR")]
        public int WriteMultCoilsResponsesError;

        [Key("MODBUS_WRITE_MULT_REGISTERS_RESPONSES_ERROR")]
        public int WriteMultRegistersResponsesError;

        [Key("MODBUS_READ_FILE_RECORD_RESPONSES_ERROR")]
        public int ReadFileRecordResponsesError;

        [Key("MODBUS_WRITE_FILE_RECORD_RESPONSES_ERROR")]
        public int WriteFileRecordResponsesError;

        [Key("MODBUS_MASK_WRITE_REGISTER_RESPONSES_ERROR")]
        public int MaskWriteRegisterResponsesError;

        [Key("MODBUS_READ_WRITE_MULT_REGISTERS_RESPONSES_ERROR")]
        public int ReadWriteMultRegistersResponsesError;

        [Key("MODBUS_READ_FIFO_RESPONSES_ERROR")]
        public int ReadFifoResponsesError;

        [Key("MODBUS_DIAGNOSTIC_RESPONSES_ERROR")]
        public int DiagnosticFunctionsResponsesError;

        [Key("MODBUS_OTHER_RESPONSES_ERROR")]
        public int OtherFunctionsResponsesError;

        [Key("MODBUS_UNDEFINED_RESPONSES_ERROR")]
        public int UndefinedFunctionsResponsesError;
        #endregion

        #region Malformed messages

        [Key("MODBUS_MALFORMED_REQUESTS")]
        public int MalformedRequests;

        [Key("MODBUS_MALFORMED_RESPONSES")]
        public int MalformedResponses;

        #endregion

        #region Aggregate function
        public static void Aggregate(ref ModbusRawData x, ref ModbusRawData y, ref ModbusRawData z)
        {
            z.UnitId = x.UnitId;
            z.DiagnosticFunctionsRequests = x.DiagnosticFunctionsRequests + y.DiagnosticFunctionsRequests;
            z.DiagnosticFunctionsResponsesError = x.DiagnosticFunctionsResponsesError + y.DiagnosticFunctionsResponsesError;
            z.DiagnosticFunctionsResponsesSuccess = x.DiagnosticFunctionsResponsesSuccess + y.DiagnosticFunctionsResponsesSuccess;
            z.MalformedRequests = x.MalformedRequests + y.MalformedRequests;
            z.MalformedResponses = x.MalformedResponses + y.MalformedResponses;
            z.MaskWriteRegisterRequests = x.MaskWriteRegisterRequests + y.MaskWriteRegisterRequests;
            z.MaskWriteRegisterResponsesError = x.MaskWriteRegisterResponsesError + y.MaskWriteRegisterResponsesError;
            z.MaskWriteRegisterResponsesSuccess = x.MaskWriteRegisterResponsesSuccess + y.MaskWriteRegisterResponsesSuccess;
            z.OtherFunctionsRequests = x.OtherFunctionsRequests + y.OtherFunctionsRequests;
            z.OtherFunctionsResponsesError = x.OtherFunctionsResponsesError + y.OtherFunctionsResponsesError;
            z.OtherFunctionsResponsesSuccess = x.OtherFunctionsResponsesSuccess + y.OtherFunctionsResponsesSuccess;
            z.ReadCoilsBits = x.ReadCoilsBits + y.ReadCoilsBits;
            z.ReadCoilsRequests = x.ReadCoilsRequests + y.ReadCoilsRequests;
            z.ReadCoilsResponsesError = x.ReadCoilsResponsesError + y.ReadCoilsResponsesError;
            z.ReadCoilsResponsesSuccess = x.ReadCoilsResponsesSuccess + y.ReadCoilsResponsesSuccess;
            z.ReadDiscreteInputsBits = x.ReadDiscreteInputsBits + y.ReadDiscreteInputsBits;
            z.ReadDiscreteInputsRequests = x.ReadDiscreteInputsRequests + y.ReadDiscreteInputsRequests;
            z.ReadDiscreteInputsResponsesError = x.ReadDiscreteInputsResponsesError + y.ReadDiscreteInputsResponsesError;
            z.ReadDiscreteInputsResponsesSuccess = x.ReadDiscreteInputsResponsesSuccess + y.ReadDiscreteInputsResponsesSuccess;
            z.ReadFifoRequests = x.ReadFifoRequests + y.ReadFifoRequests;
            z.ReadFifoResponsesError = x.ReadFifoResponsesError + y.ReadFifoResponsesError;
            z.ReadFifoResponsesSuccess = x.ReadFifoResponsesSuccess + y.ReadFifoResponsesSuccess;
            z.ReadFileRecordCount = x.ReadFileRecordCount + y.ReadFileRecordCount;
            z.ReadFileRecordRequests = x.ReadFileRecordRequests + y.ReadFileRecordRequests;
            z.ReadFileRecordResponsesError = x.ReadFileRecordResponsesError + y.ReadFileRecordResponsesError;
            z.ReadFileRecordResponsesSuccess = x.ReadFileRecordResponsesSuccess + y.ReadFileRecordResponsesSuccess;
            z.ReadHoldingRegistersRequests = x.ReadHoldingRegistersRequests + y.ReadHoldingRegistersRequests;
            z.ReadHoldingRegistersResponsesError = x.ReadHoldingRegistersResponsesError + y.ReadHoldingRegistersResponsesError;
            z.ReadHoldingRegistersResponsesSuccess = x.ReadHoldingRegistersResponsesSuccess + y.ReadHoldingRegistersResponsesSuccess;
            z.ReadHoldingRegistersWords = x.ReadHoldingRegistersWords + y.ReadHoldingRegistersWords;
            z.ReadInputRegistersRequests = x.ReadInputRegistersRequests + y.ReadInputRegistersRequests;
            z.ReadInputRegistersResponsesError = x.ReadInputRegistersResponsesError + y.ReadInputRegistersResponsesError;
            z.ReadInputRegistersResponsesSuccess = x.ReadInputRegistersResponsesSuccess + y.ReadInputRegistersResponsesSuccess;
            z.ReadInputRegistersWords = x.ReadInputRegistersWords + y.ReadInputRegistersWords;
            z.ReadWriteMultRegistersReadWords = x.ReadWriteMultRegistersReadWords + y.ReadWriteMultRegistersReadWords;
            z.ReadWriteMultRegistersRequests = x.ReadWriteMultRegistersRequests + y.ReadWriteMultRegistersRequests;
            z.ReadWriteMultRegistersResponsesError = x.ReadWriteMultRegistersResponsesError + y.ReadWriteMultRegistersResponsesError;
            z.ReadWriteMultRegistersResponsesSuccess = x.ReadWriteMultRegistersResponsesSuccess + y.ReadWriteMultRegistersResponsesSuccess;
            z.ReadWriteMultRegistersWriteWords = x.ReadWriteMultRegistersWriteWords + y.ReadWriteMultRegistersWriteWords;
            z.UndefinedFunctionsRequests = x.UndefinedFunctionsRequests + y.UndefinedFunctionsRequests;
            z.UndefinedFunctionsResponsesError = x.UndefinedFunctionsResponsesError + y.UndefinedFunctionsResponsesError;
            z.UndefinedFunctionsResponsesSuccess = x.UndefinedFunctionsResponsesSuccess + y.UndefinedFunctionsResponsesSuccess;
            z.WriteFileRecordCount = x.WriteFileRecordCount + y.WriteFileRecordCount;
            z.WriteFileRecordRequests = x.WriteFileRecordRequests + y.WriteFileRecordRequests;
            z.WriteFileRecordResponsesError = x.WriteFileRecordResponsesError + y.WriteFileRecordResponsesError;
            z.WriteFileRecordResponsesSuccess = x.WriteFileRecordResponsesSuccess + y.WriteFileRecordResponsesSuccess;
            z.WriteMultCoilsBits = x.WriteMultCoilsBits + y.WriteMultCoilsBits;
            z.WriteMultCoilsRequests = x.WriteMultCoilsRequests + y.WriteMultCoilsRequests;
            z.WriteMultCoilsResponsesError = x.WriteMultCoilsResponsesError + y.WriteMultCoilsResponsesError;
            z.WriteMultCoilsResponsesSuccess = x.WriteMultCoilsResponsesSuccess + y.WriteMultCoilsResponsesSuccess;
            z.WriteMultRegistersRequests = x.WriteMultRegistersRequests + y.WriteMultRegistersRequests;
            z.WriteMultRegistersResponsesError = x.WriteMultRegistersResponsesError + y.WriteMultRegistersResponsesError;
            z.WriteMultRegistersResponsesSuccess = x.WriteMultRegistersResponsesSuccess + y.WriteMultRegistersResponsesSuccess;
            z.WriteMultRegistersWords = x.WriteMultRegistersWords + y.WriteMultRegistersWords;
            z.WriteSingleCoilRequests = x.WriteSingleCoilRequests + y.WriteSingleCoilRequests;
            z.WriteSingleCoilResponsesError = x.WriteSingleCoilResponsesError + y.WriteSingleCoilResponsesError;
            z.WriteSingleCoilResponsesSuccess = x.WriteSingleCoilResponsesSuccess + y.WriteSingleCoilResponsesSuccess;
            z.WriteSingleRegisterRequests = x.WriteSingleRegisterRequests + y.WriteSingleRegisterRequests;
            z.WriteSingleRegisterResponsesError = x.WriteSingleRegisterResponsesError + y.WriteSingleRegisterResponsesError;
            z.WriteSingleRegisterResponsesSuccess = x.WriteSingleRegisterResponsesSuccess + y.WriteSingleRegisterResponsesSuccess;
        }

        #endregion

    }
}
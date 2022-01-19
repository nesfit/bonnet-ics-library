using Modbus.Data;
using Modbus.Message;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace AnomalyInjector
{
    /// <summary>
    ///     Class holding all implementation shared between two or more message types.
    ///     Interfaces expose subsets of type specific implementations.
    /// </summary>
    internal class CustomModbusMessage : IModbusMessage
    {
        public CustomModbusMessage()
        {
        }

        public ushort TransactionId { get; set; }

        public byte FunctionCode { get => MessageFrame[1]; set { MessageFrame[1] = value; } }

        public ushort? NumberOfPoints { get; set; }

        public byte SlaveAddress { get => MessageFrame[0]; set { MessageFrame[0] = value; } }

        public byte[] MessageFrame { get; private set; }

        public byte[] ProtocolDataUnit =>
            MessageFrame.Skip(1).ToArray();

        public const int MinimumFrameSize = 2;
        public void Initialize(byte[] frame)
        {
            if (frame == null)
            {
                throw new ArgumentNullException(nameof(frame), "Argument frame cannot be null.");
            }

            if (frame.Length < MinimumFrameSize)
            {
                string msg = $"Message frame must contain at least {MinimumFrameSize} bytes of data.";
                throw new FormatException(msg);
            }

            MessageFrame = frame;
        }
    }
}

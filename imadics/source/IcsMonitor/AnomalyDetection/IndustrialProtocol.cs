namespace IcsMonitor.AnomalyDetection
{
    /// <summary>
    /// A collection of currently supported industrial protocols.
    /// </summary>
    public enum IndustrialProtocol {
        /// <summary>
        /// MODBUS ICS protocol.
        /// <para/>
        /// <seealso cref="https://en.wikipedia.org/wiki/Modbus"/>
        /// </summary>
        Modbus,
        /// <summary>
        /// IEC104 ICS protocol.
        /// <para/>
        /// <seealso cref="https://en.wikipedia.org/wiki/IEC_60870-5"/>
        /// </summary>
        Iec,
        /// <summary>
        /// GOOSE ICS protocol.
        /// <para/>
        /// <seealso cref="https://en.wikipedia.org/wiki/Generic_Substation_Events"/>
        /// </summary>
        Goose
    }
}

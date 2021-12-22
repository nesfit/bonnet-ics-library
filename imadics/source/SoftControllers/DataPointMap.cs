namespace SoftControllers
{
    /// <summary>
    /// Extends the RegisterMap class with methods for creating register instances.
    /// </summary>
    public static class DataPointMap
    {
        public static Coil GetCoil(this RegisterMap map, ModbusDevice device, string registerTag)
        {
            return new Coil(device, registerTag, map.GetRegisterAddress(registerTag));
        }
        public static DiscreteInput GetDiscreteInput(this RegisterMap map, ModbusDevice device, string registerTag)
        {
            return new DiscreteInput(device, registerTag, map.GetRegisterAddress(registerTag));
        }
 
        public static HoldingRegister GetHoldingRegister(this RegisterMap map, ModbusDevice device, string registerTag)
        {
            return new HoldingRegister(device, registerTag, map.GetRegisterAddress(registerTag));
        }
        
        public static InputRegister GetInputRegister(this RegisterMap map, ModbusDevice device, string registerTag)
        {
            return new InputRegister(device, registerTag, map.GetRegisterAddress(registerTag));
        }              
    }
}

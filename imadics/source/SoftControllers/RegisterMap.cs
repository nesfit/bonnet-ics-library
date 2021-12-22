using System.Linq;
using System.Collections.Generic;

namespace SoftControllers
{
    /// <summary>
    /// Defines possible register types for Modbus PLC.
    /// </summary>
    public enum RegisterType
    {
        Coil, 
        Input,
        HoldingReg,
        InputReg,
    }
    /// <summary>
    /// Used to store register tags and correspoding addresses.
    /// </summary>
    public class RegisterMap
    {
        Dictionary<string, TagRecord> _tags;
        public RegisterMap(IEnumerable<TagRecord> tags)
        {
            _tags = tags.ToDictionary(x=>x.Name);
        }
        /// <summary>
        /// Gets the register address or throw exception if not found.
        /// </summary>
        /// <param name="tagName">The tag name. </param>
        /// <returns>The register address.</returns>
        public ushort GetRegisterAddress(string tagName)
        {
            if (_tags.TryGetValue(tagName, out var register))
            {
                return register.Address;
            }
            throw new KeyNotFoundException($"The tag name {tagName} not found in the register map.");
        }
        
        /// <summary>
        /// Register tag record.
        /// </summary>
        /// <param name="Name">The register tag name.</param>
        /// <param name="DataType">The data type of the register. It can be Coil, DigitalInput, InputRegister or HoldingRegister.</param>
        /// <param name="Address"The address of the registry within the address space of registers of the given type.</param>
        public record TagRecord(string Name, RegisterType DataType, ushort Address) 
        {
        } 
    }
}

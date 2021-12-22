using System;

namespace SoftControllers
{
    public abstract class DataPoint
    {
        protected readonly ModbusDevice _device;
        protected readonly string _tag;
        protected readonly ushort _address;

        public abstract object GetLastValue();
        protected readonly Type _valueType;

        public ushort Address => _address;

        public string Tag => _tag;

        protected DataPoint(ModbusDevice device, string tag, ushort address, Type valueType)
        {
            _device = device;
            _tag = tag;
            _address = address;
            _valueType = valueType;
        }
    }

    public abstract class DataPoint<T> : DataPoint
    {
        /// <summary>
        /// The scale used to convert float to integer and vice versa.
        /// <para>
        /// Floating sensor values are multiplied by this value; actuator values are divided by it. Using this approach, a floating sensor's value can be converted into an integer, sent to the client and converted back to a real number by dividing it by the scale factor (e.g. an input value of 3.14 with 100 as scale is sent as 314, then it can be divided by the same scale to obtain the real value of 3.14).
        /// </par>
        /// </summary>
        public static int Scale { get; set; } = 100;
        protected T _lastValue;

        public T LastValue { get => _lastValue; }
        public override object GetLastValue() => LastValue;

        protected DataPoint(ModbusDevice device, string registerTag, ushort registerAddress) : base(device, registerTag, registerAddress, typeof(T))
        { }
        protected abstract T GetValue();
        protected abstract void SetValue(T value);

        public T Value
        {
            get
            {
                _lastValue = GetValue();
                return _lastValue;
            }
            set
            {
                _lastValue = value;
                SetValue(value);
            }
        }

        public override string ToString()
        {
            var typ = GetType().Name;
            return $"{typ}({_address}):{_tag} = {_lastValue}";
        }
    }
}

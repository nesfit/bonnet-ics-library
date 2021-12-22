using Microsoft.Extensions.CommandLineUtils;
using System;

namespace IcsMonitor.Utils
{
    /// <summary>
    /// A helper class for processing command line options.
    /// </summary>
    static class OptionHelper
    {
        public delegate bool TryParse<T>(string input, out T value);

        public static bool TryGetValueOrDefault(this CommandOption option, string defaultValue, out string value)
        {
            value = defaultValue;
            if (option.HasValue())
            {
                value = option.Value();
                return true;
            }
            else
            {
                return true;
            }
        }
        public static bool TryGetValueOrError(this CommandOption option, Action OnValueMissingError, out string value)
        {
            value = default;
            if (option.HasValue())
            {
                value = option.Value();
                return true;
            }
            else
            {
                OnValueMissingError();
                return false;
            }
        }
        public static bool TryParseValueOrDefault<TValue>(this CommandOption option, TryParse<TValue> tryParse, TValue defaultValue, Action<string> OnError, out TValue value)
        {
            value = defaultValue;
            if (option.HasValue() && !tryParse(option.Value(), out value))
            {
                OnError(option.Value());
                return false;
            }
            else
            {
                return true;
            }
        }
        public static bool TryParseValueOrError<TValue>(this CommandOption option, TryParse<TValue> tryParse, Action OnValueMissingError, Action<string> OnParseError, out TValue value)
        {

            value = default;
            if (option.HasValue())
            {
                var input = option.Value();
                if (tryParse(input, out value))
                {
                    return true;
                }
                else
                {
                    OnParseError(input);
                    return false;
                }
            }
            else
            {
                OnValueMissingError();
                return false;
            }
        }
    }
}

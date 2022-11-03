using Microsoft.Extensions.CommandLineUtils;
using System;

namespace IcsMonitor.Utils
{
    /// <summary>
    /// A helper class for processing command line options.
    /// </summary>
    static class OptionHelper
    {
        /// <summary>
        /// Defines type for delegates used for safe option parsing.
        /// </summary>
        /// <typeparam name="T">The type of the option.</typeparam>
        /// <param name="input">The input string.</param>
        /// <param name="value"><the parsed value./param>
        /// <returns>true if option was parsed. false for any erro during parsing.</returns>
        public delegate bool TryParse<T>(string input, out T value);

        /// <summary>
        /// Gets an option value or a provided default value.
        /// </summary>
        /// <param name="option">The command option object.</param>
        /// <param name="defaultValue">The defalt value used if option does not have any value.</param>
        /// <param name="value">The output value.</param>
        /// <returns>true if option's value was used.</returns>
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
        /// <summary>
        /// Gets an option value or executes the specified action if value there is not any value.
        /// </summary>
        /// <param name="option">The command option object.</param>
        /// <param name="OnValueMissingError">The action to be executed if not value can be used.</param>
        /// <param name="value">The output value</param>
        /// <returns>true if option's value was used.</returns>
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

        /// <summary>
        /// Gets an option value or a provided default value.
        /// </summary>
        /// <typeparam name="TValue">The type of the output value.</typeparam>
        /// <param name="option">The command option object.</param>
        /// <param name="tryParse">The method used to parse the option.</param>
        /// <param name="defaultValue">The default value to use.</param>
        /// <param name="OnError">The action executed when error ocurred during parsing.</param>
        /// <param name="value">The output value.</param>
        /// <returns>true if option's value was used.</returns>
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
        /// <summary>
        /// Gets an option value or a execute the given action on error. 
        /// </summary>
        /// <typeparam name="TValue">The type of the output value.</typeparam>
        /// <param name="option">The command option object.</param>
        /// <param name="tryParse">The method used to parse the option.</param>
        /// <param name="OnValueMissingError">The action executed when option value is missing.</param>
        /// <param name="OnParseError">The action executed when error ocurred during parsing.</param>
        /// <param name="value">>The output value.</param>
        /// <returns>true if option's value was used.</returns>
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

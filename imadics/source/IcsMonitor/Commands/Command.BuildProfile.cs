using IcsMonitor.AnomalyDetection;
using IcsMonitor.Utils;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Linq;

namespace IcsMonitor
{

    internal sealed partial class ConsoleService
    {
        private void BuidProfileCommand(CommandLineApplication command)
        {
            command.Description = "Create a profile for the given input and profile parameters.";
            command.ExtendedHelpText = "This command monitors the communication on the given network interface, collects flows of the target profile protocol and " +
            "applies the profile.";
            command.HelpOption("-?|-h|--help");

            var inputOption = command.AddInputOptions();

            var anomalyDetectionMethodOption = command.Option("-m|--method <value>",
                $"An anomaly detection method to be used to create the profile. Can be one of {String.Join(", ", Enum.GetNames(typeof(AnomalyDetectionMethod)))}. This option is required.",
                CommandOptionType.SingleValue);

            var protocolTypeOption = command.Option("-p|--protocol-type <value>",
                $"A protocol type. Can be one of {String.Join(", ", Enum.GetNames(typeof(IndustrialProtocol)))}. This option is required.",
                CommandOptionType.SingleValue);

            var windowSizeOption = command.Option("-t|--windowSize <value>",
                $"Size of the window as TimeSpan. Required format is HH:MM:SS. his parameter is used to split input file to windows of the respective size.",
                CommandOptionType.SingleValue);

            var windowCountOption = command.Option("-c|--windowCount <value>",
                $"Number of windows to collect for creating the profile.",
                CommandOptionType.SingleValue);

            var customFeaturesOption = command.Option("-f|--feature-file <value>",
                "List of field names that represents a collection of features to be used as the input for AD method. For instance \"ForwardMetricsDuration,ForwardMetricsPackets,ForwardMetricsOctets\"",
                CommandOptionType.SingleValue);

            var outputFileOption = command.Option("-w|--output-file <value>",
                "An output file name to store the profile. This option is required.",
                CommandOptionType.SingleValue);



            command.OnExecute(async () =>
            {
                if (!anomalyDetectionMethodOption.TryParseValueOrError<AnomalyDetectionMethod>(EnumTryParse, () => Console.Error.WriteLine($"Error: '--{anomalyDetectionMethodOption.LongName}' option is required!"), _ => Console.Error.WriteLine("Input error: Invalid method name!"), out var anomalyDetectionMethod)) return -1;
                if (!protocolTypeOption.TryParseValueOrError<IndustrialProtocol>(EnumTryParse, () => Console.Error.WriteLine("Input error: rotocol type must be specified!"), _ => Console.Error.WriteLine("Input error: Invalid protocol type specified!"), out var protocolType)) return -1;
                if (!windowSizeOption.TryParseValueOrDefault(TimeSpan.TryParse, TimeSpan.FromMinutes(5), _ => Console.Error.WriteLine("Input error: Invalid type span specified. Required format is HH:MM:SS.ff"), out var windowTimeSpan)) return -1;
                if (!windowCountOption.TryParseValueOrDefault(Int32.TryParse, (int)Int16.MaxValue, _ => Console.Error.WriteLine("Input error: Invalid value specified. Required is integer value."), out var windowCount)) return -1;
                if (!outputFileOption.TryGetValueOrError(() => Console.Error.WriteLine($"Error: '--{outputFileOption.LongName}' option is required!"), out var outputProfileFile)) return -1;

                var customFeatures = customFeaturesOption.HasValue() ? customFeaturesOption.Value().Split(',').Select(s => s.Trim()).ToArray() : null;

                if (inputOption.HasFlowFile)
                {
                    switch (anomalyDetectionMethod)
                    {
                        case AnomalyDetectionMethod.Centroids:
                            await _functions.BuildProfileAsync(inputOption.FlowFileValue, protocolType, windowTimeSpan, customFeatures, outputProfileFile);
                            break;
                    }
                }
                else
                {
                    var captureDevice = inputOption.GetCaptureDevice();
                    if (captureDevice == null) throw new CommandParsingException(command, "An input must be specified.");

                    switch (anomalyDetectionMethod)
                    {
                        case AnomalyDetectionMethod.Centroids:
                            await _functions.BuildProfileAsync(captureDevice, protocolType, windowTimeSpan, windowCount, customFeatures, outputProfileFile, _appLifetime.ApplicationStopping);
                            break;
                    }
                }
                return 0;
            });
        }
    }
}

using IcsMonitor.AnomalyDetection;
using IcsMonitor.Utils;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.IO;
using Traffix.DataView;

namespace IcsMonitor
{
    internal sealed partial class ConsoleService
    {
        private void ExportFlowsCommand(CommandLineApplication command)
        {
            command.Description = "Export flows from the capture file or live capture.";
            command.ExtendedHelpText = "This command processes the input file, collects all flows of the selected protocol and writes " +
            "the flow records in the output file using the specified format.";

            command.HelpOption("-?|-h|--help");

            var inputOption = command.AddInputOptions();


            var protocolTypeOption = command.Option("-p|--protocol-type <value>",
                $"A protocol type. Can be one of {String.Join(", ", Enum.GetNames(typeof(IndustrialProtocol)))}. This option is required.",
                CommandOptionType.SingleValue);

            var windowSizeOption = command.Option("-t|--window-size <value>",
                $"Size of the window. This parameter is used to split input file to windows of the respective size. Required format is HH:MM:SS.ff",
                CommandOptionType.SingleValue);

            var outputFormatOption = command.Option("-f|--output-format <value>",
                $"A format of the output. Can be one of {String.Join(", ", Enum.GetNames(typeof(OutputFormat)))}. Default is {nameof(OutputFormat.Json)}.",
                CommandOptionType.SingleValue);

            var outputFileOption = command.Option("-w|--output-file <value>",
                "An optional output file name to write the flows to. Default is stdout.",
                CommandOptionType.SingleValue);

            command.OnExecute(async () =>
            {
                if (!protocolTypeOption.TryParseValueOrError<IndustrialProtocol>(EnumTryParse, () => Console.Error.WriteLine("Input error: protocol type must be specified!"), _ => Console.Error.WriteLine("Input error: Invalid protocol type specified!"), out var protocolType)) return -1;
                if (!windowSizeOption.TryParseValueOrDefault(TimeSpan.TryParse, TimeSpan.FromMinutes(5), _ => Console.Error.WriteLine("Input error: Invalid type span specified. Required format is HH:MM:SS.ff"), out var windowTimeSpan)) return -1;
                if (!outputFormatOption.TryParseValueOrDefault(EnumTryParse, OutputFormat.Json, _ => Console.Error.WriteLine("Input error: Invalid input value specified."), out var outputFormat)) return -1;
                using var writer = outputFileOption.HasValue() ? File.CreateText(outputFileOption.Value()) : Console.Out;


                var captureDevice = inputOption.GetCaptureDevice();
                if (captureDevice == null) throw new CommandParsingException(command, "An input must be specified.");
                await _functions.ExportFlowsAsync(captureDevice, protocolType, windowTimeSpan, writer, outputFormat, TimeSpan.MaxValue, _appLifetime.ApplicationStopping);
                return 0;
            });
        }
    }
}

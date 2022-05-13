using IcsMonitor.Utils;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.IO;
using Traffix.DataView;

namespace IcsMonitor
{
    internal sealed partial class ConsoleService
    {
        private void TestFlowsCommand(CommandLineApplication command)
        {
            command.Description = "Test input flows using the created profile.";
            command.ExtendedHelpText = "This command read flows from the specified input file  and " +
            "applies the pre-learned profile to detect anomalies.";
            command.HelpOption("-?|-h|--help");

            var flowFileOption = command.Option("-R|--flows-file <value>",
                "An input pcap file to read. This option is required.",
                CommandOptionType.SingleValue);

            var profileFileOption = command.Option("-p|--profile-file <value>",
                "A file name of stored profile. This argument is required.",
                CommandOptionType.SingleValue);

            var outputFormatOption = command.Option("-f|--output-format <value>",
                $"A format of the output. Can be one of {String.Join(", ", Enum.GetNames(typeof(OutputFormat)))}. Default is {nameof(OutputFormat.Json)}.",
                CommandOptionType.SingleValue);

            var writeOutputOption = command.Option("-w|--write-output <value>",
                $"A name of the output file.",
                CommandOptionType.SingleValue);

            var thresholdOutputOption = command.Option("-t|--threshold <value>",
                $"A threshold value. Used to detect anomalies. If score is less than the threshold then the anomaly is reported.",
                CommandOptionType.SingleValue);

            var writeDetectedOption = command.Option("-W|--write-detected <value>",
               $"A name of the file consisting of detected flows.",
               CommandOptionType.SingleValue);

            command.OnExecute(async () =>
            {
                if (!flowFileOption.TryGetValueOrError(() => Console.Error.WriteLine($"Error: '--{flowFileOption.LongName}' option is required!"), out var flowFile)) return -1;
                if (!profileFileOption.TryGetValueOrError(() => Console.Error.WriteLine($"Error: '--{profileFileOption.LongName}' option is required!"), out var profileFile)) return -1;
                if (!outputFormatOption.TryParseValueOrDefault(EnumTryParse, OutputFormat.Json, _ => Console.Error.WriteLine("Input error: Invalid format specified. Possible formats are Json, Csv, Yaml, Markdown."), out var outputFormat)) return -1;

                using var outputWriter = writeOutputOption.HasValue() ? new StreamWriter(File.Open(writeOutputOption.Value(), FileMode.Create)) : TextWriter.Null;
                using var detectedWriter = writeDetectedOption.HasValue() ? new StreamWriter(File.Open(writeDetectedOption.Value(), FileMode.Create)) : TextWriter.Null;
                thresholdOutputOption.TryParseValueOrDefault(Double.TryParse, 0.1, _ => Console.Error.WriteLine(), out var thresholdValue);

                var profile = _functions.LoadProfileFromFile(profileFile);
                await _functions.TestFlowsAsync(flowFile, profile, outputFormat, outputWriter, thresholdValue, detectedWriter);
                outputWriter.Flush();
                detectedWriter.Flush();
                return 0;
            });

        }

    }
}

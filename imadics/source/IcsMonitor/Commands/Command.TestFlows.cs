using IcsMonitor.Utils;
using Microsoft.Extensions.CommandLineUtils;
using System;
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

            command.OnExecute(async () =>
            {
                if (!flowFileOption.TryGetValueOrError(() => Console.Error.WriteLine($"Error: '--{flowFileOption.LongName}' option is required!"), out var flowFile)) return -1;
                if (!profileFileOption.TryGetValueOrError(() => Console.Error.WriteLine($"Error: '--{profileFileOption.LongName}' option is required!"), out var profileFile)) return -1;
                if (!outputFormatOption.TryParseValueOrDefault(EnumTryParse, OutputFormat.Json, _ => Console.Error.WriteLine("Input error: Invalid format specified. Possible formats are Json, Csv, Yaml, Markdown."), out var outputFormat)) return -1;

                var profile = _functions.LoadProfileFromFile(profileFile);
                await _functions.TestFlowsAsync(flowFile, profile, outputFormat, Console.Out);
                return 0;
            });

        }

    }
}

using IcsMonitor.Utils;
using Microsoft.Extensions.CommandLineUtils;
using System;
using Traffix.DataView;

namespace IcsMonitor
{
    internal sealed partial class ConsoleService
    {
        /// <summary>
        /// Implements and registers Watch-Traffic command.
        /// </summary>
        /// <param name="command">The command object used to register the command.</param>
        private void WatchTrafficCommand(CommandLineApplication command)
        {
            command.Description = "Watch traffic and test flows using the existing profile.";
            command.ExtendedHelpText = "This command monitors the communication on the given capture device (can be a capture file), collects flows based on the information from the given profile and " +
            "applies the pre-learned profile to test the flows.";
            command.HelpOption("-?|-h|--help");

            var inputOption = command.AddInputOptions();

            var profileFileOption = command.Option("-p|--profile-file <value>",
                "A file name of stored profile. This argument is required.",
                CommandOptionType.SingleValue);

            var outputFormatOption = command.Option("-f|--output-format <value>",
                $"A format of the output. Can be one of {String.Join(", ", Enum.GetNames(typeof(OutputFormat)))}. Default is {nameof(OutputFormat.Json)}.",
                CommandOptionType.SingleValue);

            var replaySpeedOption = command.Option("-s|--replay-speed <value>",
                $"If capture file is provided as a source, this option determines its replay speed. (This parameter is optional)",
                CommandOptionType.SingleValue);

            command.OnExecute(async () =>
            {
                if (!profileFileOption.TryGetValueOrError(() => Console.Error.WriteLine($"Error: '--{profileFileOption.LongName}' option is required!"), out var profileFile)) return -1;
                if (!outputFormatOption.TryParseValueOrDefault(EnumTryParse, OutputFormat.Json, _ => Console.Error.WriteLine("Input error: Invalid input value specified."), out var outputFormat)) return -1;

                replaySpeedOption.TryParseValueOrDefault<float>(float.TryParse, 0f, _ => Console.Error.WriteLine("Input error: Invalid replay speed specified. It must be a float value."), out var replaySpeed);
                var device = inputOption.GetCaptureDevice(replaySpeed);
                await _functions.WatchTrafficAsync(device, _functions.LoadProfileFromFile(profileFile), outputFormat, Console.Out, _appLifetime.ApplicationStopping);
                return 0;
            });

        }
    }
}

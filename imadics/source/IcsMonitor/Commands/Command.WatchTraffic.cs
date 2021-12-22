using IcsMonitor.Utils;
using Microsoft.Extensions.CommandLineUtils;
using System;
using Traffix.DataView;

namespace IcsMonitor
{
    internal sealed partial class ConsoleService
    {
        private void WatchTrafficCommand(CommandLineApplication command)
        {
            command.Description = "Watch traffic and test flows using the existing profile.";
            command.ExtendedHelpText = "This command monitors the communication on the given capture device, collects flows of the target profile protocol and " +
            "applies the pre-learned profile.";
            command.HelpOption("-?|-h|--help");

            var inputOption = command.AddInputOptions();

            var profileFileOption = command.Option("-p|--profile-file <value>",
                "A file name of stored profile. This argument is required.",
                CommandOptionType.SingleValue);

            var outputFormatOption = command.Option("-f|--output-format <value>",
                $"A format of the output. Can be one of {String.Join(", ", Enum.GetNames(typeof(OutputFormat)))}. Default is {nameof(OutputFormat.Json)}.",
                CommandOptionType.SingleValue);

            command.OnExecute(async () =>
            {
                if (!profileFileOption.TryGetValueOrError(() => Console.Error.WriteLine($"Error: '--{profileFileOption.LongName}' option is required!"), out var profileFile)) return -1;
                if (!outputFormatOption.TryParseValueOrDefault(EnumTryParse, OutputFormat.Json, _ => Console.Error.WriteLine("Input error: Invalid input value specified."), out var outputFormat)) return -1;

                var device = inputOption.GetCaptureDevice();
                await _functions.WatchTrafficAsync(device, _functions.LoadProfileFromFile(profileFile), outputFormat, Console.Out, _appLifetime.ApplicationStopping);
                return 0;
            });

        }
    }
}

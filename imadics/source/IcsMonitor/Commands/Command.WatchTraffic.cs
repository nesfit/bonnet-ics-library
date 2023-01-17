using IcsMonitor.Utils;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Threading;
using System.Threading.Tasks;
using Traffix.DataView;
using static IcsMonitor.AnomalyDetection.ClusterModel;
using static Traffix.Extensions.Decoders.Industrial.DlmsData;

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
                $"A format of the output. Can be one of {String.Join(", ", System.Enum.GetNames(typeof(OutputFormat)))}. Default is {nameof(OutputFormat.Json)}.",
                CommandOptionType.SingleValue);

            var replaySpeedOption = command.Option("-s|--replay-speed <value>",
                $"If capture file is provided as a source, this option determines its replay speed. (This parameter is optional)",
                CommandOptionType.SingleValue);

            var timeoutOption = command.Option("-t|--timeout <value>",
                $"Specifies the traffic monitoring time. After this time, the monitor stops. (This parameter is optional)",
                CommandOptionType.SingleValue);

            command.OnExecute(async () =>
            {
                if (!profileFileOption.TryGetValueOrError(() => Console.Error.WriteLine($"Error: '--{profileFileOption.LongName}' option is required!"), out var profileFile)) return -1;
                if (!outputFormatOption.TryParseValueOrDefault(EnumTryParse, OutputFormat.Json, _ => Console.Error.WriteLine("Input error: Invalid input value specified."), out var outputFormat)) return -1;

                replaySpeedOption.TryParseValueOrDefault<float>(float.TryParse, 0f, _ => Console.Error.WriteLine("Input error: Invalid replay speed specified. It must be a float value."), out var replaySpeed);
                timeoutOption.TryParseValueOrDefault<TimeSpan>(TimeSpan.TryParse, Timeout.InfiniteTimeSpan, _ => Console.Error.WriteLine("Input error: Invalid timout specified. It must be a timespan in the format \"hh:mm:ss\"."), out var timeoutSpan);

                var device = inputOption.GetCaptureDevice(replaySpeed);

                if (device == null)
                {
                    Console.Error.WriteLine($"Input error:Cannot open source {inputOption}.");
                    return 1;
                }
                // this task is not needed
                var __timeoutTask = Task.Delay(timeoutSpan, _appLifetime.ApplicationStopping).ContinueWith(t => _appLifetime.StopApplication() );
                var infoString = $@"Running Monitor:
    command: Watch-Traffic
    input-device: {device.Name} 
    profile: {profileFile}
    output-format: {outputFormat}
    output-device: Console.Out
    time-out: {((timeoutSpan == Timeout.InfiniteTimeSpan) ? "Infinite" : timeoutSpan.ToString()) }
---";
                Console.Error.WriteLine(infoString);
                await _functions.WatchTrafficAsync(device, _functions.LoadProfileFromFile(profileFile), outputFormat, Console.Out, _appLifetime.ApplicationStopping);
                return 0;
            });

        }
    }
}

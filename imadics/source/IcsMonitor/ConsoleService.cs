using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace IcsMonitor
{
    internal sealed partial class ConsoleService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly TrafficMonitorContext _functions;
        private Task _runningTask;

        public ConsoleService(
            ILogger<ConsoleService> logger,
            IHostApplicationLifetime appLifetime,
            TrafficMonitorContext functions)
        {
            _logger = logger;
            _appLifetime = appLifetime;
            _functions = functions;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _runningTask = Task.Factory.StartNew(OnRun);
            return Task.CompletedTask;
        }

        private void OnRun()
        {
            var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
            _logger.LogDebug($"Starting with arguments: {string.Join(" ", args)}");

            var app = new CommandLineApplication
            {
                Name = "IcsMonitor",
                Description = "IcsMonitor implements various anomaly detection methods for ICS network traffic.",
                ExtendedHelpText = "IcsMonitor console application can create ICS profile and detect anomalies for a range of ICS protocols: MODBUS/TCP, DNP3, and Siemens S7. The tool " +
                "can also print ICS flows possibly aggregated by flow key fields."
            };


            app.HelpOption("-?|-h|--help");
            app.VersionOption("-v|--version", () =>
            {
                return string.Format("Version {0}", Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
            });

            app.Command("Show-Devices", ShowDevicesCommand);
            app.Command("Show-Profile", ShowProfileCommand);

            app.Command("Build-Profile", BuidProfileCommand);
            app.Command("Export-Flows", ExportFlowsCommand);
            app.Command("Test-Flows", TestFlowsCommand);
            app.Command("Watch-Traffic", WatchTrafficCommand);


            try
            {
                app.Execute(args);
            }
            catch (CommandParsingException ex)
            {
                _logger.LogError("Incorrect arguments {0}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to execute application {0}", ex.Message);
            }
            _appLifetime.StopApplication();
        }

        private static bool EnumTryParse<TEnum>(string input, out TEnum value) where TEnum : struct => Enum.TryParse<TEnum>(input, true, out value);

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _runningTask;
        }
    }
}

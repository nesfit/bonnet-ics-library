using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace AnomalyInjector
{
    internal class ConsoleService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private Task _runningTask;
        private CommandArgument _modbusServerEndpointArgument;

        public ConsoleService(
            ILogger<ConsoleService> logger,
            IHostApplicationLifetime appLifetime)
        {
            _logger = logger;
            _appLifetime = appLifetime;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _runningTask = Task.Factory.StartNew(OnRun, cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _runningTask;
        }

        internal IPEndPoint ServerEndPoint
        {
            get
            {
                var serverString = _modbusServerEndpointArgument.Value;
                if (serverString == null) return null;
                var epString = serverString.Split('/')[0];
                IPEndPoint.TryParse(epString, out var ep);
                return ep;
            }
        }

        internal int DeviceId
        {
            get
            {
                var serverString = _modbusServerEndpointArgument.Value;
                if (serverString == null) return 0;
                var stringParts = serverString.Split('/');
                if (stringParts.Length > 1)
                {
                    int.TryParse(stringParts[1], out var deviceId);
                    return deviceId;
                }
                else
                {
                    return 0;
                }
            }
        }
        private void OnRun()
        {
            var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
            _logger.LogInformation($"Starting with arguments: {string.Join("__", args)}");

            var app = new CommandLineApplication
            {
                Name = "Anomalify",
                Description = "Anomalify is a simple application that when running can simulate various error and attack situations in the running Factory I/O scene.",
            };

            app.HelpOption("-?|-h|--help");
            app.VersionOption("-v|--version", () =>
            {
                return string.Format("Version {0}", Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
            });

            _modbusServerEndpointArgument = new CommandArgument() { Name = "server", Description = "Specifies MODBUS endpoint connection string, e.g., `192.168.111.17:502/1`." };
            app.Command("Address-Scan", AddressScanCommand);
            app.Command("FunctionCode-Scan", FunctionCodeScanCommand);
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

        private void AddressScanCommand(CommandLineApplication command)
        {
            command.Description = "Address scan reconnaissance stands for identification of MODBUS devices on the given IP address.";
            command.HelpOption("-?|-h|--help");

            command.Arguments.Add(_modbusServerEndpointArgument);
            var scanRangeArgument = command.Argument("scanRange", "Defines the range of device addresses to scan.");

            command.OnExecute(async () =>
            {
                var range = GetListFromRange(scanRangeArgument.Value);
                _logger.LogInformation($"Starting Address-Scan: server={this.ServerEndPoint}, address-range={String.Join(',', range)} ");

                var controller = new ModbusReconnaissanceController(this.ServerEndPoint);
                await foreach (var result in controller.SlaveAddressScanAsync(range))
                {
                    Console.WriteLine($"Slave id={result.Address}: {result.Status}");
                }
                return 0;
            });
        }
        private void FunctionCodeScanCommand(CommandLineApplication command)
        {
            command.Description = "Function code scan reconnaissance performs enumeration of supoprted functions of the device.";
            command.HelpOption("-?|-h|--help");

            command.Arguments.Add(_modbusServerEndpointArgument);
            var scanRangeArgument = command.Argument("scanRange", "Defines the range of function codes to scan.");

            command.OnExecute(async () =>
            {
                var range = GetListFromRange(scanRangeArgument.Value);
                _logger.LogInformation($"Starting Address-Scan: server={this.ServerEndPoint}, function-range={String.Join(',', range)} ");

                var controller = new ModbusReconnaissanceController(this.ServerEndPoint);
                await foreach (var result in controller.FunctionCodeScanAsync(range))
                {
                    Console.WriteLine($"Slave id={result.FunctionCode}: {result.Status}");
                }
                return 0;
            });
        }
        private int[] GetListFromRange(string value)
        {
            var parts = value.Split(',');
            IEnumerable<int> GetPartRange(string p)
            {
                var r = p.Split("..");
                if (r.Length == 2)
                {
                    var s = int.Parse(r[0]);
                    var e = int.Parse(r[1]);
                    return Enumerable.Range(s, 1 + e - s);
                }
                else
                    return new int[] { int.Parse(r[0]) };
            }

            return parts.SelectMany(GetPartRange).ToArray();
        }
    }
}
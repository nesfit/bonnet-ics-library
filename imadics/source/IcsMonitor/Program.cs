﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using System.Threading.Tasks;

namespace IcsMonitor
{
    public sealed class Program
    {
        public static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<MLContext>();
                    services.AddSingleton<TrafficMonitorContext>();
                    services.AddHostedService<ConsoleService>();
                })
                .ConfigureLogging((hostContext, logging) =>
                {
                    logging.AddConsole(options =>
                    {
                        options.LogToStandardErrorThreshold = LogLevel.Information;
                        options.IncludeScopes = true;
                        options.TimestampFormat = "hh:mm:ss ";
                    });
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .RunConsoleAsync();
        }
    }
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using System.Threading.Tasks;

namespace IcsMonitor
{
    /// <summary>
    /// The main class of the tool. It manages application registration and controls the lifetime.
    /// </summary>
    public sealed class Program
    {
        /// <summary>
        /// The entry point of the application.
        /// </summary>
        /// <param name="args">Input arguments.</param>
        /// <returns>Task that completes on exit.</returns>
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
                        options.LogToStandardErrorThreshold = LogLevel.Error;
                        options.IncludeScopes = true;
                        options.TimestampFormat = "hh:mm:ss ";
                    });
                    logging.SetMinimumLevel(LogLevel.Warning);
                })
                .RunConsoleAsync();
        }
    }
}

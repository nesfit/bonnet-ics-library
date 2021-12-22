using Microsoft.Extensions.CommandLineUtils;
using System;

namespace IcsMonitor
{
    internal sealed partial class ConsoleService
    {
        private void ShowDevicesCommand(CommandLineApplication command)
        {
            command.Description = "Show a list of all available network devices.";
            command.HelpOption("-?|-h|--help");

            var showDetailsOption = command.Option("-d|--details", "Get detailed information on network devices.", CommandOptionType.NoValue);

            command.OnExecute(() =>
            {
                var deviceIndex = 0;
                foreach (var dev in SharpPcap.CaptureDeviceList.Instance)
                {
                    Console.WriteLine($"Device Index: {deviceIndex}");
                    Console.WriteLine($"Device Name:  {dev.Name}");

                    Console.WriteLine($"Description:  {dev.Description}");

                    if (showDetailsOption.HasValue())
                    {
                        dev.Open();
                        Console.WriteLine($"Link type:    {dev.LinkType}");
                        Console.WriteLine($"Address:      {dev.MacAddress}");
                        dev.Close();

                    }

                    Console.WriteLine();
                    deviceIndex++;
                }
                return 0;
            });
        }
    }
}

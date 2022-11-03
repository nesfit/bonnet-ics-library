using Microsoft.Extensions.CommandLineUtils;
using SharpPcap;
using SharpPcap.LibPcap;
using System;

namespace IcsMonitor
{

    /// <summary>
    /// Represents the possible input configuration.
    /// </summary>
    /// <param name="FlowFile">The command option specifiying flow file name.</param>
    /// <param name="CaptureFile">The command option specifiying capture file name.</param>
    /// <param name="DeviceName">The command option specifiying device name, e.g., network interface string identifier.</param>
    /// <param name="DeviceIndex">The command option specifiying, e.h., network interface index.</param>
    public record InputSources(CommandOption FlowFile, CommandOption CaptureFile, CommandOption DeviceName, CommandOption DeviceIndex)
    {
        /// <summary>
        /// Gets the capture device as given in this comfigration record.
        /// </summary>
        /// <returns>The capture device object or null, if the current configuration does not specify a valid interface.</returns>
        public ICaptureDevice GetCaptureDevice()
        {
            if (CaptureFile.HasValue())
            {
                return new CaptureFileReaderDevice(CaptureFileValue);
            }
            if (DeviceName.HasValue())
            {
                return SharpPcap.CaptureDeviceList.Instance[DeviceNameValue];
            }
            if (DeviceIndex.HasValue())
            {
                return SharpPcap.CaptureDeviceList.Instance[DeviceIndexValue];
            }
            return null;
        }
        /// <summary>
        /// The name of the flow file or null.
        /// </summary>
        public string FlowFileValue => FlowFile.Value();
        /// <summary>
        /// The name of the capture file or null.
        /// </summary>
        public string CaptureFileValue => CaptureFile.Value();
        /// <summary>
        /// The name of the network device or null.
        /// </summary>
        public string DeviceNameValue => DeviceName.Value();
        /// <summary>
        /// The index of the network device.
        /// </summary>
        public int DeviceIndexValue => Int32.Parse(DeviceIndex.Value());

        /// <summary>
        /// True if flow file is specified in the current configuration record.
        /// </summary>
        public bool HasFlowFile => FlowFile.HasValue();
    }

    /// <summary>
    /// Provides several extensions usable in command objects.
    /// </summary>
    internal static class CommandExtensions
    {
        /// <summary>
        /// Adds the input configuration record in the command options.
        /// </summary>
        /// <param name="command">The command to be extended with input configuration record option.</param>
        /// <returns>The newly created input congiration option.</returns>
        public static InputSources AddInputOptions(this CommandLineApplication command)
        {
            var flowFileOption = command.Option("-R|--input-flows <value>",
                "An input flow file to read. One of the inputs arguments is required.",
                CommandOptionType.SingleValue);

            var captureFileOption = command.Option("-r|--input-file <value>",
                "An input pcap file to read. One of the inputs arguments is required.",
                CommandOptionType.SingleValue);

            var inputDeviceNameOption = command.Option("-n|--device-name <value>",
                "An input interface to read the packets from. One of the inputs arguments is required.",
                CommandOptionType.SingleValue);

            var inputDeviceIndexOption = command.Option("-i|--device-index <value>",
                "An input interface to read the packets from. One of the inputs arguments is required.",
                CommandOptionType.SingleValue);

            return new InputSources(flowFileOption, captureFileOption, inputDeviceNameOption, inputDeviceIndexOption);
        }
    }
}

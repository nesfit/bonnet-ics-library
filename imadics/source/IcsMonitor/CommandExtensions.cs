using Microsoft.Extensions.CommandLineUtils;
using SharpPcap;
using SharpPcap.LibPcap;
using System;

namespace IcsMonitor
{

    public record InputSources(CommandOption FlowFile, CommandOption CaptureFile, CommandOption DeviceName, CommandOption DeviceIndex)
    {
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
        public string FlowFileValue => FlowFile.Value();
        public string CaptureFileValue => CaptureFile.Value();
        public string DeviceNameValue => DeviceName.Value();
        public int DeviceIndexValue => Int32.Parse(DeviceIndex.Value());

        public bool HasFlowFile => FlowFile.HasValue();
    }

    internal static class CommandExtensions
    {
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

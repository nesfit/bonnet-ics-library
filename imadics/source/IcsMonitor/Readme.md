# IcsMonitor

The tool implements a proof of concept of anomaly detection based on a pre-computed profile applicable to ICS protocols. 
It works on packet capture files consisting of ICS communication. It has two basic modes: i) In a mode of profile building it takes the provided capture file representing a normal system communication and creates the profile; ii) In traffic anomaly detection mode, the previously created profile is applied to the provided packet capture files or live traffic for detecting deviations to the normal communication. The tool is implemented in C# and uses the ML.NET library for performing data analysis tasks.


## 🚀 Getting Started

The project is entirely written in C# 10 requiring .NET 6. It depends on packages that can be installed via the NuGet package manager system. Packet capture processing is done via [SharpPcap](https://github.com/dotpcap/sharppcap) package that requires a native `*pcap` library to be installed (depending on the OS).
The application is multiplatform and runs on every OS that supports .NET 6.

### Prerequisites

* [Libpcap](http://www.tcpdump.org/manpages/pcap.3pcap.html) on Linux and WinPcap/[Npcap](https://nmap.org/npcap/guide/npcap-devguide.html#npcap-api) on Windows needs to be installed. 
* [Microsoft .NET Core 6](https://dotnet.microsoft.com/download/dotnet/6.0) either runtime for running the tool or SDK for development.
* [Visual Studio Code](https://code.visualstudio.com/) or [Visual Studio 2022 Prefview](https://visualstudio.microsoft.com/cs/vs/preview/#download-preview) for development of the project is recommended.

### Compilation

* Clone the project:

```
gh repo clone rysavy-ondrej/bonnet-ics
```

* or alternatively download the latest version:

```
wget https://github.com/rysavy-ondrej/bonnet-ics/archive/refs/heads/main.zip
```

* In the root folder of the project execute the following command that restores references and builds the executable file:

```
dotnet build
```

* the executable file can be found in `source/IcsMonitor/bin/Debug/net6.0`

## Usage

The tool provides two main functions: building a profile from normal traffic and examining the provided communication for violating the expected traffic patterns. The tool provides the following commands:

| Command       | Description |
| ------------- | -------------------- |
| Build-Profile | Create a profile for the given input and profile parameters. | 
| Export-Flows  | Export flows from the capture file or live capture. |
| Show-Devices  | Show a list of all available network devices. |
| Show-Profile  | Prints the stored profile in user readable format. |
| Test-Flows    | Test input flows using the created profile. |
| Watch-Traffic | Watch traffic and test flows using the existing profile. |

### Build-Profile Command

A normal traffic packet capture file makes it possible to create a profile. When creating the profile some other parameters need to be set, in particular, the industrial protocol of the communication, the window size for collecting packets, and computing statistics for them.  

```
Usage: IcsMonitor Build-Profile [options]

Options:
  -?|-h|--help                Show help information
  -R|--input-flows <value>    An input pcap file to read. This option is required.
  -r|--input-file <value>     An input pcap file to read. This option is required.
  -n|--device-name <value>    An input interface to read the packets from. This argument is required.
  -i|--device-index <value>   An input interface to read the packets from. This argument is required.
  -m|--method <value>         An anomaly detection method to be used to create the profile. Can be one of Centroids. This option is required.
  -p|--protocol-type <value>  A protocol type. Can be one of Modbus, Dnp3, S7, Mqtt, Coap. This option is required.
  -t|--windowSize <value>     Size of the window as TimeSpan. Required format is HH:MM:SS. This parameter is used to split input file to windows of the respective size.
  -c|--windowCount <value>    Number of windows to collect for creating the profile.
  -f|--feature-file <value>   List of field names that represents a collection of features to be used as the input for AD method. For instance "ForwardMetricsDuration,ForwardMetricsPackets,ForwardMetricsOctets"
  -w|--output-file <value>    An output file name to store the profile. This option is required.
```

The following command builds a profile based on the specified flow file:

```
IcsMonitor Build-Profile -R "lemay\attack\characterization_modbus_6RTU_with_operate.flows.csv" -t "00:01:00" -c 60 -m Centroids -p Modbus -f "ForwardMetricsPackets,ForwardMetricsOctets,ReverseMetricsPackets,ReverseMetricsOctets" -w "profiles\6RTU_with_operate.profile.zip"
```

The next command builds based on the specific capture file:

```
IcsMonitor Build-Profile -r "lemay\attack\characterization_modbus_6RTU_with_operate.pcap" -t "00:01:00" -c 30 -m Centroids -p Modbus -f "ForwardMetricsPackets,ForwardMetricsOctets,ReverseMetricsPackets,ReverseMetricsOctets" -w "profiles\6RTU_with_operate.profile.zip"
```

Finally, it is also possible to build profile from live capture:

```
IcsMonitor  Build-Profile -i 0 -m centroids -p modbus -t 00:00:10 -c 30 -w .\data\modbus\profiles\factory-assembler-10s.profile.zip
```

The built profile is stored in the profile file and can be used to detect anomalies. It is possible to print the configuration of the profile file by the following command:

```
IcsMonitor show-profile  -p "profiles\6RTU_with_operate.profile.zip"
```

### Test-Flows Command

The created profile tests the network communication and finds possible violations from the expected traffic patterns as recognized and encoded in the profile. 

```
Usage: IcsMonitor Test-Flows [options]

Options:
  -?|-h|--help                Show help information
  -R|--flows-file <value>     An input pcap file to read. This option is required.
  -p|--profile-file <value>   A file name of stored profile. This argument is required.
  -f|--output-format <value>  A format of the output. Can be one of Json, Csv, Yaml, Markdown. Default is Json.
```

This command test flows as provided in the source CSV file:

```
IcsMonitor Test-Flows -R "lemay\attack\characterization_modbus_6RTU_with_operate.flows.csv" -p "profiles\6RTU_with_operate.profile.zip" -f csv > "results\6RTU_with_operate.csv"
```

### Watch-Traffic Command

The command performs anomaly detection applied to the packet communication. It performs flow identification and sampling data in windows defined in the profile file. Anomaly detection results are printed on the standard output in the configurable format.

```
Usage: IcsMonitor Watch-Traffic [options]

Options:
  -?|-h|--help                Show help information
  -R|--input-flows <value>    An input pcap file to read. This option is required.
  -r|--input-file <value>     An input pcap file to read. This option is required.
  -n|--device-name <value>    An input interface to read the packets from. This argument is required.
  -i|--device-index <value>   An input interface to read the packets from. This argument is required.
  -p|--profile-file <value>   A file name of stored profile. This argument is required.
  -f|--output-format <value>  A format of the output. Can be one of Json, Csv, Yaml, Markdown. Default is Json.
```

The command tests the flows in the captured traffic provided as a packet capture file:

```
IcsMonitor Watch-Traffic -r "lemay\attack\characterization_modbus_6RTU_with_operate.pcap" -p "profiles\6RTU_with_operate.profile.zip" -f csv > "results\6RTU_with_operate.csv"
```

The other option is to test flows in the live capture:

```
IcsMonitor Watch-Traffic -i 0  -p profiles\factory-assembler-1m.profile.zip -f yaml
```

### Export-Flows Command

The command enables the conversion of capture files or a live capture to the flow file. If the resulting file is a CSV file, it can be used as input to other commands of this tool and consumed by some other systems. 

```
Usage: IcsMonitor Export-Flows [options]

Options:
  -?|-h|--help                Show help information
  -R|--input-flows <value>    An input pcap file to read. This option is required.
  -r|--input-file <value>     An input pcap file to read. This option is required.
  -n|--device-name <value>    An input interface to read the packets from. This argument is required.
  -i|--device-index <value>   An input interface to read the packets from. This argument is required.
  -p|--protocol-type <value>  A protocol type. Can be one of Modbus, Dnp3, S7, Mqtt, Coap. This option is required.
  -t|--window-size <value>    Size of the window. This parameter is used to split input file to windows of the respective size. Required format is HH:MM:SS.ff
  -f|--output-format <value>  A format of the output. Can be one of Json, Csv, Yaml, Markdown. Default is Json.
  -w|--output-file <value>    An optional output file name to write the flows to. Default is stdout.
```

To convert a single capture file to the flow CSV file use the command as follows:

```
Export-Flows -r "lemay\attack\characterization_modbus_6RTU_with_operate.pcap" -p modbus -t "00:01:00" -f csv -w "lemay\attack\characterization_modbus_6RTU_with_operate.flows.csv"
```

## License

Distributed under the MIT License. See `LICENSE` for more information.

## Contact

Ondrej Rysavy - [@rysavy-ondrej](https://twitter.com/rysavy-ondrej) 

Project Link: [https://github.com/rysavy-ondrej/bonnet-ics](https://github.com/rysavy-ondrej/bonnet-ics)

## References
* [MLnet cookbok](https://github.com/dotnet/machinelearning/blob/main/docs/code/MlNetCookBook.md)



# Datasets

Datasets are only provided as CSV flow files generated from source packet captures mainly due to the size of original capture files and possible copyright. Nevertheless, the capture files are publicly available and are accessible via the accompanying links. 

## Organization

The datasets are organized according to the ICS protocol at the highest level. For each ICS protocol, the top folder collected different available datasets. Their name is either the original or an abbreviation derived from the original. Each dataset folder contains subfolders for normal communication and anomalous—however, the exact organization depends on the particular dataset. 
The naming conventions of the individual dataset files are described later in this document.

## Format 

Regardless of the ICS protocol, the format of the source CSV files contains always a basic collection of flow related fields as follows:

| Field | Meaning | Example |
| --- | --- | --- |
| WindowLabel | The label identifies the time window of the flow. Flows are collected within the windows.  | `0003` |
| WindowStart | A DateTime string represents the start edge of the window. | `02/24/2015 17:27:45` |
| WindowDuration | A TimeSpan string represents the duration of the window. | `00:01:00` |
| FlowLabel | It can be any string used for labeling the flow. Irt is useful if we have labeled dataset. | `0` |
| FlowKey | The string represents the flow key. In addition to key fields  the type of the key is given. | `MultiflowKey { ProtocolType = Tcp, ClientIpAddress = 192.168.1.101, ServerIpAddress = 192.168.1.104, ServerPort = 502 }`|
| ForwardMetricsDuration | Duration of the forward flow in seconds.  | `59.92964` |
| ForwardMetricsOctets | Number of octets of the forward flow. | `39944` |
| ForwardMetricsPackets | Number of packets of the forward flow. | `608` |
| ForwardMetricsFirstSeen | Value gives tick representation of timestamp of the first packet in the forward flow. | `635603956657284690` |
| ForwardMetricsLastSeen | Value gives tick representation of timestamp of the last packet in the forward flow. |`635603957256581090` |
| ReverseMetricsDuration |Duration of the reverse flow in seconds.  |`59.930524`|
| ReverseMetricsOctets | Number of octets of the reverse flow.| `37762`|
| ReverseMetricsPackets | Number of packets of the reverse flow. | `600`|
| ReverseMetricsFirstSeen | Value gives tick representation of timestamp of the first packet in the reverse flow.| `635603956657285550`|
| ReverseMetricsLastSeen | Value gives tick representation of timestamp of the last packet in the reverse flow.| `635603957256590790`|

Additional protocol-specific information appends the basic flow columns depending on the ICS protocol. For instance, MODBUS defines these columns: DataUnitId,DataReadRequests,DataWriteRequests,DataDiagnosticRequests,DataOtherRequests,DataUndefinedRequests,DataResponsesSuccess,DataResponsesError,DataMalformedRequests,and DataMalformedResponses.


## Usage

Datasets are provided in the form of CSV files representing extracted flow records. Each raw source capture file was processed several times using different window sizes. The naming convention is as follows:

```
NAME.flow-[WINDOW_SIZE].csv
```
where 
* NAME is the original name of the source PCAP file,
* WINDOW_SIZE is the size of the window used for collecting flows. It uses the convention of providing a number followed by the units.

For instance:
```
characterization_modbus_6RTU_with_operate.flows.1m.csv
```

To generate flow files `Export-Flows` command of `IcsMonitor` tool was used,e.g.:

```
IcsMonitor.exe Export-Flows -r "lemay\attack\characterization_modbus_6RTU_with_operate.pcap" -p modbus -t "00:01:00" -f csv -w "lemay\attack\characterization_modbus_6RTU_with_operate.flows.csv"
```


## Modbus

The MODBUS ICS communication for experiments is provided by the following datasets: 

* [Lemay](https://github.com/antoine-lemay/Modbus_dataset) - To provide representative data at the network level, the data sets were generated in a SCADA sandbox, where electrical network simulators were used to introduce realism in the physical component. Also, real attack tools, some of them custom built for Modbus networks, were used to generate the malicious traffic. Even though they do not fully replicate a production network, these data sets represent a good baseline to validate detection tools for SCADA systems.
* [Tjcruz](https://ieee-dataport.org/documents/cyber-security-modbus-ics-dataset) - This dataset was generated on a small-scale process automation scenario using MODBUS/TCP equipment, for research on the application of ML techniques to cybersecurity in Industrial Control Systems. The testbed emulates a CPS process controlled by a SCADA system using the MODBUS/TCP protocol. It consists of a liquid pump simulated by an electric motor controlled by a variable frequency drive (allowing for multiple rotor speeds), which in its turn controlled by a Programmable Logic Controller (PLC). The motor speed is determined by a set of predefined liquid temperature thresholds, whose measurement is provided by a MODBUS Remote Terminal Unit (RTU) device providing a temperature gauge, which is simulated by a potentiometer connected to an Arduino. The PLC communicates horizontally with the RTU, providing insightful knowledge of how this type of communications may have an effect on the overall system. The PLC also communicates with the Human-Machine Interface (HMI) controlling the system. The testbed is depicted in the image hereby included.
* [Factory](https://ieee-dataport.org/documents/modbus-dataset-ics-anomaly-detection) - This dataset consisting of MODBUS/TCP communication was created using the Factory.IO industrial process simulation application. The dataset contains different scenarios that control various industrial processes. For each scenario, capture files of regular communication, as well as communication with anomalies, are provided. The purpose of the dataset is to support research and evaluation of anomaly detection methods for the ICS domain. The dataset is provided in the form of raw source PCAP files supplemented by various index files and extracted data as CSV files suitable for further processing. Each scenario is described in detail in the accompanied Readme.md file.






 


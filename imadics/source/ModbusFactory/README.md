# ModbusFactory

The collection of programs that controls various Factory I/O scenes using Modbus protocol. The controllers are written as C# programs
that communicates with MODBUS server provided by Factory.IO environment. the purpose of this project is to provide a simulated control 
for various industrial processing pipeline as simulated in Factory. Becasue of separation of control appliance (C# software controller) from RTU devices (Modbus Server of Factory.IO)
it is possible to observe the industrial communication. 

## Process Controllers
Process controllers are C# console applications that uses [NSModbus4](https://www.nuget.org/packages/NSModbus4/) package 
for Modbus communication with RTUs. In fact, they are similar to SoftPLC, e.g., [MeckOut SoftPLC](http://meckout.com/).
In our seetings the devices are controlled by using Modbus/TCP connections. 

# Scenes

Currently, the following scenes from provided samples by Factory.IO are supported:

* Assembler Analog - Assemble parts made of lids and bases using analog control.
* Assembler - Assemble parts made of lids and bases using a two axis pick and place. 
* Production Line - Produce the same number of lids and bases by controlling two machine centers. 
* Separatng Station - Separate blue and green arts into two conveyors
* Sorting Station - Separate blue and green items using a vision sensor.

However, creating a new custom created scene is simple:

* For the scene the controller needs to be written with the help of `Controller` class provided by `SoftController` project.
* The controller reads the mapping of MODBUS addresses to Factory I/O from a Tag file. This file is generated from Factory.IO (in driver configuration, right bottom button - Tag Export) 
* For integration to SampleScene project, the newly implemented controller is automatically discovered using reflection.

## Assembler Analog


```
ModbusFactory.exe 192.168.111.17:502/1 AssemblerAnalog
```

## Assembler

```
ModbusFactory.exe 192.168.111.17:502/1 Assembler 
```

## Production Line

```
ModbusFactory.exe 192.168.111.17:502/1 ProductionLine 
```
## Separating Station

```
ModbusFactory.exe 192.168.111.17:502/1 SeparatingStation 
```
## Sorting Station

```
ModbusFactory.exe 192.168.111.17:502/1 SortingStation 
```

# Dataset Automation

To create the dataset automatically we need to orchestrate the start of Factory simulation and corresponding controller, and packet capture. 

## Factory Control

Factory simulation can be controlled via REST API. Web server in Factory I/O must be activated with the `app.web_server = True` console command.
Once the required scene is loaded in the environment it can be run using the following command:

```
PUT [FACTORY]:7410/api/tag/values/by-name

[
  {
    "name": "FACTORY I/O (Run)",
    "value": true
  }
]
```

When the simulation complete, we stop the scene by the following command:

```
PUT [FACTORY]:7410/api/tag/values/by-name

[
  {
    "name": "FACTORY I/O (Run)",
    "value": false
  }
]
```


## Traffic Capturing

MODBUS communication is captured using TSHARK as follows:

```
tshark -i DEVICE -w OUTFILE.pcap -F pcap -a duration:SECONDS "port 502"
```
where
* DEVICE is a network device to capture the communication at, use `tshark -D` for get a list of interfaces.
* OUTFILE is a name of capture file to create
* SECONDS is a duration of the capture in seconds (600~10min, 3600~1h, 43200~12h, 86400~1d)

For instance, the following captures 10 minutes of communication on the first non-loopback interface:

```
tshark -w Net-Assembler_Basic_Normal.pcap -F pcap -a duration:600 "port 502"
```

It is also possible to capture on loopback interface, eg:

```
tshark -i \Device\NPF_Loopback "port 502"
```

To automate dataset creation all the previous steps needs to be combined.


# Related Projects

* [EasyModbusTCP.NET](https://github.com/rossmann-engineering/EasyModbusTCP.NET) is Modbus TCP, Modbus UDP and Modbus RTU client/server library for .NET. Fast and secure access from PC or Embedded Systems to many PLC-Systems and other components for industry automation. Only a few lines of codes are needed to read or write data from or to a PLC.
* [SoftPlc](https://github.com/fbarresi/SoftPlc) is a software (Siemens brand) PLC controlled by over Web API. It communicates only with S7 protocol. Docker deployment is supported.
* [modbusPlcSimulator] is multi modbus slave devices simulator. The modbus server works as PLC devices. The simulation is controlled by use provided CSV dataset.
* [LadderApp](https://github.com/taleswsouza/LadderApp) the project allows to develop a program in ladder language (standard IEC 61131-3), simulate a PLC working, send the executable to a microcontroller, read a previously uploaded file from a microcontroller, and "remount" it in ladder language again.

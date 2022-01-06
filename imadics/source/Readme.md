# Source Projects

## IcsMonitor

The project implements a proof of concept of anomaly detection based on a pre-computed profile applicable to ICS protocols. 
The tool works on packet capture files consisting of ICS communication. It works in two basic modes. i) In a mode of profile building expecting to provide a capture file representing a normal system communication that can be used to learn it and create the profile. ii) In traffic anomaly detection mode, the previously created profile is applied to the provided packet capture files for detecting deviations to the normal communication. 
The tool is implemented in C# and uses the ML.NET library for performing data analysis tasks.

## ModbusFactory

The collection of programs that controls various Factory I/O scenes using Modbus protocol. The controllers are written as C# programs
that communicates with MODBUS server provided by Factory.IO environment. the purpose of this project is to provide a simulated control 
for various industrial processing pipeline as simulated in Factory. Becasue of separation of control appliance (C# software controller) from RTU devices (Modbus Server of Factory.IO) it is possible to observe the industrial communication. 

## SoftControllers

SoftControllers is a library used to control Factory I/O using MODBUS protocol. It provides base classes for MODBUS controllers implemented in ModbusFactory project.  

## AmomalyInjector

Anomaly injector is a simple application that when running can simulate various error and attack situations in the running Factory I/O scene.  

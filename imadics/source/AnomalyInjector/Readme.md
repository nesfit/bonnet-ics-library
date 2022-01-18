# Anomaly Injector

Anomaly injector is a simple application that when running can simulate various error and attack situations in the running Factory I/O scene.  

## Anomalies

Anomalies can be roughly identified as either errors or attacks. While errors results as the product of a failure, attacks are caused on purpose by an intruder. However, both of them may be identified as deviations from the expected system behavior.  


### Errors

The errors can be either network related or factory based.

Network relate errors are: 

* *packet delays and jitter* cause that some messages are delivered late and thus this influence the process being controlled 
* *packet loss* means that they need to be retrasmitted and again this can cause the delay

To simulate the effect of network errors we can use [Toxiproxy](https://github.com/shopify/toxiproxy).

Factory related error can be simulated by failure injection method provided by Factory I/O on most of the parts. It is possible to set:

* Open circuit
* Short circuit 
* Forced Value

The issue is that it seems this is not possible programmatically and must be only done in the UI.  

### Attacks

Morris and Gao (2013) identified 17 different attacks on ICS communiting with MODBUS protocol. They grouped these attacks into the following categories:

* Reconnaissance activities consist of (A1) Address Scan, (A2) Function Code Scan, and (A3) Device Identification
* Response and measurement injection attacks comprise of (A4) Naïve Read Payload Size, (A5) Invalid Read Payload Size, (A6)  Naïve False Error Response, 
(A7) Sporadic Sensor Measurement Injection, (A8) Calculated Sensor Measurement Injection, (A9) Replayed Measurement Injection, (A10) High Frequency Measurement Injection. 
* Commands injection stands for sending false control and configuration commands into a control system.
* Denial of Service

Attacks 8-10 are designed to look like normal process functions and because these attacks reflect a normal state, it is very difficult to detect them.

## Usage

Attacks are injected by executing the tool with URI of the MODBUS server specified.

```
Anomalify.exe [MODBUS-SERVER-URI] [ATTACK-TYPE] [ATTACK-PARAMETERS]
```

### A1. Address Scan

Address scan reconnaissance stands for identification of MODBUS devices on the given IP address. The address range for MODBUS RTU and ASCII systems is the attack parameter. The valid range is 0..247. Value 0 stands for broadcast address. The response may be acknowledgement or an error message.

```
Anomalify.exe [MODBUS-SERVER-URI] Address-Scan [ADDRESS-SCAN-RANGE]
```

For example:

```
Anomalify.exe 192.168.111.17:502 Address-Scan [1-100] 
```

```
Anomalify.exe 192.168.111.17:502 Address-Scan {1,3,5,7,9} 
```

### A2. Function Code Scan

This attack represents the way to enumerate a list of supported functions of RTU device. An attacker sends a query to all required function codes. MODBUS query payloads vary by function code. However, an attacker need not form a proper payload for each function code to determine if a function code is supported by a MODBUS server. If the function code is not supported an exception code 1 (invalid function code) response will be returned. All other responses, whether indicating an error or transaction success, indicate the function code is supported by the targeted server.

```
Anomalify.exe [MODBUS-SERVER-URI/DEVICE-ADDRESS] FunctionCode-Scan [FC-RANGE]
```

For example:

```
Anomalify.exe 192.168.111.17:502/1 Address-Scan {1,2,3,4,5,6} 
```

### A3. Device Identification 

MODBUS servers may provide function code that allows the client to read device identification information. Function code 0x11 allows an attacker to obtain the current run status and other device-specific information. According to the documentation, this is only available for serial lines. MODBUS servers may also implement function code 0x2B to provide access to basic, common, and advanced information.  The basic is mandatory for all MODBUS servers and includes vendor name, product code, and major and minor revisions. This operation implements reading BASIC, COMMON, and ADVANCED information. 

```
Anomalify.exe [MODBUS-SERVER-URI/DEVICE-ADDRESS] Device-Identification [BASIC|COMMON|ADVANCED] 
```

For example:

```
Anomalify.exe 192.168.111.17:502/1 Address-Scan BASIC 
```

### A4. Naïve Read Payload Size

### A5. Invalid Read Payload Size

### A6.  Naïve False Error Response

### A7. Sporadic Sensor Measurement Injection

### A8. Calculated Sensor Measurement Injection

### A9. Replayed Measurement Injection

### A10. High Frequency Measurement Injection

### A11. Altered System Control Scheme

### A12. Altered Actuator State

### A13. Altered Control Set Point

### A14. Force Listen Only Mode

### A15. Restart Communication

### A16. Invalid Cyclic Redundancy Code 

### A17. MODBUS Master Traffic Jamming

## References
* Morris TH, Gao W. Industrial Control System Cyber Attacks. In: Proceedings of the 1st International Symposium for ICS&SCADA Cyber Security Research. ; 2013:22-29. doi:10.14236/ewic/icscsr2013.3

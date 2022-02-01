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

### A1. Address Scan :white_check_mark:

Address scan reconnaissance stands for identification of MODBUS devices on the given IP address. The address range for MODBUS RTU and ASCII systems is the attack parameter. The valid range is 0..247. Value 0 stands for broadcast address. The response may be acknowledgement or an error message.

```
Anomalify.exe [MODBUS-SERVER-URI] Address-Scan [ADDRESS-SCAN-RANGE]
```

* `ADDRESS-SCAN-RANGE` defines the ranges of device addresses to scan. It can be given as a list of subranges, e.g., `1,10..20,30,40,50,60..80`.

For example:

```
Anomalify.exe 192.168.111.17:502 Address-Scan 1..100 
```


### A2. Function Code Scan :white_check_mark:

This attack represents the way to enumerate a list of supported functions of RTU device. An attacker sends a query to all required function codes. MODBUS query payloads vary by function code. However, an attacker need not form a proper payload for each function code to determine if a function code is supported by a MODBUS server. If the function code is not supported an exception code 1 (invalid function code) response will be returned. All other responses, whether indicating an error or transaction success, indicate the function code is supported by the targeted server.

```
Anomalify.exe [MODBUS-SERVER-URI/DEVICE-ADDRESS] FunctionCode-Scan [FC-RANGE]
```

* `FC-RANGE` the range of function codes to scan. The syntax is the same as for  `ADDRESS-SCAN-RANGE`.
For example:

```
Anomalify.exe 192.168.111.17:502/1 Address-Scan 1,2,3,4,5,6 
```

### A3. Device Identification :white_check_mark: 

MODBUS servers may provide function code that allows the client to read device identification information. Function code 0x11 allows an attacker to obtain the current run status and other device-specific information. According to the documentation, this is only available for serial lines. MODBUS servers may also implement function code 0x2B to provide access to basic, common, and advanced information.  The basic is mandatory for all MODBUS servers and includes vendor name, product code, and major and minor revisions. This operation implements reading BASIC, COMMON, and ADVANCED information. 

```
Anomalify.exe [MODBUS-SERVER-URI/DEVICE-ADDRESS] Device-Identification [BASIC|COMMON|ADVANCED] 
```

For example:

```
Anomalify.exe 192.168.111.17:502/1 Address-Scan BASIC 
```



### A4. Naïve Read Payload Size
An NMRI attack can craft malicious responses which include the correct quantity of returned objects but with preset or randomly generated content.
Executing this command creates the proxy which connects to specified MODBUS server. 

For instance, the query:

```
Function Code: Read Discrete Input (2)
Reference Number: 9
Bit Count: 1
```

will answer with:

```
Function Code: Read Discrete Input (2)
Byte Count: 1
Bit 9: False
```

This attack will change the return value, ie. Bit 9. The type of modification is either to set the value to zero or one or to generate random content.

```
Anomalify.exe [MODBUS-SERVER-URI/DEVICE-ADDRESS] Naive-Read-PayloadSizes [ZERO|ONES|RANDOM] --repeat REPEAT [--delay DELAY|--skip SKIP]
```

* ```ZERO``` always set the paylod to 0
* ```ONES``` always set the payload to 1
* ```RANDOM``` use random value to replace the original content
* ```REPEAT``` specifies how many NRMI modifications should be made. If not specified it performs an infinite number of modifications
* ```DELAY``` the delay between modifications. If not set no delay is applied.
* ```SKIP``` skips the specified number of replies between modifications. 

For instance, the following command modifies each 10th reply in the communication:

```
Anomalify.exe 192.168.111.17:502/1 Naive-Read-PayloadSizes --skip 9
```

### A5. Invalid Read Payload Size

In this attack, the reply is modified in a way that the number of requested objects does not match the request. The response can be modified by trimming the content part or by extending it with fixed or randomly generated values.

```
Anomalify.exe [MODBUS-SERVER-URI/DEVICE-ADDRESS] Invalid-Read-PayloadSizes [TRIM|EXTEND|RANDOM] --repeat REPEAT [--delay DELAY|--skip SKIP]
```

* ```TRIM``` modifies the respoonse by trimming the value
* ```EXTEND``` modifies the response by extending the value
* ```RANDOM``` modifies the size of the value randomly
* ```REPEAT``` specifies how many NRMI modifications should be made. If not specified it performs an infinite number of modifications
* ```DELAY``` the delay between modifications. If not set no delay is applied.
* ```SKIP``` skips the specified number of replies between modifications. 

For instance, the following command modifies each 10th reply in the communication:

```
Anomalify.exe 192.168.111.17:502/1 Naive-Read-PayloadSizes --skip 9
```

### A6.  Naïve False Error Response

The attack modifies the response in a way that it inserts false error code in the message. The modification is trivial by setting the MSB of the function code, eg., for `Reading Discrete Input`, which has FC `0x02`, the error answer is `0x82`.

```
Anomalify.exe [MODBUS-SERVER-URI/DEVICE-ADDRESS] Naive-False-ErrorResponse --repeat REPEAT [--delay DELAY|--skip SKIP]
```

* ```REPEAT``` specifies how many NRMI modifications should be made. If not specified it performs an infinite number of modifications
* ```DELAY``` the delay between modifications. If not set no delay is applied.
* ```SKIP``` skips the specified number of replies between modifications. 

For instance, the following command modifies each 10th reply in the communication:

```
Anomalify.exe 192.168.111.17:502/1 Naive-False-ErrorResponse --skip 9
```




### A7. Sporadic Sensor Measurement Injection

### A8. Calculated Sensor Measurement Injection

### A9. Replayed Measurement Injection

### A10. High Frequency Measurement Injection




### A11. Altered System Control Scheme

### A12. Altered Actuator State

### A13. Altered Control Set Point




### A14. Force Listen Only Mode

### A15. Restart Communication

### A16. Denial of Service Attacks

Denial of Service (DOS) attacks attempt to stop the proper functioning of some portion of the cyber-physical system. There may be several variations of this attack. We only consider MODBUS/TCP environment. The specific DOS attack includes generating MODBUS/TCP messages and sending them towards the attacked device. 
This attack considers to create a new TCP connection to the device and send MODBUS message using the given rate.

```
Anomalify.exe [MODBUS-SERVER-URI/DEVICE-ADDRESS] Denial-Of-Service --duration DURATION --rate RATE
```

* ```DURATION``` specifies the duration of the attack 
* ```RATE``` specifies the attack rate, ie., message per second

## References
* Morris TH, Gao W. Industrial Control System Cyber Attacks. In: Proceedings of the 1st International Symposium for ICS&SCADA Cyber Security Research. ; 2013:22-29. doi:10.14236/ewic/icscsr2013.3

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

* Reconnaissance - Address Scan, Function Code Scan, Device Identification
* Response and measurement injection
* Commands injection
* Denial of Service

## Usage

Attacks are injected by executing the tool with URI of the MODBUS server specified.

```
Anomalify.exe [MODBUS-SERVER-URI] [ATTACK-TYPE] [ATTACK-PARAMETERS]
```

### Address Scan

Address scan reconnaissance stands for identification of MODBUS devices on the given IP address. The address range for MODBUS RTU and ASCII systems is the attack parameter. The valid range is 0..247. The response may be acknowledgement or an error message.

```
Anomalify.exe [MODBUS-SERVER-URI] Address-Scan [ADDRESS-SCAN-RANGE]
```

For example:

```
Anomalify.exe 192.168.111.17:502 Address-Scan 1-100 
```

### Function Code Scan

```
Anomalify.exe FunctionCode-Scan
```

### Device Identification 

```
Anomalify.exe Device-Identification 
```


## References
* Morris TH, Gao W. Industrial Control System Cyber Attacks. In: Proceedings of the 1st International Symposium for ICS&SCADA Cyber Security Research. ; 2013:22-29. doi:10.14236/ewic/icscsr2013.3

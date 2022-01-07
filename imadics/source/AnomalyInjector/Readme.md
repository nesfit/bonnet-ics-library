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

### Attacks

Morris and Gao (2013) identified 17 different attacks on ICS communiting with MODBUS protocol. They grouped these attacks into the following categories:

* Recopnnaissance
* Response and measurement injection
* Commands injection
* Denial of Service

## Usage

TODO:...


## References
* Morris TH, Gao W. Industrial Control System Cyber Attacks. In: Proceedings of the 1st International Symposium for ICS&SCADA Cyber Security Research. ; 2013:22-29. doi:10.14236/ewic/icscsr2013.3

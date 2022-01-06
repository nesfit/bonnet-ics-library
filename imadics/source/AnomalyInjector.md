


# Anomaly Injector

## Injecting errors

The errors can be either network related or factory based.

Network relate errors are 

* excessive packet delays causeing the messages are delivered late and thus this influence the process being controlled 
* some. packet are dropped which means that they need to be retrasmitted and again this can cause the delay

Factory related error can be simulated by failure injection method provided by Factory I/O on most of the parts. It is possible to set:

* Open circuit
* Short circuit 
* Forced Value

## Injecting Attacks

Morris and Gao (2013) identified 17 different attacks on ICS communiting with MODBUS protocol. They grouped these attacks into the following categories:

* Recopnnaissance
* Response and measurement injection
* Commands injection
* Denial of Service

## References
* Morris TH, Gao W. Industrial Control System Cyber Attacks. In: Proceedings of the 1st International Symposium for ICS&SCADA Cyber Security Research. ; 2013:22-29. doi:10.14236/ewic/icscsr2013.3

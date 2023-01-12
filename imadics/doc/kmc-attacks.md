# ICS Attacks detectable by KMC method

* Reconnaissance attacks are conducted to discover information on the environment. For the general information on a host on the network, the traditional Internet-based protocols, such as ICMP, ARP, and UDP/TCP can be used to identify and determine the IP addresses of connected devices. Further, the ICS protocol and its information commands can be used to obtain additional information on connected ICS devices.

* Response injection attacks modify the responses from ICS devices thus causing the control algorithms to make incorrect decisions. Similalrly, command injection attacks insert false control commands making ICS devices to behave incorrectly.

* Replay attack stands for capturing a sequence of legitimate message and resend it again in anytime later. The idea of this attack is similar to injection attack, which means to intervene the  

The simple approach to simulate attacks is taken. The source PCAP file representing the normal traffic is modified according to the type of attack specified and provided parameters. 

## Attack Characteristics

### Reconnaissance
Reconnaissance attacks can be simulated by inserting new commands supposed to obtain information from the devices. 
The type of command depends on the specific ICS protocol, for instance, MODBUS have the following:
the attack has the following parameters:

* Type of ICS message
* Rate of messages
* Start and duration of the attack

### Injection
The method does not inspect the data payload of the messages thus we cannot detect modification done with the regualr message. However, it is possible to detection newly injected messages being either responses or commands.

* Type of ICS messages
* Rate of messages
* Start and duration of the attack

### Replay
The replay attack:

* Specification of the interval and a filter for recording the communication 
* Start of the attack

### DDoS Attack

This attack involves sending the number of requests necessary to prevent further requests from being processed due to resource exhaustion.


## PCAP 


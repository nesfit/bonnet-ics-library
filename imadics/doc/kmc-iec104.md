# K-Means Anomaly Detection Method for IEC104

The K-Means Anomaly Detection method (KMC) is based on calculating clusters that model normal behavior. Each cluster is represented by its centroid and a radius of acceptance. If the observed pattern lies in a cluster, it is accepted as correct. Points that lie far from existing clusters are called anomalous.

In this document, the application of this method to IEC104 IPFIX is specified.

## IEC104 IPFIX Records

| Field | Meaning |
| -- | ------ |
| BYTES | |
| PACKETS |  |
| L3_IPV4_SRC | |
| L3_IPV4_DST | |
| L4_PORT_SRC | |
| L4_PORT_DST | |
| IEC104_PKT_LENGTH | The lenght of the packet (IEC?) |
| IEC104_FRAME_FMT | U-frames, S-frames, I-frames |
| IEC104_ASDU_TYPE_IDENT | Type identification stands for the type of the information objects carried in the message,e.g., single point information, bit string of 32 bit, file segment (see Appendinx C1 of [[1]](https://www.fit.vut.cz/research/publication-file/11570/TR-IEC104.pdf))   |
| IEC104_ASDU_NUM_ITEMS | A number of objects referenced in the message. Each object can correspond to a data element for which the given operation (COT) is to be applied. |
| IEC104_ASDU_COT | Cause of transmission determines the meaning of information at the destination station. This information can be considered as the operation/command, e.g., spontaneous, activation, activation termination (see Appendinx C2 of [[1]](https://www.fit.vut.cz/research/publication-file/11570/TR-IEC104.pdf)). |
| IEC104_ASDU_ORG | Originator Address provides a means for a controlling station to explicitly identify itself.  |
| IEC104_ASDU_ADDRESS | The ASDU address (also labels as COA in the specification) represents a device address which combined with the information object address contained within the data itself stands for the system-wide unique address for each data element.|

## Towards Monitoring IEC104

An analysis in [[1]](https://www.fit.vut.cz/research/publication-file/11570/TR-IEC104.pdf)) provides the following observations:

* One TCP stream transmits several types of IEC format frames: U-frames, S-frames, I-frames. 
These frames have different format and usage for IEC communication. From point of view of 
network monitoring, it can be useful to keep statistics that include no. of packets, bytes, etc. 
for each of the frame formats.
* It is better to monitor transactions related to objects than individual packets. Each 
informational object is addressed by an IP address of the controlled station (on L3), by a 
controlled station address on L7 (common ASDU address, COA), and an object address (IOA). 
Thus, the transaction can be identified using a target address (COA+IOA) and action (COT, 
cause of transmission). Each transaction gets values or sets values of information elements 
that are part of the referred object. The standard defines which object type contains what kind 
of information elements. 
* Transactions are build by exchanges of ASDU messages between the slave and the controlling 
station. There is no transaction ID, thus slave and master have to check transactions based on 
COA, COT and OUI. If a message is lost, the loss is detected by L7 via ASDU sequence numbers 
and re-sent. 
* One ASDU can transmits several objects, however, these objects must have the same COT. 
* One TCP packet can contain several ASDUs with same or different COTs. 

Considering the above observations and based on the availability of information provided by IEC IPFIX template the following key fields were defined:

| Flow Key Fields |
| ----------- |
| L3_IPV4_SRC | 
| L3_IPV4_DST | 
| L4_PORT_SRC | 
| L4_PORT_DST |
| IEC104_ASDU_ADDRESS |  

L3 and L4 fields determines source and destination hosts of IEC conversations and together with IEC104_ASDU_ADDRESS they identify the IEC conversation. 

The column fields contains values that represents keys for the counters. As the number of possible values can be large we need to represent them compactly. 

| Column Fields |
| ---------------- |
| IEC104_FRAME_FMT |
| IEC104_ASDU_COT |
| IEC104_ASDU_TYPE_IDENT |

Given IEC fields determine the frame type, requested operation/result and the element types. In direct representation, for each valid 
combination of column field we provide the following counters:

| Value Fields | Aggregation Operation |
| ------------ | ---------------------- |
| BYTES | SUM |
| PACKETS | SUM |
| IEC104_PKT_LENGTH | SUM |
| IEC104_ASDU_NUM_ITEMS | SUM |


## Converting IEC IPFIX records to KMC input

The KMC method assumes that a single input record represents a conversation in terms of its statistical properties such as a number of different operations.
Given that flow is defined by Flow Key Fields the content of the input record consists of a collection of key-value pairs. The key is composed of Column Fields using concatenation. 
The value consists of a Value Fields structure comprised of four counters.

TODO: One-hot encoding?

TODO: Some other more compact representation?

## Example
In this section, an example is provided to demonostrate the describe principle of IEC IPFIX for KMC.

TODO: Provide an example.

## References

* https://www.fit.vut.cz/research/publication-file/11570/TR-IEC104.pdf


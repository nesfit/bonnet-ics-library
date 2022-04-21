# K-Means Anomaly Detection Method for IEC104

The K-Means Anomaly Detection method (KMC) is based on calculating clusters that model normal behavior. Each cluster is represented by its centroid and a radius of acceptance. If the observed pattern lies in a cluster, it is accepted as correct. Points that lie far from existing clusters are called anomalous.

In this document, the application of this method to IEC104 IPFIX is specified. The input to the method is a collection of flow records. The flow records are aggregated according to the key and selected fields represent features used for cluster computation. Appropriate features are aggregated values representing the numbers of observed requests. For instance, for HTTP flow, there will be a set of features, each of which will count a number of different request types, e.g., one for GET, another for POST, etc. As there may be many such features, the method adopts PCA preprocessing to reduce dimensionality. 

## IEC104 IPFIX Records

The IEC IPFIX records are not suitable as the input for KMC without preprocessing. According to the specification, the IPFIX record for IEC104 is computed for each ASDU and consists of the following fields: 
| Field | Meaning |
| -- | ------ |
| BYTES | Number of bytes in packets of the flow. |
| PACKETS | Number of packets in the flow.  |
| L3_IPV4_SRC | Source IP address. |
| L3_IPV4_DST | Destination IP adddress. |
| L4_PORT_SRC | Source TCP port. |
| L4_PORT_DST | Destination TCP port. |
| IEC104_PKT_LENGTH | The lenght of the packet (IEC?) |
| IEC104_FRAME_FMT | U-frames, S-frames, I-frames |
| IEC104_ASDU_TYPE_IDENT | Type identification stands for the type of the information objects carried in the message,e.g., single point information, bit string of 32 bit, file segment (see Appendinx C1 of [[1]](https://www.fit.vut.cz/research/publication-file/11570/TR-IEC104.pdf))   |
| IEC104_ASDU_NUM_ITEMS | A number of objects referenced in the message. Each object can correspond to a data element for which the given operation (COT) is to be applied. |
| IEC104_ASDU_COT | Cause of transmission determines the meaning of information at the destination station. This information can be considered as the operation/command, e.g., spontaneous, activation, activation termination (see Appendinx C2 of [[1]](https://www.fit.vut.cz/research/publication-file/11570/TR-IEC104.pdf)). |
| IEC104_ASDU_ORG | Originator Address provides a means for a controlling station to explicitly identify itself.  |
| IEC104_ASDU_ADDRESS | The ASDU address (also labels as COA in the specification) represents a device address which combined with the information object address contained within the data itself stands for the system-wide unique address for each data element.|


## Towards Monitoring IEC104

An analysis in [[1]](https://www.fit.vut.cz/research/publication-file/11570/TR-IEC104.pdf) provides the following observations:

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

The model of communication is created by aggregating IEC IPFIX records using the key fields that represents flow and computing statistics of
the operations. The key fields of flows are as follows:

| Flow Key Fields |
| ----------- |
| L3_IPV4_SRC | 
| L3_IPV4_DST | 
| L4_PORT_SRC | 
| L4_PORT_DST |
| IEC104_ASDU_ADDRESS |  

The flow key is given by L3 and L4 connectio identifiers and IEC104_ASDU_ADDRESS that identifies the IEC process. 

Next, the three IEC fields determine the frame type, cause of transmission and the element types, which together stands for the IEC operation requested or responsed. 
The all possible operations are represented by cartesian product of the following fields:

| Operation Fields |
| ---------------- |
| IEC104_FRAME_FMT |
| IEC104_ASDU_COT |
| IEC104_ASDU_TYPE_IDENT |

The last group of IPFIX fields is a collection of counters:

| Counter Fields |
| ------------ | 
| BYTES | 
| PACKETS | 
| IEC104_PKT_LENGTH |
| IEC104_ASDU_NUM_ITEMS | 

There are general counters of bytes and packets, and IEC-specific counters, which provide IEC packet length and the number of items affected by the operation. 

## Converting IEC IPFIX records to IEC KMC records

The preprocessing of IEC IPFIX records to corresponding elements suitable as the input for KMC is explained using an illustrative example.
Consider the following IEC IPFIX records:

| BYTES |  PACKETS |  L3_IPV4_SRC |  L3_IPV4_DST |  L4_PORT_SRC |  L4_PORT_DST |  IEC104_PKT_LENGTH |  IEC104_FRAME_FMT |  IEC104_ASDU_TYPE_IDENT |  IEC104_ASDU_NUM_ITEMS |  IEC104_ASDU_COT |  IEC104_ASDU_ORG |  IEC104_ASDU_ADDRESS |
| -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | 
| 112 | 1 | 10.20.100.108 | 10.20.102.1 | 2404 | 46413 | 26 | 0 | 3 | 4 | 20 | 0 | 10 |
| 476 | 1 | 10.20.100.108 | 10.20.102.1 | 2404 | 46413 | 34 | 0 | 11 | 4 | 20 | 0 | 10 |
| 520 | 1 | 10.20.100.108 | 10.20.102.1 | 2404 | 46413 | 42 | 0 | 7 | 4 | 20 | 0 | 10 |

The KMC method assumes that a single input record represents a conversation characterized by its statistical properties, such as a count of different operations.
To prepare a suitable input we identify the operation and statistical properties for every IEC IPFIX record. The operation fields represent categorical data. It is possible to 
obtain a single categorical value from these fields to represent *operation tag*. One option is to concatenate individual values possibly prepending it with a text prefix: 

```
IEC_OP = "X_" | IEC104_FRAME_FMT | IEC104_ASDU_COT | IEC104_ASDU_TYPE_IDENT
```

For instance, the first record will have the following operation tag:

```
IEC104_FRAME_FMT = 00 (i-frame)
IEC104_ASDU_COT = 03 (spontaneous)
IEC104_ASDU_TYPE_IDENT = 20 (packed single-point information with status change detection)
IEC_OP  = X_000320
```

The other two records will have operation tags `X_001120` and `X_000720`, respectively. 

Next we convert the IEC IPFIX records to the intermediate record as follows:

| BYTES | PACKETS |  L3_IPV4_SRC |  L3_IPV4_DST |  L4_PORT_SRC |  L4_PORT_DST |  X_000320_IEC104_PKT_LENGTH  | X_000320_IEC104_ASDU_NUM_ITEMS  | X_001120_IEC104_PKT_LENGTH  | X_001120_IEC104_ASDU_NUM_ITEMS  | X_000720_IEC104_PKT_LENGTH  | X_000720_IEC104_ASDU_NUM_ITEMS  |  IEC104_ASDU_ADDRESS |
| --    | --      | --           | --           | --           | --           | --   | -- | -- | -- | -- | --                              |  -- |
| 112   | 1       | 10.20.100.108| 10.20.102.1  | 2404         | 46413        | 26 | 4 | 0  | 0 | 0  | 0 | 10 |
| 476 | 1 | 10.20.100.108 | 10.20.102.1 | 2404 | 46413                        | 0  | 0 | 34 | 4 | 0  | 0 | 10 |
| 520 | 1 | 10.20.100.108 | 10.20.102.1 | 2404 | 46413                        | 0  | 0 | 0  | 0 | 42 | 4 | 10 |

In general, each IEC IPFIX record is transformed to an intermediate record by annotating selected counter fields with operation name (IEC_OP) string computed as above and 
by omitting unused fileds and operation fields. IEC IPFIX records that have the same operation will have the same fields, but records with different operations will be transformed to intermeidate records whose counter fields will have different annotations.  

To create an input for the KMC, we need to aggregate the individual IEC KMC records. An aggregation use 
`L3_IPV4_SRC`, `L3_IPV4_DST`, `L4_PORT_SRC`, `L4_PORT_DST` and `IEC104_ASDU_ADDRESS` as the compound key and applies SUM operation on other fields. It will yield to the following aggregated conversation record:

| BYTES | PACKETS |  L3_IPV4_SRC |  L3_IPV4_DST |  L4_PORT_SRC |  L4_PORT_DST |  X_000320_IEC104_PKT_LENGTH  | X_000320_IEC104_ASDU_NUM_ITEMS  | X_001120_IEC104_PKT_LENGTH  | X_001120_IEC104_ASDU_NUM_ITEMS  | X_000720_IEC104_PKT_LENGTH  | X_000720_IEC104_ASDU_NUM_ITEMS  | IEC104_ASDU_ADDRESS | 
| --    | --      | --           | --           | --           | --           | --   | -- | -- | -- | -- | --                              |  -- |
| 1108   | 3      | 10.20.100.108| 10.20.102.1  | 2404         | 46413        | 26 | 4 | 34  | 4 | 42  | 4 |  10 |

The KMC method can accept input that can have many feature columns as it preprocess the input with PCA to reduce dimensionality before clustering is perfomed. Thus, if there are tens or even hundreds of input feature columns the application of PCA reduce them to specified number of dimensions (default is 3).


## References

* https://www.fit.vut.cz/research/publication-file/11570/TR-IEC104.pdf

* https://towardsdatascience.com/encoding-categorical-variables-one-hot-vs-dummy-encoding-6d5b9c46e2db
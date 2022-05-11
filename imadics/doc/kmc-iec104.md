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
IEC_OP =  X_  | IEC104_FRAME_FMT | IEC104_ASDU_COT | IEC104_ASDU_TYPE_IDENT
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


## Evaluation

### Datasets

| Dataset | Description |
| ------- | ----------- |
| connection-loss | Connection lost twice during communication period. |
| dos-attack | Denial of service attack against a IEC 104 control station. |
| injection-attack | An attacker compromises one host and starts sending unusual requests. |
| normal-traffic | Normal IEC 104 communication (58930 packets, 2 days+19:55 hours traffic). |
| rogue-device | A rogue devices starts communicating with an IEC 104 host using legitimate IEC 104 packets. |
| scanning-attack | Horizontal (IP addresses) and vertical (IOA) scanning |
| switching-attack | Switching the device on/off. |

## Normal Traffic

The following table shows outliers of normal traffic. These are flows that are not matched by the learned profile.  

| Flow | Window | Period | Id | Features | Distances | Scores | Max | Min | Avg | Model |
| ---- | ------ | ------ | -- | ----- | -------- | ------ | --- | --- | --- | ----- |
| 192.168.11.248:2404-192.168.11.111:61254 | 2022-05-10T17:35:00 | 00:05:00 |  | [1.0050716,-0.085428834,-0.3713873] | [0.33117401599884033,0.19988322257995605,0.19505995512008667] | [0.5689585692021375,0,0] | 0.5689585692021375 | 0 | 0.1896528564007125 | 0 |
| 192.168.11.111:61254-192.168.11.248:2404 | 2022-05-11T01:10:00 | 00:05:00 |  | [-0.38772142,0.7191001,-0.11106855] | [0.27929291129112244,0.5919634103775024,0.18159154057502747] | [0,0,0] | 0 | 0 | 0 | 0 |
| 192.168.11.248:2404-192.168.11.111:61254 | 2022-05-11T01:10:00 | 00:05:00 |  | [0.20466681,0.7892811,0.004813969] | [0.7408515214920044,0.22953033447265625,0.2400776445865631] | [0.03574047357083365,0,0.004030402740745176] | 0.03574047357083365 | 0 | 0.013256958770526276 | 0 |
| 192.168.11.111:61254-192.168.11.248:2404 | 2022-05-11T03:45:00 | 00:05:00 |  | [-0.44219273,0.6094379,-0.12574953] | [0.17128005623817444,0.42717552185058594,0.0977572500705719] | [0.33140858703014353,0.18650250829105008,0.44619952797773477] | 0.44619952797773477 | 0.18650250829105008 | 0.3213702077663095 | 2 |
| 192.168.11.248:2404-192.168.11.111:61254 | 2022-05-11T03:45:00 | 00:05:00 |  | [0.30026305,0.6715432,-0.033313334] | [0.5110399127006531,0.12176918983459473,0.12887337803840637] | [0.3348530847115868,0.4673989871805764,0.4653647712872675] | 0.4673989871805764 | 0.3348530847115868 | 0.4225389477264769 | 1 |
| 192.168.11.111:61254-192.168.11.248:2404 | 2022-05-12T01:20:00 | 00:05:00 |  | [-0.44219273,0.6094379,-0.12574953] | [0.17128005623817444,0.42717552185058594,0.0977572500705719] | [0.33140858703014353,0.18650250829105008,0.44619952797773477] | 0.44619952797773477 | 0.18650250829105008 | 0.3213702077663095 | 2 |
| 192.168.11.111:61254-192.168.11.248:2404 | 2022-05-13T02:05:00 | 00:05:00 |  | [-0.44404465,0.69106954,-0.16570592] | [0.2511464059352875,0.5466586947441101,0.16083353757858276] | [0.019650424605881844,0,0.08886871343322766] | 0.08886871343322766 | 0 | 0.0361730460130365 | 2 |


## Connection Loss

There are two intervals of lost packets:

* connection loss from 16:27:57.68 to 16:37:48.63 (10 minutes 146 missing packets)
* connection loss from 08:08:01.20 to 09:08:25.95 (1 hour, 921 missing packets)

| Flow | Window | Period | Id | Features | Distances | Scores | Max | Min | Avg | Model |
| ---- | ------ | ------ | -- | ----- | -------- | ------ | --- | --- | --- | ----- |
|  192.168.11.111:61254-192.168.11.248:2404  |  2022-05-11T01:10:00  |  00:05:00  |    | [-0.38772142,0.7191001,-0.11106855] | [0.27929291129112244,0.5919634103775024,0.18159154057502747] | [0,0,0] | 0 | 0 |
|  192.168.11.248:2404-192.168.11.111:61254  |  2022-05-11T01:10:00  |  00:05:00  |    | [0.20466681,0.7892811,0.004813969] | [0.7408515214920044,0.22953033447265625,0.2400776445865631] | [0.03574047357083365,0,0.004030402740745176] | 0.03574047357083365 | 0 |
|  192.168.11.111:61254-192.168.11.248:2404  |  2022-05-11T16:25:00  |  00:05:00  |    | [-0.3410402,-0.0832013,0.31581393] | [0.17917418479919434,0.16869908571243286,0.17955631017684937] | [0,0.6787356108652249,0] | 0.6787356108652249 | 0 | 0.22624520362174164 | 1 |
|  192.168.11.111:61254-192.168.11.248:2404  |  2022-05-11T16:35:00  |  00:05:00  |    | [-0.25470334,-0.12278709,0.4377263] | [0.32110515236854553,0.31887108087539673,0.3240521550178528] | [0,0.3927535375929426,0] | 0.3927535375929426 | 0 | 0.1309178458643142 | 1 |
|  192.168.11.248:2404-192.168.11.111:61254  |  2022-05-11T16:35:00  |  00:05:00  |    | [0.29199857,-0.13445753,0.38856018] | [0.25340327620506287,0.2461678385734558,0.25204524397850037] | [0.6701815194804452,0,0] | 0.6701815194804452 | 0 | 0.22339383982681507 | 0 |
|  192.168.11.111:61254-192.168.11.248:2404  |  2022-05-12T09:05:00  |  00:05:00  |    | [-0.17207046,0.0008904934,0.4797259] | [0.43215930461883545,0.40871626138687134,0.41804903745651245] | [0,0.22165565101088602,0] | 0.22165565101088602 | 0 | 0.07388521700362867 | 1 |
|  192.168.11.248:2404-192.168.11.111:61254  |  2022-05-12T09:05:00  |  00:05:00  |    | [0.10265805,0.019386647,0.5047711] | [0.47910410165786743,0.37197816371917725,0.3697008192539215] | [0.3764193218575008,0,0] | 0.3764193218575008 | 0 | 0.1254731072858336 | 0 |
|  192.168.11.111:61254-192.168.11.248:2404  |  2022-05-13T02:05:00  |  00:05:00  |    | [-0.44404465,0.69106954,-0.16570592] | [0.2511464059352875,0.5466586947441101,0.16083353757858276] | [0.019650424605881844,0,0.08886871343322766] | 0.08886871343322766 | 0 | 0.0361730460130365 | 2 |


## Injection Attack

The attacker sends ASDUs with TypeID=45 (Single command) and CoT=6 (Activation) on the object with IOA=31, 32 and 2. The host responses with CoT=7 (Activation Conf). The attack starts at 19:35:19 and ends at 19:41:06. It includes 83 packets.

Another injection attack appears at 21:05:32 when an attacker starts to transfer a file to the compromised host with IP address 192.168.11.111. The attacker sends messages with ASDU typeID=122 (Call directory, select file), 120 (File ready), 121 (Section ready), 123 (Last section), 124 (Ack file), 125 (Segment). The attacker accesses object with IOA=65537 which is not typically accessible. The attack includes 221 messages and ends at 21:21:14. 
 
| Flow | Window | Period | Id | Features | Distances | Scores | Max | Min | Avg | Model |
| ---- | ------ | ------ | -- | ----- | -------- | ------ | --- | --- | --- | ----- |
|  192.168.11.111:61254-192.168.11.248:2404  |  2022-05-11T01:10:00  |  00:05:00  |    | [-0.38772142,0.7191001,-0.11106855] | [0.27929291129112244,0.5919634103775024,0.18159154057502747] | [0,0,0] | 0 | 0 | 0 | 0 |
|  192.168.11.248:2404-192.168.11.111:61254  |  2022-05-11T01:10:00  |  00:05:00  |    | [0.20466681,0.7892811,0.004813969] | [0.7408515214920044,0.22953033447265625,0.2400776445865631] | [0.03574047357083365,0,0.004030402740745176] | 0.03574047357083365 | 0 | 0.013256958770526276 | 0 |
|  192.168.11.248:2404-192.168.11.111:61254  |  2022-05-11T21:05:00  |  00:05:00  |    | [0.03617227,-0.06078623,0.5508733] | [0.6016594767570496,0.5132524371147156,0.5102669596672058] | [0.21690667387576668,0,0] | 0.21690667387576668 | 0 | 0.07230222462525555 | 0 |
|  192.168.11.111:61254-192.168.11.248:2404  |  2022-05-11T21:05:00  |  00:05:00  |    | [-0.22654174,-0.10877179,0.46504495] | [0.3679368495941162,0.36060893535614014,0.3722448945045471] | [0,0.3132694890165967,0] | 0.3132694890165967 | 0 | 0.10442316300553223 | 1 |
|  192.168.11.248:2404-192.168.11.111:61254  |  2022-05-11T21:10:00  |  00:05:00  |    | [-0.085733645,-0.038695287,0.60163826] | [0.6541699171066284,0.6213729381561279,0.6352929472923279] | [0,0,0] | 0 | 0 | 0 | 0 |
|  192.168.11.111:61254-192.168.11.248:2404  |  2022-05-11T21:10:00  |  00:05:00  |    | [-0.1702185,-0.08074119,0.5196823] | [0.4720152020454407,0.45449966192245483,0.4691818952560425] | [0,0.13446741200258783,0] | 0.13446741200258783 | 0 | 0.04482247066752928 | 1 |
|  192.168.11.111:61254-192.168.11.248:2404  |  2022-05-11T21:15:00  |  00:05:00  |    | [-0.19838011,-0.094756484,0.49236363] | [0.4182402193546295,0.40581849217414856,0.4207547903060913] | [0,0.22717405706535543,0] | 0.22717405706535543 | 0 | 0.07572468568845181 | 1 |
|  192.168.11.248:2404-192.168.11.111:61254  |  2022-05-11T21:15:00  |  00:05:00  |    | [-0.085733645,-0.038695287,0.60163826] | [0.6541699171066284,0.6213729381561279,0.6352929472923279] | [0,0,0] | 0 | 0 | 0 | 0 |

## Scanning Attack

Two types of scanning attacks are included:

* horizontal scanning starts at 10:32:07 and ends at 10:49:10. The attacker employs a spoofed IP address 192.168.11.102:45280. The scan sends IEC 104 U-commands TestFrame Act (ApduType 0x03, UType 0x10) on port 2404 (used by IEC 104). If a station exists, the scan yields a proper response TestFrame Conf (ApduType 0x03, UType 0x20).
* vertical scanning attack - explores IEC 104 information objects on the device with IP address 192.168.11.111. In order to masquerade his identity, the attacker uses a spoofed source address 192.168.11.248 which belongs to the existing node. The attacker sends an interrogation command with TypeID=100 (General Interrogation) and CoT=6 (Activation). If an object exists, it responses with TypeID=100 and CoT=7 (Activation Conf), otherwise it sends a message with CoT=47 (unknown object address). For vertical scanning we use a default ASDU address 65535 (Global address) and a default value for the Originator address (OA = 0, not used). The IOA length is limited to 2 bytes, however, our scan tests only values from 1 to 127. The attack starts at 01:02:18 and ends at 01:23:19. 

| Flow | Window | Period | Id | Features | Distances | Scores | Max | Min | Avg | Model |
| ---- | ------ | ------ | -- | -------- | --------- | ------ | --- | --- | --- | ----- |
| 192.168.11.248:2404-192.168.11.111:61254 | 2022-05-10T17:35:00 | 00:05:00 |  | [1.0050716,-0.085428834,-0.3713873] | [0.33117401599884033,0.19988322257995605,0.19505995512008667] | [0.5689585692021375,0,0] | 0.5689585692021375 | 0 | 0.1896528564007125 | 0 |
| 192.168.11.111:61254-192.168.11.248:2404 | 2022-05-11T01:10:00 | 00:05:00 |  | [-0.38772142,0.7191001,-0.11106855] | [0.27929291129112244,0.5919634103775024,0.18159154057502747] | [0,0,0] | 0 | 0 | 0 | 0 |
| 192.168.11.248:2404-192.168.11.111:61254 | 2022-05-11T01:10:00 | 00:05:00 |  | [0.20466681,0.7892811,0.004813969] | [0.7408515214920044,0.22953033447265625,0.2400776445865631] | [0.03574047357083365,0,0.004030402740745176] | 0.03574047357083365 | 0 | 0.013256958770526276 | 0 |
| 192.168.11.102:45280-192.168.11.1:2404 | 2022-05-11T10:30:00 | 00:05:00 |  | [-0.031262368,0.070966996,0.6163192] | [0.6847476363182068,0.5418599843978882,0.5423175096511841] | [0,0,0] | 0 | 0 | 0 | 0 |
| 192.168.11.111:61254-192.168.11.248:2404 | 2022-05-13T02:05:00 | 00:05:00 |  | [-0.44404465,0.69106954,-0.16570592] | [0.2511464059352875,0.5466586947441101,0.16083353757858276] | [0.019650424605881844,0,0.08886871343322766] | 0.08886871343322766 | 0 | 0.0361730460130365 | 2 |




## References

* https://www.fit.vut.cz/research/publication-file/11570/TR-IEC104.pdf

* https://towardsdatascience.com/encoding-categorical-variables-one-hot-vs-dummy-encoding-6d5b9c46e2db
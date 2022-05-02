# K-Means Anomaly Detection Method for GOOSE

The K-Means Anomaly Detection method (KMC) is based on calculating clusters that model normal behavior. Each cluster is represented by its centroid and a radius of acceptance. If the observed pattern lies in a cluster, it is accepted as correct. Points that lie far from existing clusters are called anomalous.

In this document, the application of this method to GOOSE IPFIX is specified. GOOSE messages are “published” by a device via Ethernet multicast such that they can be “subscribed” by any number of other devices.
The GOOSE message is multicasted from the originating IED and received by the IEDs which have been configured to subscribe to it. GOOSE messages are immediately created once an event, such as the triggering of a circuit breaker, occurs within the substation and they are sent periodically through the network.

GOOSE messages are based on datasets. While IEC 61850 allows any dataset to be used with GOOSE, the practical uses suggest that the datasets should contain small numbers of status values and their related quality information. A change in the value of any dataset member is considered a change of “state”, the new information is published immediately. Even if there is not any change the message is periodically transmitted in case of lost packets or devices coming online which need the current state. In order to preserve bandwidth, the delay between retransmissions grows over time from minTime immediately following the state change to maxTime at the steady-state condition.

GOOSE sends status values in data sets. Data sets are used to define a list of variables that can be transferred via GOOSE.
Data sets are referenced by Report Control Blocks (RCB). Each RCB is related to a specific data set that is monitored by this RCB on the IED.

## GOOSE IPFIX Records

The IPFIX GOOSE consists of the following fields:

| Field | Meaning |
| --             | ------ |
| BYTES          | Number of bytes in packets of the flow. |
| PACKETS        | Number of packets in the flow.  |
| L3_IPV6_SRC    | Source IP address. |
| L3_IPV6_DST    | Destination IP adddress. |
| GOOSE_APPID    | It can be used to determine the application.    |
| GOOSE_CB_REF   | GOOSE control block specifies which data set it controls. |
| GOOSE_DATA_SET | The name of the GOOSE data set in the IED.   |
| GOOSE_ID       | goID is an identifier associated with the IED. |
| GOOSE_ST_NUM   | It is a counter that increments for each event causing the GOOSE message, thus, this value should be changed when the status is changed. |

## Towards Monitoring GOOSE communication

The KMC model of communication is created by aggregating GOOSE IPFIX records using the key fields that identify each flow and statistics of the operations. GOOSE has a single operation, which represents the dissemination of state values for data sets of IED. 
The key fields of flows are as follows:

| Flow Key Fields |
| --------------- |
| L3_IPV6_SRC     | 
| L3_IPV6_DST     | 
| GOOSE_APPID     |
| GOOSE_CB_REF    |  

The flow key is given by L3 connection identifiers and `GOOSE_APPID` that identifies the IED sender device.

Next, the three GOOSE fields determine the data that are published:

| Operation Fields |
| ---------------- |
| GOOSE_CB_REF |
| GOOSE_DATA_SET |
| GOOSE_ID |

The last group of IPFIX fields is a collection of counters:

| Counter Fields |
| -------------- | 
| BYTES          | 
| PACKETS        | 

GOOSE IFPIX provides general counters that stand for bytes and packets. We omit `GOOGLE_ST_NUM` field. However, it may be used to determine the status change. For this we can apply `COUNT DISTINCT` aggregator to find the number of changes (1 indicating no change).

## Converting GOOSE IPFIX records to GOOSE KMC records

The preprocessing of GOOSE IPFIX records to corresponding elements suitable as the input for KMC is explained using an illustrative example.
Consider the following GOOSE IPFIX records:

| BYTES | PACKETS | START_SEC | END_SEC | L3_PROTO | BYTES_A | PACKETS_A | L3_IPV6_SRC | L3_IPV6_DST | GOOSE_APPID | GOOSE_CB_REF | GOOSE_DATA_SET | GOOSE_ID | GOOSE_ST_NUM |
| ----- | ------- | --------- | ------- | -------- | ------- | --------- | ----------- | ----------- | ----------- | ------------ | -------------- | -------- | ------------ | 
| 2072 | 14 | 2021-11-26 14:39:21.535568830 | 2021-11-26 14:40:20.934397455 | 6 | 2072 | 14 | fe80::221:c1ff:fe25:8a2 | ff02::10c:cd01:0:0 | 1 | AA1J1Q01A1LD0/LLN0$GO$LEDs_info | AA1J1Q01A1LD0/LLN0$LEDs_ON_OFF | AA1J1Q01A1LD0/LLN0.LEDs_info | 1 |
| 12420 | 60 | 2021-11-26 14:39:48.656617832 | 2021-11-26 14:40:46.659219514 | 6 | 12420 | 60 | fe80::221:c1ff:fe25:8a2 | ff02::10c:cd01:0:1 | 1 | ASNERIES1_CALApplication/LLN0$GO$Control_DataSet_2 | ASNERIES1_CALApplication/LLN0$DataSet_2 | ASNERIES1_CAL/Application/LLN0/Control_DataSet_2 | 1 |
| 2072 | 14 | 2021-11-26 14:40:30.834047613 | 2021-11-26 14:41:30.235201636 | 6 | 2072 | 14 | fe80::221:c1ff:fe25:8a2 | ff02::10c:cd01:0:0 | 1 | AA1J1Q01A1LD0/LLN0$GO$LEDs_info | AA1J1Q01A1LD0/LLN0$LEDs_ON_OFF | AA1J1Q01A1LD0/LLN0.LEDs_info | 1 |
| 12420 | 60 | 2021-11-26 14:40:48.658606924 | 2021-11-26 14:41:46.666262985 | 6 | 12420 | 60 | fe80::221:c1ff:fe25:8a2 | ff02::10c:cd01:0:1 | 1 | ASNERIES1_CALApplication/LLN0$GO$Control_DataSet_2 | ASNERIES1_CALApplication/LLN0$DataSet_2 | ASNERIES1_CAL/Application/LLN0/Control_DataSet_2 | 1 |
| 12420 | 60 | 2021-11-26 14:41:48.665979860 | 2021-11-26 14:42:46.667508761 | 6 | 12420 | 60 | fe80::221:c1ff:fe25:8a2 | ff02::10c:cd01:0:1 | 1 | ASNERIES1_CALApplication/LLN0$GO$Control_DataSet_2 | ASNERIES1_CALApplication/LLN0$DataSet_2 | ASNERIES1_CAL/Application/LLN0/Control_DataSet_2 | 1 |
 
The KMC method assumes that a single input record represents a conversation characterized by its statistical properties, such as a count of different data set transmissions.
To prepare a suitable input we identify the data set and statistical properties for every GOOSE IPFIX record. The data set related fields to represent categorical data. It is possible to 
obtain a single categorical value from these fields to represent *data set tag*. One option is to concatenate individual values and apply a suitable hash function to get a short tag string:

```
GOOSE_DS = HASH( GOOSE_CB_REF | GOOSE_DATA_SET | GOOSE_ID )
```

Possible hash function can be [MurmurHash3](https://en.wikipedia.org/wiki/MurmurHash) function. For instance, the first record will have the following data set tag:

```
GOOSE_CB_REF = AA1J1Q01A1LD0/LLN0$GO$LEDs_info
GOOSE_DATA_SET = AA1J1Q01A1LD0/LLN0$LEDs_ON_OFF
GOOSE_ID = AA1J1Q01A1LD0/LLN0.LEDs_info
GOOSE_DS  = D78C4C3D
```

The second record will have `GOOSE_DS=F530A646`. These value were computed using [on-line implementation of MurmurHash3](http://murmurhash.shorelabs.com/).
Next we convert the IEC IPFIX records to the intermediate record as follows:

| BYTES | PACKETS | START_SEC | END_SEC | L3_PROTO | BYTES_A | PACKETS_A | L3_IPV6_SRC | L3_IPV6_DST | GOOSE_APPID | D78C4C3D_BYTES | D78C4C3D_PACKETS | F530A646_BYTES | F530A646_PACKETS |
| ----- | ------- | --------- | ------- | -------- | ------- | --------- | ----------- | ----------- | ----------- | -------------- | ---------------- | -------------- | ---------------- | 
| 2072 | 14 | 2021-11-26 14:39:21.535568830 | 2021-11-26 14:40:20.934397455 | 6 | 2072 | 14 | fe80::221:c1ff:fe25:8a2 | ff02::10c:cd01:0:0 | 1 | 2072 | 14 | 0 | 0 |
| 12420 | 60 | 2021-11-26 14:39:48.656617832 | 2021-11-26 14:40:46.659219514 | 6 | 12420 | 60 | fe80::221:c1ff:fe25:8a2 | ff02::10c:cd01:0:1 | 1 | 0 | 0 | 12420 | 60 |
| 2072 | 14 | 2021-11-26 14:40:30.834047613 | 2021-11-26 14:41:30.235201636 | 6 | 2072 | 14 | fe80::221:c1ff:fe25:8a2 | ff02::10c:cd01:0:0 | 1 | 2072 | 14 | 0 | 0 |
| 12420 | 60 | 2021-11-26 14:40:48.658606924 | 2021-11-26 14:41:46.666262985 | 6 | 12420 | 60 | fe80::221:c1ff:fe25:8a2 | ff02::10c:cd01:0:1 | 1 | 0 | 0 | 12420 | 60 |
| 12420 | 60 | 2021-11-26 14:41:48.665979860 | 2021-11-26 14:42:46.667508761 | 6 | 12420 | 60 | fe80::221:c1ff:fe25:8a2 | ff02::10c:cd01:0:1 | 1 | 0 | 0 | 12420 | 60 |

In general, each GOOSE IPFIX record is transformed to an intermediate record by annotating selected counter fields with data set tag string computed as above and 
by omitting unused fileds and operation fields.  

To create an input for the KMC, we need to aggregate the individual GOOSE KMC records. An aggregation use 
`L3_IPV6_SRC`, `L3_IPV6_DST` and `GOOSE_APPID` as the compound key and applies SUM operation on other fields. It will yield to the following aggregated conversation records:

| BYTES | PACKETS | START_SEC | END_SEC | L3_PROTO | BYTES_A | PACKETS_A | L3_IPV6_SRC | L3_IPV6_DST | GOOSE_APPID | D78C4C3D_BYTES | D78C4C3D_PACKETS | F530A646_BYTES | F530A646_PACKETS |
| ----- | ------- | --------- | ------- | -------- | ------- | --------- | ----------- | ----------- | ----------- | -------------- | ---------------- | -------------- | ---------------- | 
| 4144 | 28 | 2021-11-26 14:39:21.535568830 | 2021-11-26 14:42:46.667508761 | 6 | 41404 | 28 | fe80::221:c1ff:fe25:8a2 | ff02::10c:cd01:0:0 | 1 | 4144 | 28 | 0 | 0 |
| 37260 | 180 | 2021-11-26 14:39:21.535568830 | 2021-11-26 14:42:46.667508761 | 6 | 37260 | 180 | fe80::221:c1ff:fe25:8a2 | ff02::10c:cd01:0:1 | 1 | 0 | 0 | 37260 | 180 |

The KMC method can accept input that can have many feature columns as it preprocess the input with PCA to reduce dimensionality before clustering is perfomed. Thus, if there are tens or even hundreds of input feature columns the application of PCA reduce them to specified number of dimensions (default is 3).


## References

* https://www.fit.vut.cz/research/publication-file/11832/TR-61850.pdf

* https://towardsdatascience.com/encoding-categorical-variables-one-hot-vs-dummy-encoding-6d5b9c46e2db
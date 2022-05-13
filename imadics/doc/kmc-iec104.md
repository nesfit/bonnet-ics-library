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
by omitting unused fileds and operation fields. IEC IPFIX records that have the same operation will have the same fields, but records with different operations will be transformed to intermediate records whose counter fields will have different annotations.  

To create an input for the KMC, we need to aggregate the individual IEC KMC records. An aggregation use 
`L3_IPV4_SRC`, `L3_IPV4_DST`, `L4_PORT_SRC`, `L4_PORT_DST` and `IEC104_ASDU_ADDRESS` as the compound key and applies SUM operation on other fields. It will yield to the following aggregated conversation record:

| BYTES | PACKETS |  L3_IPV4_SRC |  L3_IPV4_DST |  L4_PORT_SRC |  L4_PORT_DST |  X_000320_IEC104_PKT_LENGTH  | X_000320_IEC104_ASDU_NUM_ITEMS  | X_001120_IEC104_PKT_LENGTH  | X_001120_IEC104_ASDU_NUM_ITEMS  | X_000720_IEC104_PKT_LENGTH  | X_000720_IEC104_ASDU_NUM_ITEMS  | IEC104_ASDU_ADDRESS | 
| --    | --      | --           | --           | --           | --           | --   | -- | -- | -- | -- | --                              |  -- |
| 1108   | 3      | 10.20.100.108| 10.20.102.1  | 2404         | 46413        | 26 | 4 | 34  | 4 | 42  | 4 |  10 |

The KMC method can accept input that can have many feature columns as it preprocess the input with PCA to reduce dimensionality before clustering is perfomed. Thus, if there are tens or even hundreds of input feature columns the application of PCA reduce them to specified number of dimensions (default is 3).


## Evaluation

For the evaluation, we use the dataset that contains a single trace of normal traffic and several attacks. Each trace is almost 70 hours of system communication. The system is set to use 5 minutes time window for aggregating IEC flows. This interval is used for training the model as well as for detecting the anomalies. The KMC method performs scoring of tested flows. The score threshold is set to 0.1 that is the score less than this value indicates the anomaly. The traffic samples starts at 2022-05-13T14:40:00 and ends at 2022-05-16T10:35:00.

Three KMC methods are evaluated:

* Centroids - this is KMC method, in which the input features corresponds directly to computed the computed features for all identified operations. No preprocessing except normalization is made to these features.

* Centroids/Pca - this is KMC methods, which applies PCA reduction to the input features producing less dimensionality feature space (default=3 dimensions) .

* Centroids/Avg - this is KMC method that uses 4-feature input. Features are MIN, MAX, AVG and STDEV values computed from `IEC104_PKT_LENGTH` vector.

### Datasets

| Dataset | Description | Source Files
| ------- | ----------- | ------------ | 
| normal-traffic | Normal IEC 104 communication (58930 packets, 2 days+19:55 hours traffic). | [normal-traffic.csv](https://github.com/nesfit/bonnet-ics-library/blob/main/imadics/data/iec/bonnet/normal-traffic.csv)|
| connection-loss | Connection lost twice during communication period. |  [connection-loss.csv](https://github.com/nesfit/bonnet-ics-library/blob/main/imadics/data/iec/bonnet/connection-loss.csv)|
| dos-attack | Denial of service attack against a IEC 104 control station. |  [dos-attack.csv](https://github.com/nesfit/bonnet-ics-library/blob/main/imadics/data/iec/bonnet/dos-attack.csv)|
| injection-attack | An attacker compromises one host and starts sending unusual requests. |  [injection-attack.csv](https://github.com/nesfit/bonnet-ics-library/blob/main/imadics/data/iec/bonnet/injection-attack.csv)|
| rogue-device | A rogue devices starts communicating with an IEC 104 host using legitimate IEC 104 packets. |  [rogue-device.csv](https://github.com/nesfit/bonnet-ics-library/blob/main/imadics/data/iec/bonnet/rogue-device.csv)|
| scanning-attack | Horizontal (IP addresses) and vertical (IOA) scanning. |  [scanning-attack.csv](https://github.com/nesfit/bonnet-ics-library/blob/main/imadics/data/iec/bonnet/scanning-attack.csv)|
| switching-attack | Switching the device on/off. |  [switching-attack.csv](https://github.com/nesfit/bonnet-ics-library/blob/main/imadics/data/iec/bonnet/switching-attack.csv)|

## Normal Traffic

The normal traffic consists of 1630 aggregated flows. All these flows are used to learn the profile. We tested the learned profile against the same input dataset to determine its coverage. In an ideal case, there should not be any anomalies detected. But for all tested sub-methods some false positives appeared as shown below.

An aggregated flows representing the input to the method are available at:
[normal-traffic.dump.flows.csv](https://github.com/nesfit/bonnet-ics-library/blob/main/imadics/data/iec/bonnet/normal-traffic.dump.flows.csv)


### Centroids

Scored flow records: [normal-traffic.dir.score.csv](https://github.com/nesfit/bonnet-ics-library/blob/main/imadics/data/iec/bonnet/normal-traffic.dir.score.csv)

Anomaly flows detected: [normal-traffic.dir.t0.1.csv](https://github.com/nesfit/bonnet-ics-library/blob/main/imadics/data/iec/bonnet/normal-traffic.dir.t0.1.csv)

|FlowKey                                   |WindowStart          |WindowDuration|FlowLabel|Features                 |Distances                                           |Scores |MaxScore|MinScore|AverageScore|BestModel|
|------------------------------------------|---------------------|--------------|---------|-------------------------|----------------------------------------------------|-------|--------|--------|------------|---------|
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-13T17:35:00"|"00:05:00"    |"2983"   |[0,0.36363637,1,0,0,0,1,0]|[0.23264455795288086,0.23264479637145996,0.18967878818511963]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T01:10:00"|"00:05:00"    |"9527"   |[0,1,0,0.5,0,0,0,0]      |[0.5919825434684753,0.2386244535446167,0.5919825434684753]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-14T01:10:00"|"00:05:00"    |"9528"   |[0,1,0.24484538,0,0,0,0.24358974,0]|[0.2943263053894043,0.29432618618011475,0.14597368240356445]|[0.08968577929433941,0.08968614799366914,0.059347376086522186]|0.08968614799366914|0.059347376086522186|0.07957310112484357|1        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-16T02:05:00"|"00:05:00"    |"51168"  |[0,1,0,0.5833334,0,0,0,0]|[0.5466819405555725,0.21409475803375244,0.5466819405555725]|[0,0.05671194109938471,0]|0.05671194109938471|0       |0.01890398036646157|1        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-16T10:35:00"|"00:05:00"    |"58903"  |[0,0,0.24484538,0,0,0,0.24358974,0]|[0.3368363380432129,0.3368365168571472,0.2647618055343628]|[0,0,0]|0       |0       |0           |0        |

### Centroids/Pca

Scored flow records: [normal-traffic.pca.score.csv](https://github.com/nesfit/bonnet-ics-library/blob/main/imadics/data/iec/bonnet/normal-traffic.pca.score.csv)

Anomaly flows detected: [normal-traffic.pca.t0.1.csv](https://github.com/nesfit/bonnet-ics-library/blob/main/imadics/data/iec/bonnet/normal-traffic.pca.t0.1.csv)

|FlowKey                                   |WindowStart          |WindowDuration|FlowLabel|Features                 |Distances                                           |Scores |MaxScore|MinScore|AverageScore|BestModel|
|------------------------------------------|---------------------|--------------|---------|-------------------------|----------------------------------------------------|-------|--------|--------|------------|---------|
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-13T17:35:00"|"00:05:00"    |"2983"   |[-1.0050718,-0.085428596,-0.3713619]|[0.23257899284362793,0.19631177186965942,0.18962126970291138]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T01:10:00"|"00:05:00"    |"9527"   |[0.38772142,0.71910024,-0.11106318]|[0.5919620990753174,0.5919620990753174,0.2221955955028534]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-14T01:10:00"|"00:05:00"    |"9528"   |[-0.20466684,0.78928125,0.0048155785]|[0.294316828250885,0.23552128672599792,0.145033061504364]|[0.08960588294943272,0,0.040495956562130786]|0.08960588294943272|0       |0.043367279837187835|0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-16T02:05:00"|"00:05:00"    |"51168"  |[0.4440447,0.69106966,-0.16569775]|[0.5466556549072266,0.5466555953025818,0.19855552911758423]|[0,0,0.0675074943277274]|0.0675074943277274|0       |0.02250249810924247|2        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-16T10:35:00"|"00:05:00"    |"58903"  |[-0.22503851,-0.10866751,0.44431]|[0.33675944805145264,0.331036776304245,0.26635390520095825]|[0,0,0]|0       |0       |0           |0        |

### Centroids/Avg

Scored flow records: [normal-traffic.avg.score.csv](https://github.com/nesfit/bonnet-ics-library/blob/main/imadics/data/iec/bonnet/normal-traffic.avg.score.csv)

Anomaly flows detected: [normal-traffic.avg.t0.1.csv](https://github.com/nesfit/bonnet-ics-library/blob/main/imadics/data/iec/bonnet/normal-traffic.avg.t0.1.csv)

|FlowKey                                   |WindowStart          |WindowDuration|FlowLabel|Features                            |Distances                                                   |Scores                                     |MaxScore           |MinScore|AverageScore       |BestModel|
|------------------------------------------|---------------------|--------------|---------|------------------------------------|------------------------------------------------------------|-------------------------------------------|-------------------|--------|-------------------|---------|
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-13T17:35:00"|"00:05:00"    |"2983"   |[0,1940,489,837.7607]               |[450425,246177.5,94173.5]                                   |[0,0,0.006830079240766285]                 |0.006830079240766285|0       |0.0022766930802554284|2        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-16T10:35:00"|"00:05:00"    |"58902"  |[0,36,9,15.588457]                  |[2255.67041015625,2255.67041015625,2255.67041015625]        |[0,0,0]                                    |0                  |0       |0                  |0        |

## Connection Loss

There are two intervals of lost packets:

* connection loss from 16:27:57.68 to 16:37:48.63 (10 minutes 146 missing packets)
* connection loss from 08:08:01.20 to 09:08:25.95 (1 hour, 921 missing packets)

An aggregated flows representing the input to the method are available at:
[connection-loss.dump.flows.csv](https://github.com/nesfit/bonnet-ics-library/blob/main/imadics/data/iec/bonnet/connection-loss.dump.flows.csv)

### Centroids

|FlowKey                                   |WindowStart          |WindowDuration|FlowLabel|Features                 |Distances                                           |Scores |MaxScore|MinScore|AverageScore|BestModel|
|------------------------------------------|---------------------|--------------|---------|-------------------------|----------------------------------------------------|-------|--------|--------|------------|---------|
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-13T17:35:00"|"00:05:00"    |"2983"   |[0,0.36363637,1,0,0,0,1,0]|[0.23264455795288086,0.2326446771621704,0.2326444387435913]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T01:10:00"|"00:05:00"    |"9527"   |[0,1,0,0.5,0,0,0,0]      |[0.5919825434684753,0.1259913444519043,0.10451245307922363]|[0,0.04624162886365191,0.07660202810260941]|0.07660202810260941|0       |0.040947885655420437|2        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-14T01:10:00"|"00:05:00"    |"9528"   |[0,1,0.24484538,0,0,0,0.24358974,0]|[0.29432469606399536,0.29432618618011475,0.2943263053894043]|[0.08968900499957788,0.0896861844882404,0.08968603475634962]|0.08968900499957788|0.08968603475634962|0.08968707474805597|0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-14T16:35:00"|"00:05:00"    |"22553"  |[0,0,0.30927837,0,0,0,0.30769232,0]|[0.2987085282802582,0.2987067997455597,0.2987067997455597]|[0.07613033758203502,0.07613749858707342,0.07613772081341652]|0.07613772081341652|0.07613033758203502|0.07613518566084165|2        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-15T09:05:00"|"00:05:00"    |"35069"  |[0,0.09090909,0,0.20833334,0,0,0,0]|[0.40880802273750305,0.408390611410141,0.3999834954738617]|[0.22150965576088455,0,0]|0.22150965576088455|0       |0.07383655192029485|0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-15T09:05:00"|"00:05:00"    |"35070"  |[0,0.09090909,0.12886599,0,0,0,0.12820514,0]|[0.3605960011482239,0.3605945110321045,0.36059457063674927]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-16T10:35:00"|"00:05:00"    |"57836"  |[0,0,0.24484538,0,0,0,0.24358974,0]|[0.3368380665779114,0.3368363082408905,0.33683639764785767]|[0,0,0]|0       |0       |0           |0        |


### Centroids/Pca

|FlowKey                                   |WindowStart          |WindowDuration|FlowLabel|Features                 |Distances                                           |Scores |MaxScore|MinScore|AverageScore|BestModel|
|------------------------------------------|---------------------|--------------|---------|-------------------------|----------------------------------------------------|-------|--------|--------|------------|---------|
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-13T17:35:00"|"00:05:00"    |"2983"   |[1.0050719,-0.0854278,0.3713717]|[0.23258674144744873,0.23258650302886963,0.1966172456741333]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T01:10:00"|"00:05:00"    |"9527"   |[-0.38772207,0.7190999,0.11107075]|[0.5919633507728577,0.22219565510749817,0.23861581087112427]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-14T01:10:00"|"00:05:00"    |"9528"   |[0.20466621,0.7892814,-0.0048128963]|[0.29431718587875366,0.2943171262741089,0.23552152514457703]|[0.08961430479007315,0.08961430663267045,0]|0.08961430663267045|0       |0.05974287047424787|1        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-14T16:35:00"|"00:05:00"    |"22553"  |[0.2919988,-0.1344572,-0.38855344]|[0.29865700006484985,0.298657089471817,0.25074854493141174]|[0.07619033587335922,0.07618987409980293,0]|0.07619033587335922|0       |0.050793403324387386|0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-15T09:05:00"|"00:05:00"    |"35069"  |[-0.17207044,0.00089033693,-0.4797135]|[0.40870797634124756,0.44011440873146057,0.4410706162452698]|[0.22167088458097028,0,0]|0.22167088458097028|0       |0.07389029486032343|0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-15T09:05:00"|"00:05:00"    |"35070"  |[0.10265812,0.019386772,-0.50475997]|[0.36050844192504883,0.3605085015296936,0.3710561990737915]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-16T02:05:00"|"00:05:00"    |"50101"  |[-0.44404534,0.6910692,0.16570711]|[0.5466580390930176,0.1985565423965454,0.21408236026763916]|[0,0.06750527835379061,0.05672224576806695]|0.06750527835379061|0       |0.041409174707285855|1        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-16T10:35:00"|"00:05:00"    |"57836"  |[0.22503856,-0.1086671,-0.4443257]|[0.3367701768875122,0.336770236492157,0.33475157618522644]|[0,0,0]|0       |0       |0           |0        |


### Centroids/Avg

|FlowKey                                   |WindowStart          |WindowDuration|FlowLabel|Features                            |Distances                                                   |Scores                                     |MaxScore           |MinScore|AverageScore       |BestModel|
|------------------------------------------|---------------------|--------------|---------|------------------------------------|------------------------------------------------------------|-------------------------------------------|-------------------|--------|-------------------|---------|
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-13T17:35:00"|"00:05:00"    |"2983"   |[0,1940,489,837.7607]               |[450425,246177.5,94173.5]                                   |[0,0,0.006830079240766285]                 |0.006830079240766285|0       |0.0022766930802554284|2        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T16:25:00"|"00:05:00"    |"22510"  |[0,44,12,18.547237]                 |[1479.78076171875,1479.78076171875,1479.78076171875]        |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T16:35:00"|"00:05:00"    |"22552"  |[0,32,8,13.856406]                  |[2699.35595703125,2699.35595703125,2699.35595703125]        |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-15T09:05:00"|"00:05:00"    |"35069"  |[0,20,6,8.246211]                   |[4255.92236328125,4255.92236328125,4255.92236328125]        |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-15T09:05:00"|"00:05:00"    |"35070"  |[0,250,63.5,107.68821]              |[36828.86328125,36828.86328125,36828.86328125]              |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-16T10:35:00"|"00:05:00"    |"57835"  |[0,36,9,15.588457]                  |[2255.67041015625,2255.67041015625,2255.67041015625]        |[0,0,0]                                    |0                  |0       |0                  |0        |


## Dos Attack
The attacker sends a hundred of legitimate IEC 104 packets to the destination. He uses a spoofed IP address 192.168.11.248 which sends an ASDU with TypeID 36 (Measured value, short floating point, with time tag) and CoT=3 (Spontaneous event). This message is only confirmed by the receiver using an APDU of the S-type. The attack start at 23:50:02 and ends at 01:18:29. It contains about 1049 spoofed messages. The attack is repeated at 02:30:05 and lasts until 04:01:54. 

An aggregated flows representing the input to the method are available at:
[dos-attack.dump.flows.csv](https://github.com/nesfit/bonnet-ics-library/blob/main/imadics/data/iec/bonnet/dos-attack.dump.flows.csv)


### Centroids

|FlowKey                                   |WindowStart          |WindowDuration|FlowLabel|Features                 |Distances                                           |Scores |MaxScore|MinScore|AverageScore|BestModel|
|------------------------------------------|---------------------|--------------|---------|-------------------------|----------------------------------------------------|-------|--------|--------|------------|---------|
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-13T17:35:00"|"00:05:00"    |"2983"   |[0,0.36363637,1,0,0,0,1,0]|[0.23264455795288086,0.2326446771621704,0.2326444387435913]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T01:15:00"|"00:05:00"    |"9621"   |[0,0.18181819,0,0.16666667,0,0,0,0]|[0.431702196598053,0.4057646691799164,0.3847934305667877]|[0.177912435798267,0,0]|0.177912435798267|0       |0.059304145266088994|0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-16T10:35:00"|"00:05:00"    |"58902"  |[0,0,0.24484538,0,0,0,0.24358974,0]|[0.3368380665779114,0.3368363082408905,0.33683639764785767]|[0,0,0]|0       |0       |0           |0        |


### Centroids/Pca

|FlowKey                                   |WindowStart          |WindowDuration|FlowLabel|Features                 |Distances                                           |Scores |MaxScore|MinScore|AverageScore|BestModel|
|------------------------------------------|---------------------|--------------|---------|-------------------------|----------------------------------------------------|-------|--------|--------|------------|---------|
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-13T17:35:00"|"00:05:00"    |"2983"   |[1.0050719,-0.0854278,0.3713717]|[0.23258674144744873,0.23258650302886963,0.1966172456741333]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T01:15:00"|"00:05:00"    |"9621"   |[-0.14576086,0.09653735,-0.46707597]|[0.431606650352478,0.4144206941127777,0.4077422022819519]|[0.17806345404590085,0,0]|0.17806345404590085|0       |0.05935448468196695|0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-16T02:05:00"|"00:05:00"    |"51167"  |[-0.44404534,0.6910692,0.16570711]|[0.5466580390930176,0.1985565423965454,0.21408236026763916]|[0,0.06750527835379061,0.05672224576806695]|0.06750527835379061|0       |0.041409174707285855|1        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-16T10:35:00"|"00:05:00"    |"58902"  |[0.22503856,-0.1086671,-0.4443257]|[0.3367701768875122,0.336770236492157,0.33475157618522644]|[0,0,0]|0       |0       |0           |0        |


### Centroids/Avg

|FlowKey                                   |WindowStart          |WindowDuration|FlowLabel|Features                            |Distances                                                   |Scores                                     |MaxScore           |MinScore|AverageScore       |BestModel|
|------------------------------------------|---------------------|--------------|---------|------------------------------------|------------------------------------------------------------|-------------------------------------------|-------------------|--------|-------------------|---------|
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-13T17:35:00"|"00:05:00"    |"2983"   |[0,1940,489,837.7607]               |[450425,246177.5,94173.5]                                   |[0,0,0.006830079240766285]                 |0.006830079240766285|0       |0.0022766930802554284|2        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T01:15:00"|"00:05:00"    |"9621"   |[0,16,6,6.6332498]                  |[4818.2265625,4818.2265625,4818.2265625]                    |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T04:00:00"|"00:05:00"    |"11572"  |[0,36,13,14.798649]                 |[2189.005859375,2189.005859375,2189.005859375]              |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-16T10:35:00"|"00:05:00"    |"58901"  |[0,36,9,15.588457]                  |[2255.67041015625,2255.67041015625,2255.67041015625]        |[0,0,0]                                    |0                  |0       |0                  |0        |



## Injection Attack
There are two attack types:

* The attacker sends ASDUs with TypeID=45 (Single command) and CoT=6 (Activation) on the object with IOA=31, 32 and 2. The host responses with CoT=7 (Activation Conf). The attack starts at 19:35:19 and ends at 19:41:06. It includes 83 packets.
* Another injection attack appears at 21:05:32 when an attacker starts to transfer a file to the compromised host with IP address 192.168.11.111. The attacker sends messages with ASDU typeID=122 (Call directory, select file), 120 (File ready), 121 (Section ready), 123 (Last section), 124 (Ack file), 125 (Segment). The attacker accesses object with IOA=65537 which is not typically accessible. The attack includes 221 messages and ends at 21:21:14.

An aggregated flows representing the input to the method are available at:
[injection-loss.dump.flows.csv](https://github.com/nesfit/bonnet-ics-library/blob/main/imadics/data/iec/bonnet/injection-loss.dump.flows.csv)


### Centroids

|FlowKey                                   |WindowStart          |WindowDuration|FlowLabel|Features                 |Distances                                           |Scores |MaxScore|MinScore|AverageScore|BestModel|
|------------------------------------------|---------------------|--------------|---------|-------------------------|----------------------------------------------------|-------|--------|--------|------------|---------|
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-13T17:35:00"|"00:05:00"    |"2983"   |[0,0.36363637,1,0,0,0,1,0]|[0.23264455795288086,0.2326446771621704,0.2326444387435913]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-13T19:35:00"|"00:05:00"    |"5019"   |[0,0,0.36082476,0,0,0,0.35897437,0]|[0.243272602558136,0.24327242374420166,0.243272602558136]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T01:10:00"|"00:05:00"    |"9527"   |[0,1,0,0.5,0,0,0,0]      |[0.5919825434684753,0.1259913444519043,0.10451245307922363]|[0,0.04624162886365191,0.07660202810260941]|0.07660202810260941|0       |0.040947885655420437|2        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-14T01:10:00"|"00:05:00"    |"9528"   |[0,1,0.24484538,0,0,0,0.24358974,0]|[0.29432469606399536,0.29432618618011475,0.2943263053894043]|[0.08968900499957788,0.0896861844882404,0.08968603475634962]|0.08968900499957788|0.08968603475634962|0.08968707474805597|0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-14T21:05:00"|"00:05:00"    |"26702"  |[0,0,0.09020619,0.041666668,0,0,0.08974359,0]|[0.49749287962913513,0.4974910616874695,0.49749118089675903]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-14T21:10:00"|"00:05:00"    |"26766"  |[0,0,0,0.083333336,0,0,0,0]|[0.6215161681175232,0.6288387775421143,0.6193761229515076]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T21:10:00"|"00:05:00"    |"26772"  |[0,0,0,0.20833334,0,0,0,0]|[0.45460695028305054,0.47358059883117676,0.4667007625102997]|[0.13429506874773012,0,0]|0.13429506874773012|0       |0.04476502291591004|0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T21:15:00"|"00:05:00"    |"26840"  |[0,0,0,0.25,0,0,0,0]     |[0.4059149920940399,0.419540137052536,0.41557347774505615]|[0.2270188343881594,0,0]|0.2270188343881594|0       |0.07567294479605313|0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-14T21:15:00"|"00:05:00"    |"26841"  |[0,0,0,0.083333336,0,0,0,0]|[0.6215161681175232,0.6288387775421143,0.6193761229515076]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-16T10:35:00"|"00:05:00"    |"58903"  |[0,0,0.24484538,0,0,0,0.24358974,0]|[0.3368380665779114,0.3368363082408905,0.33683639764785767]|[0,0,0]|0       |0       |0           |0        |


### Centroids/Pca

|FlowKey                                   |WindowStart          |WindowDuration|FlowLabel|Features                 |Distances                                           |Scores |MaxScore|MinScore|AverageScore|BestModel|
|------------------------------------------|---------------------|--------------|---------|-------------------------|----------------------------------------------------|-------|--------|--------|------------|---------|
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-13T17:35:00"|"00:05:00"    |"2983"   |[1.0050719,-0.0854278,0.3713717]|[0.23258674144744873,0.23258650302886963,0.1966172456741333]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-13T19:35:00"|"00:05:00"    |"5019"   |[0.345567,-0.15508929,-0.3439357]|[0.2432159185409546,0.2432161569595337,0.19543957710266113]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T01:10:00"|"00:05:00"    |"9527"   |[-0.38772207,0.7190999,0.11107075]|[0.5919633507728577,0.22219565510749817,0.23861581087112427]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-14T01:10:00"|"00:05:00"    |"9528"   |[0.20466621,0.7892814,-0.0048128963]|[0.29431718587875366,0.2943171262741089,0.23552152514457703]|[0.08961430479007315,0.08961430663267045,0]|0.08961430663267045|0       |0.05974287047424787|1        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-14T21:05:00"|"00:05:00"    |"26702"  |[0.036172383,-0.060786173,-0.5508608]|[0.49738529324531555,0.4973853528499603,0.5119916796684265]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-14T21:10:00"|"00:05:00"    |"26766"  |[-0.08573356,-0.038695354,-0.6016237]|[0.6213599443435669,0.6392205357551575,0.6442415714263916]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T21:10:00"|"00:05:00"    |"26772"  |[-0.17021841,-0.080741346,-0.5196692]|[0.45448988676071167,0.4669719338417053,0.4699620008468628]|[0.134485422339776,0,0]|0.134485422339776|0       |0.044828474113258666|0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T21:15:00"|"00:05:00"    |"26840"  |[-0.19838002,-0.09475667,-0.49235106]|[0.40580981969833374,0.4140912592411041,0.41674724221229553]|[0.22719003230208423,0,0]|0.22719003230208423|0       |0.07573001076736141|0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-14T21:15:00"|"00:05:00"    |"26841"  |[-0.08573356,-0.038695354,-0.6016237]|[0.6213599443435669,0.6392205357551575,0.6442415714263916]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-16T02:05:00"|"00:05:00"    |"51168"  |[-0.44404534,0.6910692,0.16570711]|[0.5466580390930176,0.1985565423965454,0.21408236026763916]|[0,0.06750527835379061,0.05672224576806695]|0.06750527835379061|0       |0.041409174707285855|1        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-16T10:35:00"|"00:05:00"    |"58903"  |[0.22503856,-0.1086671,-0.4443257]|[0.3367701768875122,0.336770236492157,0.33475157618522644]|[0,0,0]|0       |0       |0           |0        |


### Centroids/Avg

|FlowKey                                   |WindowStart          |WindowDuration|FlowLabel|Features                            |Distances                                                   |Scores                                     |MaxScore           |MinScore|AverageScore       |BestModel|
|------------------------------------------|---------------------|--------------|---------|------------------------------------|------------------------------------------------------------|-------------------------------------------|-------------------|--------|-------------------|---------|
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-13T17:35:00"|"00:05:00"    |"2983"   |[0,1940,489,837.7607]               |[450425,246177.5,94173.5]                                   |[0,0,0.006830079240766285]                 |0.006830079240766285|0       |0.0022766930802554284|2        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-14T21:05:00"|"00:05:00"    |"26702"  |[0,6813,1748,2925.1323]             |[37452304,35341380,33126654]                                |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T21:05:00"|"00:05:00"    |"26705"  |[0,136,41,56.02678]                 |[4227.76953125,4227.76953125,4227.76953125]                 |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-14T21:10:00"|"00:05:00"    |"26766"  |[0,9215,2305.75,3989.0586]          |[77524880,74474176,71242816]                                |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T21:10:00"|"00:05:00"    |"26772"  |[0,170,47.5,71.19515]               |[10537.4296875,10537.4296875,10537.4296875]                 |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T21:15:00"|"00:05:00"    |"26840"  |[0,170,48.5,70.82902]               |[10559.8046875,10559.8046875,10559.8046875]                 |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-14T21:15:00"|"00:05:00"    |"26841"  |[0,9101,2277.25,3939.6953]          |[75296672,72290576,69107464]                                |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-14T21:20:00"|"00:05:00"    |"26913"  |[4,2345,795.5,955.04565]            |[1362275,991674,659577.5]                                   |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-16T10:35:00"|"00:05:00"    |"58902"  |[0,36,9,15.588457]                  |[2255.67041015625,2255.67041015625,2255.67041015625]        |[0,0,0]                                    |0                  |0       |0                  |0        |


## Rogue Device Attack

The attacker uses a sequence of IEC 104 messages with ASDU type=36 (Measured value, short floating point with time tag) and CoT=3 (spontaneous event). It also correctly responses with supervisory APDUs. The attack start at 15:19:00 and ends at 15:46:03. It uses an IP address 192.168.11.246. The attack includes 417 packets.


An aggregated flows representing the input to the method are available at:
[rogue-device.dump.flows.csv](https://github.com/nesfit/bonnet-ics-library/blob/main/imadics/data/iec/bonnet/rogue-device.dump.flows.csv)


### Centroids

|FlowKey                                   |WindowStart          |WindowDuration|FlowLabel|Features                 |Distances                                           |Scores |MaxScore|MinScore|AverageScore|BestModel|
|------------------------------------------|---------------------|--------------|---------|-------------------------|----------------------------------------------------|-------|--------|--------|------------|---------|
|"192.168.11.246:2404-192.168.11.111:61254"|"2022-05-13T15:15:00"|"00:05:00"    |"639"    |[0,0,0.12886599,0,0,0,0.12820514,0]|[0.4471054971218109,0.44710367918014526,0.4471037685871124]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.111:61254-192.168.11.246:2404"|"2022-05-13T15:15:00"|"00:05:00"    |"640"    |[0,0,0,0.16666667,0,0,0,0]|[0.5067711472511292,0.5229837894439697,0.5203309655189514]|[0.03495914235689235,0,0]|0.03495914235689235|0       |0.01165304745229745|0        |
|"192.168.11.246:2404-192.168.11.111:61254"|"2022-05-13T15:45:00"|"00:05:00"    |"1044"   |[0,0.09090909,0.18814434,0,0,0,0.1923077,0]|[0.295146107673645,0.29514461755752563,0.2951447367668152]|[0.08714847737928433,0.08715487934147537,0.08715473021846598]|0.08715487934147537|0.08714847737928433|0.08715269564640855|1        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-13T17:35:00"|"00:05:00"    |"2946"   |[0,0.36363637,1,0,0,0,1,0]|[0.23264455795288086,0.2326446771621704,0.2326444387435913]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T01:10:00"|"00:05:00"    |"9490"   |[0,1,0,0.5,0,0,0,0]      |[0.5919825434684753,0.1259913444519043,0.10451245307922363]|[0,0.04624162886365191,0.07660202810260941]|0.07660202810260941|0       |0.040947885655420437|2        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-14T01:10:00"|"00:05:00"    |"9491"   |[0,1,0.24484538,0,0,0,0.24358974,0]|[0.29432469606399536,0.29432618618011475,0.2943263053894043]|[0.08968900499957788,0.0896861844882404,0.08968603475634962]|0.08968900499957788|0.08968603475634962|0.08968707474805597|0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-16T10:35:00"|"00:05:00"    |"58866"  |[0,0,0.24484538,0,0,0,0.24358974,0]|[0.3368380665779114,0.3368363082408905,0.33683639764785767]|[0,0,0]|0       |0       |0           |0        |


### Centroids/Pca

|FlowKey                                   |WindowStart          |WindowDuration|FlowLabel|Features                 |Distances                                           |Scores |MaxScore|MinScore|AverageScore|BestModel|
|------------------------------------------|---------------------|--------------|---------|-------------------------|----------------------------------------------------|-------|--------|--------|------------|---------|
|"192.168.11.246:2404-192.168.11.111:61254"|"2022-05-13T15:15:00"|"00:05:00"    |"639"    |[0.10451014,-0.062244907,-0.54471564]|[0.4470008313655853,0.4470008909702301,0.46571528911590576]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.111:61254-192.168.11.246:2404"|"2022-05-13T15:15:00"|"00:05:00"    |"640"    |[-0.1420568,-0.066726014,-0.5469874]|[0.5066417455673218,0.5233243107795715,0.5266485214233398]|[0.03516925411673255,0,0]|0.03516925411673255|0       |0.011723084705577516|0        |
|"192.168.11.246:2404-192.168.11.111:61254"|"2022-05-13T15:45:00"|"00:05:00"    |"1044"   |[0.16692904,-0.00536751,-0.45122522]|[0.2950645685195923,0.29506462812423706,0.3121839165687561]|[0.08730249121712053,0.08730212385643432,0]|0.08730249121712053|0       |0.058201538357851614|0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-13T17:35:00"|"00:05:00"    |"2946"   |[1.0050719,-0.0854278,0.3713717]|[0.23258674144744873,0.23258650302886963,0.1966172456741333]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T01:10:00"|"00:05:00"    |"9490"   |[-0.38772207,0.7190999,0.11107075]|[0.5919633507728577,0.22219565510749817,0.23861581087112427]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-14T01:10:00"|"00:05:00"    |"9491"   |[0.20466621,0.7892814,-0.0048128963]|[0.29431718587875366,0.2943171262741089,0.23552152514457703]|[0.08961430479007315,0.08961430663267045,0]|0.08961430663267045|0       |0.05974287047424787|1        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-16T02:05:00"|"00:05:00"    |"51131"  |[-0.44404534,0.6910692,0.16570711]|[0.5466580390930176,0.1985565423965454,0.21408236026763916]|[0,0.06750527835379061,0.05672224576806695]|0.06750527835379061|0       |0.041409174707285855|1        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-16T10:35:00"|"00:05:00"    |"58866"  |[0.22503856,-0.1086671,-0.4443257]|[0.3367701768875122,0.336770236492157,0.33475157618522644]|[0,0,0]|0       |0       |0           |0        |


### Centroids/Avg

|FlowKey                                   |WindowStart          |WindowDuration|FlowLabel|Features                            |Distances                                                   |Scores                                     |MaxScore           |MinScore|AverageScore       |BestModel|
|------------------------------------------|---------------------|--------------|---------|------------------------------------|------------------------------------------------------------|-------------------------------------------|-------------------|--------|-------------------|---------|
|"192.168.11.246:2404-192.168.11.111:61254"|"2022-05-13T15:15:00"|"00:05:00"    |"639"    |[0,250,62.5,108.253174]             |[36833.4765625,36833.4765625,36833.4765625]                 |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.246:2404"|"2022-05-13T15:15:00"|"00:05:00"    |"640"    |[0,16,4,6.928203]                   |[4874.099609375,4874.099609375,4874.099609375]              |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.246:2404"|"2022-05-13T15:45:00"|"00:05:00"    |"1043"   |[0,24,7,9.949874]                   |[3694.09423828125,3694.09423828125,3694.09423828125]        |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.246:2404-192.168.11.111:61254"|"2022-05-13T15:45:00"|"00:05:00"    |"1044"   |[0,365,92.25,157.48076]             |[102704.3984375,102704.3984375,102704.3984375]              |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-13T17:35:00"|"00:05:00"    |"2946"   |[0,1940,489,837.7607]               |[450425,246177.5,94173.5]                                   |[0,0,0.006830079240766285]                 |0.006830079240766285|0       |0.0022766930802554284|2        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-16T10:35:00"|"00:05:00"    |"58865"  |[0,36,9,15.588457]                  |[2255.67041015625,2255.67041015625,2255.67041015625]        |[0,0,0]                                    |0                  |0       |0                  |0        |


## Scanning Attack

Two types of scanning attacks are included:

* horizontal scanning starts at 10:32:07 and ends at 10:49:10. The attacker employs a spoofed IP address 192.168.11.102:45280. The scan sends IEC 104 U-commands TestFrame Act (ApduType 0x03, UType 0x10) on port 2404 (used by IEC 104). If a station exists, the scan yields a proper response TestFrame Conf (ApduType 0x03, UType 0x20).
* vertical scanning attack - explores IEC 104 information objects on the device with IP address 192.168.11.111. In order to masquerade his identity, the attacker uses a spoofed source address 192.168.11.248 which belongs to the existing node. The attacker sends an interrogation command with TypeID=100 (General Interrogation) and CoT=6 (Activation). If an object exists, it responses with TypeID=100 and CoT=7 (Activation Conf), otherwise it sends a message with CoT=47 (unknown object address). For vertical scanning we use a default ASDU address 65535 (Global address) and a default value for the Originator address (OA = 0, not used). The IOA length is limited to 2 bytes, however, our scan tests only values from 1 to 127. The attack starts at 01:02:18 and ends at 01:23:19.


An aggregated flows representing the input to the method are available at:
[scanning-attack.dump.flows.csv](https://github.com/nesfit/bonnet-ics-library/blob/main/imadics/data/iec/bonnet/scanning-attack.dump.flows.csv)


### Centroids

|FlowKey                                   |WindowStart          |WindowDuration|FlowLabel|Features                 |Distances                                           |Scores |MaxScore|MinScore|AverageScore|BestModel|
|------------------------------------------|---------------------|--------------|---------|-------------------------|----------------------------------------------------|-------|--------|--------|------------|---------|
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-13T17:35:00"|"00:05:00"    |"2983"   |[0,0.36363637,1,0,0,0,1,0]|[0.23264455795288086,0.2326446771621704,0.2326444387435913]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T01:10:00"|"00:05:00"    |"9527"   |[0,1,0,0.5,0,0,0,0]      |[0.5919825434684753,0.1259913444519043,0.10451245307922363]|[0,0.04624162886365191,0.07660202810260941]|0.07660202810260941|0       |0.040947885655420437|2        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-14T01:10:00"|"00:05:00"    |"9528"   |[0,1,0.24484538,0,0,0,0.24358974,0]|[0.29432469606399536,0.29432618618011475,0.2943263053894043]|[0.08968900499957788,0.0896861844882404,0.08968603475634962]|0.08968900499957788|0.08968603475634962|0.08968707474805597|0        |
|"192.168.11.102:45280-192.168.11.1-255:2404"  |"2022-05-14T10:30:00"|"00:05:00"    |"17038" - "17294"  |[0,0.09090909,0,0,0,0,0,0]|[0.5458970665931702,0.545895516872406,0.5458956360816956]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-14T10:45:00"|"00:05:00"    |"17295"  |[0,0.09090909,0.06443299,0,0,0,0.06410257,0]|[0.4449857473373413,0.4449842572212219,0.4449843466281891]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T10:45:00"|"00:05:00"    |"17298"  |[0,0.09090909,0,0.041666668,0,0,0,0]|[0.547633171081543,0.5476316213607788,0.5476317405700684]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-15T01:05:00"|"00:05:00"    |"29839"  |[0,0,0,0,0,0,0,0]        |[0.6324065327644348,0.6324046850204468,0.6324048042297363]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-15T01:05:00"|"00:05:00"    |"29840"  |[0,0,0,0,0,0,0,0]        |[0.6324065327644348,0.6324046850204468,0.6324048042297363]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-15T01:10:00"|"00:05:00"    |"29909"  |[0,0,0,0,0,0,0,0]        |[0.6324065327644348,0.6324046850204468,0.6324048042297363]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-15T01:10:00"|"00:05:00"    |"29910"  |[0,0,0,0,0,0,0,0]        |[0.6324065327644348,0.6324046850204468,0.6324048042297363]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-15T01:15:00"|"00:05:00"    |"29964"  |[0,0,0,0,0,0,0,0]        |[0.6324065327644348,0.6324046850204468,0.6324048042297363]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-15T01:15:00"|"00:05:00"    |"29965"  |[0,0,0,0,0,0,0,0]        |[0.6324065327644348,0.6324046850204468,0.6324048042297363]|[0,0,0]|0       |0       |0           |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-15T01:20:00"|"00:05:00"    |"30023"  |[0,0.27272728,0,0.16666667,0,0,0,0]|[0.4189611077308655,0.3719484806060791,0.34181803464889526]|[0.20217520488919272,0,0]|0.20217520488919272|0       |0.06739173496306423|0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-16T10:35:00"|"00:05:00"    |"58900"  |[0,0,0.24484538,0,0,0,0.24358974,0]|[0.3368380665779114,0.3368363082408905,0.33683639764785767]|[0,0,0]|0       |0       |0           |0        |


### Centroids/Pca

|FlowKey                                   |WindowStart          |WindowDuration|FlowLabel|Features                            |Distances                                                   |Scores                                     |MaxScore           |MinScore|AverageScore       |BestModel|
|------------------------------------------|---------------------|--------------|---------|------------------------------------|------------------------------------------------------------|-------------------------------------------|-------------------|--------|-------------------|---------|
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-13T17:35:00"|"00:05:00"    |"2983"   |[1.0050719,-0.0854278,0.3713717]    |[0.23258674144744873,0.23258650302886963,0.1966172456741333]|[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T01:10:00"|"00:05:00"    |"9527"   |[-0.38772207,0.7190999,0.11107075]  |[0.5919633507728577,0.22219565510749817,0.23861581087112427]|[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-14T01:10:00"|"00:05:00"    |"9528"   |[0.20466621,0.7892814,-0.0048128963]|[0.29431718587875366,0.2943171262741089,0.23552152514457703]|[0.08961430479007315,0.08961430663267045,0]|0.08961430663267045|0       |0.05974287047424787|1        |
|"192.168.11.102:45280-192.168.11.1-255:2404"  |"2022-05-14T10:30:00"|"00:05:00"    |"17038" - "17294"  |[-0.031262364,0.07096699,-0.6163044]|[0.5457615852355957,0.5457616448402405,0.5426161289215088]  |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-14T10:45:00"|"00:05:00"    |"17295"  |[0.035697877,0.04517688,-0.56053215]|[0.44487565755844116,0.44487571716308594,0.44857680797576904]|[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T10:45:00"|"00:05:00"    |"17298"  |[-0.05942398,0.056951657,-0.5889862]|[0.547510027885437,0.547510027885437,0.5443644523620605]    |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-15T01:05:00"|"00:05:00"    |"29839"  |[-0.029410332,-0.010664694,-0.6562601]|[0.6322504281997681,0.6322504878044128,0.6372717022895813]  |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-15T01:05:00"|"00:05:00"    |"29840"  |[-0.029410332,-0.010664694,-0.6562601]|[0.6322504281997681,0.6322504878044128,0.6372717022895813]  |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-15T01:10:00"|"00:05:00"    |"29909"  |[-0.029410332,-0.010664694,-0.6562601]|[0.6322504281997681,0.6322504878044128,0.6372717022895813]  |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-15T01:10:00"|"00:05:00"    |"29910"  |[-0.029410332,-0.010664694,-0.6562601]|[0.6322504281997681,0.6322504878044128,0.6372717022895813]  |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-15T01:15:00"|"00:05:00"    |"29964"  |[-0.029410332,-0.010664694,-0.6562601]|[0.6322504281997681,0.6322504878044128,0.6372717022895813]  |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-15T01:15:00"|"00:05:00"    |"29965"  |[-0.029410332,-0.010664694,-0.6562601]|[0.6322504281997681,0.6322504878044128,0.6372717022895813]  |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-15T01:20:00"|"00:05:00"    |"30023"  |[-0.14761288,0.17816904,-0.42712027]|[0.41888004541397095,0.35288891196250916,0.3491743505001068]|[0.2022995535043689,0,0]                   |0.2022995535043689 |0       |0.0674331845014563 |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-16T02:05:00"|"00:05:00"    |"51165"  |[-0.44404534,0.6910692,0.16570711]  |[0.5466580390930176,0.1985565423965454,0.21408236026763916] |[0,0.06750527835379061,0.05672224576806695]|0.06750527835379061|0       |0.041409174707285855|1        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-16T10:35:00"|"00:05:00"    |"58900"  |[0.22503856,-0.1086671,-0.4443257]  |[0.3367701768875122,0.336770236492157,0.33475157618522644]  |[0,0,0]                                    |0                  |0       |0                  |0        |



### Centroids/Avg

|FlowKey                                   |WindowStart          |WindowDuration|FlowLabel|Features                            |Distances                                                   |Scores                                     |MaxScore           |MinScore|AverageScore       |BestModel|
|------------------------------------------|---------------------|--------------|---------|------------------------------------|------------------------------------------------------------|-------------------------------------------|-------------------|--------|-------------------|---------|
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-13T17:35:00"|"00:05:00"    |"2983"   |[0,1940,254.25,637.6515]            |[416096.25,211538,190712.75]                                |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T10:30:00"|"00:05:00"    |"17006"  |[0,36,5.5,11.82159]                 |[2002.7685546875,2002.7685546875,2002.7685546875]           |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.102:45280-192.168.11.1-255:2404"  |"2022-05-14T10:30:00"|"00:05:00"    |"17038" - "17294"  |[0,4,0.5,1.3228756]                 |[6209.0078125,6209.0078125,6209.0078125]                    |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-14T10:45:00"|"00:05:00"    |"17295"  |[0,125,16.75,40.959583]             |[2457.892578125,2457.892578125,2457.892578125]              |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T10:45:00"|"00:05:00"    |"17298"  |[0,4,1,1.7320508]                   |[6178.47509765625,6178.47509765625,6178.47509765625]        |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-15T01:00:00"|"00:05:00"    |"29781"  |[0,182,29.875,58.916122]            |[12232.9375,12232.9375,12232.9375]                          |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-15T01:10:00"|"00:05:00"    |"29909"  |[0,392,52.5,128.64583]              |[110798.609375,110798.609375,110798.609375]                 |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-15T01:10:00"|"00:05:00"    |"29910"  |[0,378,50.625,124.05134]            |[101130.90625,101130.90625,101130.90625]                    |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-15T01:15:00"|"00:05:00"    |"29964"  |[0,406,54.375,133.24033]            |[120907.5625,120907.5625,120081.375]                        |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-15T01:15:00"|"00:05:00"    |"29965"  |[0,406,54.375,133.24033]            |[120907.5625,120907.5625,120081.375]                        |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-15T01:20:00"|"00:05:00"    |"30022"  |[0,308,68.75,110.53478]             |[63321.875,63321.875,63321.875]                             |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-15T01:20:00"|"00:05:00"    |"30023"  |[0,308,44.75,99.829544]             |[59445.91015625,59445.91015625,59445.91015625]              |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-16T10:35:00"|"00:05:00"    |"58899"  |[0,36,4.5,11.905881]                |[2013.20849609375,2013.20849609375,2013.20849609375]        |[0,0,0]                                    |0                  |0       |0                  |0        |



## Switching Attack

Switching the device on/off. The attack contains a sequence of IEC 104 packets with TypID=46 (double cmd), numix=1, CoT=6 (Act), Cot=7 (ActCon), CoT=10 (ActTerm), OA=0, Addr=655535, IOA=2. The attack starts at 06:27:55:00 and repeats the series (10 minutes, 72 new packets).

An aggregated flows representing the input to the method are available at:
[switching-attack.dump.flows.csv](https://github.com/nesfit/bonnet-ics-library/blob/main/imadics/data/iec/bonnet/switching-attack.dump.flows.csv)


### Centroids

|FlowKey                                   |WindowStart          |WindowDuration|FlowLabel|Features                            |Distances                                                   |Scores                                     |MaxScore           |MinScore|AverageScore       |BestModel|
|------------------------------------------|---------------------|--------------|---------|------------------------------------|------------------------------------------------------------|-------------------------------------------|-------------------|--------|-------------------|---------|
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-13T17:35:00"|"00:05:00"    |"2983"   |[0,0.36363637,1,0,0,0,1,0]          |[0.23264455795288086,0.2326446771621704,0.2326444387435913] |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T01:10:00"|"00:05:00"    |"9527"   |[0,1,0,0.5,0,0,0,0]                 |[0.5919825434684753,0.1259913444519043,0.10451245307922363] |[0,0.04624162886365191,0.07660202810260941]|0.07660202810260941|0       |0.040947885655420437|2        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-14T01:10:00"|"00:05:00"    |"9528"   |[0,1,0.24484538,0,0,0,0.24358974,0] |[0.29432469606399536,0.29432618618011475,0.2943263053894043]|[0.08968900499957788,0.0896861844882404,0.08968603475634962]|0.08968900499957788|0.08968603475634962|0.08968707474805597|0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-16T10:35:00"|"00:05:00"    |"58975"  |[0,0,0.24484538,0,0,0,0.24358974,0] |[0.3368380665779114,0.3368363082408905,0.33683639764785767] |[0,0,0]                                    |0                  |0       |0                  |0        |


### Centroids/Pca

|FlowKey                                   |WindowStart          |WindowDuration|FlowLabel|Features                            |Distances                                                   |Scores                                     |MaxScore           |MinScore|AverageScore       |BestModel|
|------------------------------------------|---------------------|--------------|---------|------------------------------------|------------------------------------------------------------|-------------------------------------------|-------------------|--------|-------------------|---------|
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-13T17:35:00"|"00:05:00"    |"2983"   |[1.0050719,-0.0854278,0.3713717]    |[0.23258674144744873,0.23258650302886963,0.1966172456741333]|[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T01:10:00"|"00:05:00"    |"9527"   |[-0.38772207,0.7190999,0.11107075]  |[0.5919633507728577,0.22219565510749817,0.23861581087112427]|[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-14T01:10:00"|"00:05:00"    |"9528"   |[0.20466621,0.7892814,-0.0048128963]|[0.29431718587875366,0.2943171262741089,0.23552152514457703]|[0.08961430479007315,0.08961430663267045,0]|0.08961430663267045|0       |0.05974287047424787|1        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-16T02:05:00"|"00:05:00"    |"51240"  |[-0.44404534,0.6910692,0.16570711]  |[0.5466580390930176,0.1985565423965454,0.21408236026763916] |[0,0.06750527835379061,0.05672224576806695]|0.06750527835379061|0       |0.041409174707285855|1        |
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-16T10:35:00"|"00:05:00"    |"58975"  |[0.22503856,-0.1086671,-0.4443257]  |[0.3367701768875122,0.336770236492157,0.33475157618522644]  |[0,0,0]                                    |0                  |0       |0                  |0        |


### Centroids/Avg

|FlowKey                                   |WindowStart          |WindowDuration|FlowLabel|Features                            |Distances                                                   |Scores                                     |MaxScore           |MinScore|AverageScore       |BestModel|
|------------------------------------------|---------------------|--------------|---------|------------------------------------|------------------------------------------------------------|-------------------------------------------|-------------------|--------|-------------------|---------|
|"192.168.11.248:2404-192.168.11.111:61254"|"2022-05-13T17:35:00"|"00:05:00"    |"2983"   |[0,1940,489,837.7607]               |[450425,246177.5,94173.5]                                   |[0,0,0.006830079240766285]                 |0.006830079240766285|0       |0.0022766930802554284|2        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T06:25:00"|"00:05:00"    |"13335"  |[0,175,67.75,68.77636]              |[12711.8828125,12711.8828125,12711.8828125]                 |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T06:30:00"|"00:05:00"    |"13421"  |[0,250,84.5,99.22071]               |[37781.65625,37781.65625,37781.65625]                       |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-14T06:35:00"|"00:05:00"    |"13519"  |[0,175,64.75,71.18769]              |[12630.83984375,12630.83984375,12630.83984375]              |[0,0,0]                                    |0                  |0       |0                  |0        |
|"192.168.11.111:61254-192.168.11.248:2404"|"2022-05-16T10:35:00"|"00:05:00"    |"58974"  |[0,36,9,15.588457]                  |[2255.67041015625,2255.67041015625,2255.67041015625]        |[0,0,0]                                    |0                  |0       |0                  |0        |

## References

* https://www.fit.vut.cz/research/publication-file/11570/TR-IEC104.pdf

* https://towardsdatascience.com/encoding-categorical-variables-one-hot-vs-dummy-encoding-6d5b9c46e2db
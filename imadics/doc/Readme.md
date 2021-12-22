# Machine-learning for profile based anomaly detection in ICS traffic

## 1. Introduction

The presented methods of detecting network communication anomalies are based on the characterization of normal behavior using a model. These methods accept a vector representation of IPFIX records for a supported set of protocols. The problem with detecting anomalies is to identify different communication patterns in the input data, given that these anomalies occur rarely and rather in small numbers. The AD method is expected to decide whether the observed pattern is normal or abnormal. However, most existing AD algorithms return more than a binary label. Instead, they are able to calculate a scoring function that is used to provide binary prediction using a certain threshold value calculated according to the statistical distribution of the samples. The score allows us to use a domain-specific threshold to adjust the detection output. In this report, we present principles and evaluate several algorithms as a possible basis for a robust method for detecting anomalies.

### 1.1 Problem Statement

The goal of the anomaly detection method for ICS communication analysis is to decide whether the spotted traffic pattern is normal or anomalous. As ICS network communication commonly consists of regular intervals of network communication with recurring patterns the AD method should be able to precisely identify these patterns. Nevertheless, the robust method also needs to tolerate slight differences in the traffic characterization as the system communication may be influenced by the actual network situation. 
The presented anomaly detection method requires establishing a profile by means of learning normal network patterns first. The profile should be created for the sample of normal traffic. This sample should be large enough to represent all normal network patterns.

### 1.2 Detection Methods

There may be different approaches to construct AD algorithms. The AD algorithm can be based on clustering, one-class classification or principal component analysis, for instance. 
In principle, the AD methods can be to classified to:

* Supervised - requires labels for normal as well as abnormal samples of data.
* Semi-supervised - does not require abnormal data to train, it is sufficient to provide normal data. 
* Unsupervised - does not require training data at all. The techniques that abnormal data are rare.

Anomaly detection is often formulated as a problem of identification of outliers. Outliers are objects for which a large proportion of the data lies beyond a fixed distance threshold. Often, instead of defining an explicit distance the score reflecting its ‘outlierness’ is computed.


### 1.3 Evaluation Approach

The evaluation of anomaly classification methods is complicated due to the lack of suitable data sets. We follow the approach of Campos et al. [1]. They provided guidance on how to evaluate and analyze anomaly classification methods. To evaluate the semi-supervised or unsupervised anomaly detection methods we usually do not have labeled datasets.

Cluster-based methods can be evaluated wrt various [measures](https://www.cs.upc.edu/~bejar/URL/material/04-Validation.pdf):

* Cluster tendency - are there clusters in the data? Here we assume so, because of the flow characteristics. However, this is influenced by feature selection.
* Compare the clusters to the true partition of the data - do not know how to do it.
* Quality of the clusters without reference to external information - use only internal criteria, namely, cluster compactness and separation. This is based on computing indices. More than 30 indices can be found in the literature. 
* Evaluate algorithm parameters, for instance, to determine the correct number of clusters - checking the result of clustering for different numbers of clusters.
 
The performance evaluation of the proposed method is performed using an automated evaluation script that considers the following dimensions:

* Method parameters
* Model input 
* Test input

The combinations of parameters, input model and test files give a set of evaluation scenarios. For each scenario we compute its overall performance in terms of the following scores:

| Category | Meaning | 
| --------- | --------------- |  
| True positive, TP | anomaly flow is correctly labeled as anomaly |
| True negative, TN | normal flow is correctly labeled as normal |
| False positive, FP | normal flow is incorrectly labeled as anomaly |
|  False negative, FN | anomaly flow is incorrectly labeled as normal |


Based on TP, TN, FP and FN we can determine the sensitivity and specificity of the method:

* Sensitivity, TPR = TP/(TP+FN)
* Specificity, SPC = TN/(TN+FP)
* False positive rate, FPR = 1 - SPC
* The receiver operating characterizing (ROC) curve is TPR/FPR for different threshold values T


#### 1.4 Datasets

For the evaluation we use various available datasets for supported ICS protocols and in addition to this we have also created a new collection of ICS communication traces using the simulated industrial environment (denoted as Factory20 dataset). We are able to evaluate the method for Modbus, DNP3 and S7COMM communication protocols. Recently, new ICS datasets have been made public. These datasets usually provide a collection of CSV files containing the preprocessed data on ICS events sometimes also including network communication. Only a few of them also contain packet traces. Thus we consider the datasets containing PCAP files, which is the case of [Lemay](https://github.com/antoine-lemay/Modbus_dataset ),[tjcruz-dei](https://ieee-dataport.org/documents/cyber-security-modbus-ics-dataset), [4SICS](https://www.netresec.com/?page=PCAP4SICS ), [2017QUT](https://github.com/qut-infosec), 
[DigitalBond](https://www.netresec.com/?page=DigitalBond_S4), [DEFCON23](https://www.defcon.org/html/defcon-23/dc-23-index.html) and our [Factory20](https://ieee-dataport.org/documents/modbus-dataset-ics-anomaly-detection).

## 2. ICS Traffic Monitoring

Traffic monitoring is a prerequisite for anomaly detection. The AD method may depend on the amount of information provided by the monitoring process. Traffic monitoring can range from packet capture on the one hand to traffic overview consisting of connection counters on the other. In our approach, we assume the use of information collected by IPFIX probes, which in addition to standard NetFlow records can also provide application protocol-specific information for each connection. It is therefore possible to use the available application information in AD methods to increase their accuracy.

### 2.1 	IPFIX based monitoring

In the course of ICS traffic monitoring the IPFIX metering data is generated. The flow data contains ICS related information in the form of various operation counters. For instance, the MODBUS connection is characterized using the set of IPFIX counters as listed in Table [modbus-ipfix]. The connections are processed in the predefined time intervals, e.g., 30 seconds. Thus long term connections are split into several flows. In each time interval the IPFIX metering process generates the collection of flows occurring within this interval and sends it to the collector that performs further computations. ICS devices may manage the underlying TCP connection in several ways. They can either establish the TCP connection, perform a transaction and close the connection. Or they can keep this connection alive between transactions. In order to unify both approaches the IPFIX collector performs an aggregation of flows.   

### 2.2 Metering Process

The measurement process continuously collects ICS flows. The cache expiration is set to a configurable interval to obtain a snapshot of system communication. After each interval, the flow cache is processed and emptied. Therefore, a set of corresponding flows is recorded for the observed ICS communication in a given interval. For profile-based methods, it is therefore necessary to use window mechanisms correctly, ie the window used to create the profile must have the same duration as the window used to detect flow anomalies.

### 2.3 Content Durability
As observed, MODBUS ICS communicating hosts can either establish a single long-term TCP connection or use several short-term ones. Since the profile is compiled assuming data exchange between a pair of hosts at specified intervals, we unify communication using aggregation on observed flows. The MODBUS connections can be as follows:

* Short connections, which last only for the duration of performing the single transaction. The connection is closed after sending one or a few commands and receiving the corresponding answers.

* Long connections are open for a substantial time and are used by the Master to regularly communicate with the Slave device. This connection contains many transactions and it is not terminated. The connection can be reestablished in the case of network errors or under some other circumstances.

### 2.4 Aggregation
The purpose of the aggregation of ICS flows is to unify the metered information for both short and long connections. To create a profile of ICS communication and detect anomalies by considering a number of ICS operations the count of operations per individual time interval is considered. The aggregation is defined by the following flow key:

```
Transport Protocol, Client-side address, Server-side address, Server-side port
```

The non-key flow fields are aggregated depending flow field types:

* Timer fields are aggregated by applying MIN or MAX operation based on field semantics,
* Counter fields are aggregated by applying SUM operation.

For instance, the following two flow records:

```
[PROTOCOL=TCP, IPV4_SRC_ADDR=192.168.1.110, L4_SRC_PORT=34556, IPV4_DST_ADDR=192.168.1.10, L4_DST_PORT=502, BYTES=4568, PKTS=34, FIRST=1639954800122, LAST=1639954800435]
[PROTOCOL=TCP, IPV4_SRC_ADDR=192.168.1.110, L4_SRC_PORT=35534, IPV4_DST_ADDR=192.168.1.10, L4_DST_PORT=502, BYTES=2368, PKTS=12, FIRST=1639954800765, LAST=1639954800967]
```

are aggregated to the resulting soingle flow record as follows:

```
[PROTOCOL=TCP, IPV4_SRC_ADDR=192.168.1.110, L4_SRC_PORT=*, IPV4_DST_ADDR=192.168.1.10, L4_DST_PORT=502, BYTES=6936, PKTS=46, FIRST=1639954800122, LAST=1639954800967]
```

## 3. Methods

Anomaly detection methods include a common preprocessing unit that allows different detection methods to be applied. There are three different anomaly detection methods available in the current version. Two of them are based on grouping communication patterns into clusters according to their characteristics by employing K-Means clustering (KMC) and Gaussian Mixture Model (GMM). The last method analyzes peforms Time Series Analysis (TSA) of flow data reduced to one dimension using singular spectrum analysis (SSA).

### 3.1 K-Means Clustering

The K-Means Clustering (KMC) method uses a clustering algorithm (K-Means++) to model normal traffic as a collection of clusters. Based on identified clusters of normal flows, it is possible to classify the new flow either as normal or unknown. If the method also contains the model of malicious flows then we can classify flows as normal, malicious and unknown.
The problem for the clustering algorithm is to discover groups of members that are well separated. Also, we would like to have a way to decide the optimal number of clusters that fits a data set. Note that more cluster instances do not necessary correspond to a better clusterization. When clusters are identified in the phase of model training we use a validation algorithm to evaluate each possible solution and to select the most suitable one. Clustering validity can be computed using different algorithms, e.g., Silhouette index,  Dunn index or Davies–Bouldin index. 

The method is described in [design and specification document](kmc-method.md). The implementation of the method was evaluated using the available datasets as described in the [experiment report](kmc-experiments.md).

### 3.2 Time Series Analysis

The time series analysis (TSA) approach uses singular spectrum analysis (SSA) to retrieve data by splitting the time series into a set of components. These components is interpreted in terms of trends, noise, seasonality and other factors. By reconstruction of these components it is possible to predict/verify values in the future.

### 3.3.Gaussian Mixture Model

A Gaussian mixture model [(GMM)](https://scikit-learn.org/stable/modules/mixture.html) expects that all the data points are generated from a mixture of a finite number of Gaussian distributions. In our settings it means that each normal communication pattern can be considered as belonging to some Gaussian distribution. Thus the model represents an entire ICS communication by the reconstrucet mixture of gaussian distributions. The GMM can be considered as a deneralization of k-means clustering. 

## References
1. Campos, Guilherme, Arthur Zimek, Jorg Sander, Ricardo Campello, Barbora Micenkova, Erich Schubert, Ira Assent, and Michael Houle. ‘On the Evaluation of Unsupervised Outlier Detection: Measures, Datasets, and an Empirical Study’. Data Mining and Knowledge Discovery 30, no. 4 (2016): 891–927.

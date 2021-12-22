# Specification of K-Means Anomaly Detection Method 

The K-Means Anomaly Detection method (KMC) is based on calculating clusters that model normal behavior. Each cluster is represented by its centroid and a radius of acceptance. If the observed pattern lies in a cluster, it is accepted as correct. Points that lie far from existing clusters are called anomalous. To increase the robustness of the method while maintaining good sensitivity, the profile consists of a set of models. Dimensional reduction is also applied to the input data, which provides the following advantages: i) the PCA method allows to preserve the most important data characteristic, which is suitable for ICS input, because it contains a large amount of redundancy, ii) allows data visualization. patterns that help develop and debug detection methods.

## Introduction

The KMC method uses a clustering algorithm (K-Means++) to model normal traffic as a collection of clusters. Based on identified clusters of normal flows, it is possible to classify the new flow either as normal or unknown. If the method also contains the model of malicious flows then we can classify flows as normal, malicious and unknown.
The problem for the clustering algorithm is to discover groups of members that are well separated. Also, we would like to have a way to decide the optimal number of clusters that fits a data set. Note that more cluster instances do not correspond to better clusterization. When clusters are identified in the phase of model training we use a validation algorithm to evaluate each possible solution and to select the most suitable one. Clustering validity can be computed using different algorithms, e.g., Silhouette index,  Dunn index or Davies–Bouldin index.

Given an input dataset for which the profile is to be created the K-means++ algorithm is applied to produce a specified number of clusters that best cover the input points. This works nicely if the input data fits in the clusters and there are not many outliers. However, for real ICS communication, most of the data form such clusters but there are few that do not fit well. This situation represents the problem as these legitimate data points cause profile violation and thus increase the false positive rate. The proposed solution is to create more than a single model for the normal communication using the same method (K-Means++) but with different settings, e.g., number of clusters. The profile is a collection of models that all are used for anomaly detection. The deviation from normal behavior is only reported if none of the models can match the input sample.  

By dimensionality reduction it is possible to spot differences in the input data and represented them using a set of clusters. This steps enables to identify a subtle deviations from the 
normal behavior thus increasing the detection capabilities.
Model composition solves the problem of high false positiveness of the method as outliers in the normal traffic are hard to capture by a single model. 
Instead by creating multiple models the different cluster organizations are obtained which enables to cover variance in the source data and include most of the outliers presented. 

As an added value the parameters such as number of clusters, number and the minimal precision of each model in the ensemble enables to tune the profile in terms of accuracy, acceptance and robustness as necessary.

## PCA Dimensionality Reduction

PCA method is one of the main techniques for dimensionality reduction that does a linear mapping, which maximizes a variance of the data in the low-dimensional representation.
Using this kind of transformation provides the following benefits:

* mapping enables to identify variance in data that would be otherwise hard to separate, which enables the possibility to detect even small difference to normal traffic patterns
* space can be visualized that enables debugging of the profile and provides an additional insight 

## Ensemble of Models in the Profile

Instead of using a single model, the number of candidate  models is computed for the input data to form an ensemble of models. This idea enables also to alter the profile by adding more models later. Moreover, it can be possible to recompute the profile from a collection of models by merging or splitting clusters of existing models (future work).

## Creating a model of a normal behavior

The IPFIX records representing the normal behavior are clustered using the K-means algorithm. Each cluster identifies a conversation pattern. The input to profile construction algorithm is a feature vector that is created from selected Modbus IPFIX fields. The vector consists of the following columns (each value is a float number):

| Field | Description  |
| ----- | ------------ |
| ForwardDuration | The duration of the forward flow in seconds. |
| ReverseDuration | The duration of the reverse flow in seconds. |
| ForwardPackets | Number of packets of the forward flow.|
| ReversePackets | Number of packets of the reverse flow.|
| ForwardOctets | Amount of bytes of the forward flow.|
| ReverseOctets | Amount of bytes of the reverse flow.|
| ReadRequests | A number of READ messages requested by the Master unit.|
| WriteRequests  | A number of WRITE  messages requested by the Master unit.|
| DiagnosticRequests | A number of DIAGNOSTIC messages request by the Master unit.|
| OtherRequests | A number of other messages requested by the Master unit.|
| UndefinedRequests | A number of requests whose function type is undefined.|
| MalformedRequests | A number of malformed request messages.|
| ResponsesSuccess | A number of confirmation messages responded by the Slave unit.|
| ResponsesError | A number of error responses responded by the Slave unit.|
| MalformedResponses | A number of malformed response messages.|

As in ICS, the communication is rather monotonic, and we can expect that most of the fields of this vector will have 0 or some (almost) constant value.
To reduce the dimension of the vector and to find the most significant contribution of the input features, we apply [PCA transformation](https://scikit-learn.org/stable/modules/generated/sklearn.decomposition.PCA.html) to this vector, which gives us a vector of a few fields (based on experiments with several datasets we found rank=3 as a suitable value).

It means that the input to the K-Means++ algorithm will be 3-dimensional vectors of PCA features (not the original IPFIX features). Clusters are computed for the predefined parameters K, default is K = {2,4,8}. This gives us three models that describe the patterns in the input data but each with a different number of clusters. For each cluster, we compute its variance for all points of the cluster. 
While it can be possible after the test of acceptance of input points by the computed clusters of all models to reduce the profile by removing the least populated clusters, it is better to retain all clusters. The number of clusters is not usually large, and the computation of the distance of a point to each centroid of clusters is cheap. The advantage is that the profile consisting of several models has fewer false positives. In fact, for all tested datasets it was possible to achieve full coverage of normal patterns by computing clusters for the default configuration.

The algorithm for computing the profile considering a fixed set of models given by parameters for their K values is as follows:
```python
# loads input data as a collection of protocol features
input = load("INPUT_FILE");

# scales the input data to (0..1) interval
var scaler = new MinMaxScaler()
scaler.fit(input)

# reduce dimensions and get pca vectors
pca = PCA(n_components=3)
pca.fit(scaler)

# fit k-means 
c2 = kmeans_plusplus(pca, n_clusters=2)
c4 = kmeans_plusplus(pca, n_clusters=4)
c8 = kmeans_plusplus(pca, n_clusters=8)

v2 = # variance vector for c2
v4 = # variance vector for c4
v8 = # variance vector for c8

profile = (c2,v2,c4,v4,c8,v8)
```
Variance is computed over all items of the cluster for each model as:
```python
def get_variance(distances):
    if len(distances) > 1:
        list(map(lambda a : math.pow(s, 2)), distances)/len(distances))
    else[]
        sys.float_info.epsilon
```
Note that variance is never 0, but even for the single item it is set to the smallest non-zero positive value.
For example, if a cluster with centroid c2 = (0.5, 0.5, 0.5) has assigned points 
`[(0.51,0.56,0.52),(0.55,0.54,0.53),(0.52,0.56,0.52)]` the computed variance is `0.0045`. 
The variance value is important as it provides the value used to determine the distance from the cluster centroid for samples acceptance.
For instance, given point `(0.6,0.7,0.6)` its distance is `0.24494897427831774` which is outside of the cluster diameter, if it is set to `3*Math.Sqrt(v)`. 

The profile consists of a given number of models. Each model is a collection of clusters as learned by the K-Means++ algorithm. Obviously, the clusters of different models overlap. However, we can take this as an advantage as it better covers the normal samples even if they contain outliers. Also if a sample is covered by multiple clusters it represents a more common traffic pattern. The experiments demonstrated that using the ensemble of models we can achieve better coverage of normal patterns without compromising the method sensitivity to anomalies.

Note on possible optimization: Not all clusters may be necessary. It may be possible to determine the smallest set of clusters that covers all nodes in the training dataset and remove redundant clusters (future work). 

An example of set of generated clusters:
```
v2 = [0.010674240067601204, 4.4955595512874424E-06]
c2 = [(0.17029736936092377, 0.0057853814214468002, 0.00087752460967749357),
      (2.4674854278564453, 0.085474930703639984, 0.020355600863695145)]
v4 = [4.4955595512874424E-06,0.00011407816054997966,0.0001844313956098631, 0.0001416708983015269]
c4 = [(2.4674854278564453, -0.085474930703639984, 0.020355600863695145)
      (-0.13641324639320374, 0.15974605083465576, -0.00050400808686390519)
      (-0.3070407509803772, -0.57111120223999023, 0.012006569653749466), 
      (-0.20962771773338318,-0.18032638728618622,-0.0037096540909260511)]
v8 = ...
v4 = ...
```
## Detecting anomalies with the profile

The constructed profile of a system is used to test the observed traffic patterns and identify anomalies.
The detection is based on computing distances of an input sample to all cluster centroids among all models.
If a profile consists of three models (for k = 2,4,8)  distances of a sample are computed to all 14 clusters.
Each cluster has a variance value that is used to decide whether the sample is close enough to the center of a cluster to be accepted. 
An acceptance radius r is computed from the variance as `r = k * Math.sqrt(v)` where k is a multiplier that can be used to adjust the threshold. 
If k=1 then the radius corresponds to the standard deviation. Considering that the values follow the normal distribution then using k = 3 covers 99.6% of samples. 

Instead of checking whether the sample lies in the radius of a cluster directly, we compute the score, which determines how much the sample fits the given cluster:

```python
def get_score(distance, variance):
    1 - Math.min(distance / (6 * Math.sqrt(variance)), 1)
```

The score is value in (0,1). Value 1 means that the sample is in the center of the cluster, while 0 means that it is very far. In the presented computation value 0.5 can be considered as borderline as it corresponds to the radius `r = 3 * Math.sqrt(v)`, which covers 99.6% of normal samples. 
The threshold value t used to accept or reject the sample can be adjusted as necessary.

Computing scores of a sample for all clusters yields to a score vector `sv = (s1,s2,...sn)`. To decide whether the sample represents a normal pattern the decision may be based on the selected aggregation function. the simple approach may find the maximum score and compare it with the predefined threshold value. Alternatively, it is possible to take an average score that better reflects the fitness of the sample in the given profile.  

For instance, the following flow matches the profile because it score is `0.96594609184310154` for the third model. The first and second models have lower scores but still high enough to pass the threshold. 
```
- flowKey: Tcp@192.168.1.99:2129-192.168.1.101:502
  scores:
  - 0.88670625626729549
  - 0.80757623774672227
  - 0.96594609184310154
  maxScore: 0.96594609184310154
  minScore: 0.80757623774672227
  avgScore: 0.88674286195237306
```

## Parameters of the Method
The method can be parameterized to adjust its detection capabilities. The following are parameters of K-means method:
* Number of clusters - target number of clusters. While more clusters can describe more traffic patterns precisely, it makes the model more complex and less robust
* Initialization value of K-means algorithm - the algorithm is started with initialization value (usually some random number).The initialization value can influence the cluster composition. Alternatively it can be possible to use K-Means++ algorithm that promises better algorithm intialization approach. 
* Feature set - selected fields of the IPFIX template and their representation. It is possible to use only a subset of fields or even compute features as a combination of a number of IPFIX fields.  

To come up with a suitable model we execute cluster validation for different parameters. As the main parameter we consider the number of clusters. The optimization algorithm computes the validation indices for different numbers of clusters and selects the one for the best result. For instance, the popular is silhouette algorithm that can provide a single scalar value assessing the quality of clustering wrt number of groups.

## Auto Model Selection

The profile consists of N models. To identify the best available models it is possible to compute M different (for M > N) models and select N models that best describe the source flows. The procedure for selecting best models are as follows:

* For the given source flows compute N models for the given set of K-values. For instance, compute models for K = (3,8). Thus we get up to M=6 models. 
* Compute DB-index for every model and order the models according to this index.
* Select first M models ordered by DB-index. These models will comprise the profile.

The resulting collection of models will be used to classify the flows. As a result of flow classification by the profile we obtain a vector of scores for each model.   

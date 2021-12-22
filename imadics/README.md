
# IPFIX Monitoring and Anomaly Detection in Industrial Control Systems (IMADICS)

This repository contains an implementation of network anomaly detection method for industrial communication. The method builds a profile for normal communication. Anomalies are identified as the significant differences in communication patterns to this profile. 
IPFIX flows are the source of the monitored communication. Instead of analyzing every single packet of the communication the method identifies anomalies in IPFIX representation of ICS conversations. The presented method can work with different amount of available information about a flow.  

## Documentation

 The basic idea of the implemented methods is to use a suitable model to characterize network communication patterns representing common behavior. For example, K-Means is one of the methods that computes the set of clusters that best matches normal behavior. The clusters represent a profile that the detection method then uses to check whether the observed communication patterns are normal, ie they belong to the profile clusters or are further from some cluster and thus represent anomalies. Details of the implemented methods are described in a separate [document](doc/Readme.md).


## Source Codes

The method was implemented as a software prototype. All source codes of the method are available at [source/IcsMonitor](source/IcsMonitor) folder. The prototype implementation targets  .NET Core >5. The project is self-contained thus it does not require any external tool to be installed on the target system. Because of .NET Core portability the tool is executable on all major OSes.  

## Datasets

For experiments and evaluation datasets consisting of ICS communication are necessary. The existing publicly available datasets of various ICS communication were collected and flow information was extracted. Also, we created a new dataset by using Factory.IO simulator of industrial processes that enables us to customize them as necessary for performing different types of experiments. Overall, we consider the following datasets Lemay, tjcruz-dei, 4SICS, 2017QUT, DigitalBond, DEFCON23 and our factory20. The extracted flow records in form of CSV files are located in [data](data) folder.

## Experiments

The demonstration and evaluation of proposed methods are done by using Notebooks. Notebooks can be open in Visual Studio Code with .NET Interactive extension installed. A notebook usually contains an experiment with a specific dataset and provides either demonstration or evaluation of properties. See [scripts](scripts/Readme.md) folder for details.

## Acknowledgement

The  project was supported by grant *Security monitoring of ICS communication in the smart grid*, MV CR, VI20192022138, 2019-2022.

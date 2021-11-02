#!/bin/bash

#Assumes detano installed using `detano-install.sh`

GREEN='\033[0;32m'
NC='\033[0m'

cd
#Download dataset library
git clone https://github.com/matousp/datasets.git
cd detano/src

echo -e "${GREEN}Example of automata learning -- PA${NC}"
./pa_learning.py ../../datasets/scada-iec104/iec104-traffic/10122018-104Mega-ioa.csv --atype=pa --format=ipfix

echo -e "${GREEN}Example of automata learning -- PTA${NC}"
./pa_learning.py ../../datasets/scada-iec104/iec104-traffic/10122018-104Mega-ioa.csv --atype=pta --format=ipfix

echo -e "${GREEN}Example of anomaly detection -- Single message reasoning${NC}"
./anomaly_check.py ../../datasets/scada-iec104/attacks/normal-traffic.csv ../../datasets/scada-iec104/attacks/scanning-attack.csv --atype=pa --alg=member

echo -e "${GREEN}Example of anomaly detection -- Distribution comparison${NC}"
./anomaly_check.py ../../datasets/scada-iec104/attacks/normal-traffic.csv ../../datasets/scada-iec104/attacks/scanning-attack.csv --atype=pa --alg=distr --smoothing

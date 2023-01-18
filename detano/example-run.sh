#!/bin/bash

#Assumes detano installed using `detano-install.sh`

GREEN='\033[0;32m'
NC='\033[0m'

dir=`pwd`
if ! [ -d "datasets" ]; then
	#Download dataset library
	git clone https://github.com/matousp/datasets.git
fi
cd ${dir}/src

echo -e "${GREEN}Example of automata learning -- PA${NC}"
python3.9 pa_learning.py ~/datasets/scada-iec104/iec104-traffic/10122018-104Mega.csv --atype=pa --format=ipfix

echo -e "${GREEN}Example of automata learning -- PTA${NC}"
python3.9 pa_learning.py ~/datasets/scada-iec104/iec104-traffic/10122018-104Mega.csv --atype=pta --format=ipfix

echo -e "${GREEN}Example of anomaly detection -- Single message reasoning${NC}"
python3.9 anomaly_check.py ~/datasets/scada-iec104/attacks/normal-traffic.csv ~/datasets/scada-iec104/attacks/scanning-attack.csv --atype=pa --format=ipfix --alg=member

echo -e "${GREEN}Example of anomaly detection -- Distribution comparison${NC}"
python3.9 anomaly_check.py ~/datasets/scada-iec104/attacks/normal-traffic.csv ~/datasets/scada-iec104/attacks/scanning-attack.csv --atype=pa --format=ipfix --alg=distr --smoothing

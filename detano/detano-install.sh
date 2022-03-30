#!/bin/bash

#Install system packages
sudo apt-get update
sudo apt-get install python3.9
sudo apt install python3-pip

#Install python packages
#git clone https://github.com/vhavlena/detano.git
python3.9 -m pip install -r requirements.txt

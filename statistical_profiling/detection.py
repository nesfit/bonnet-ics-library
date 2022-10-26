#!/usr/bin/env python3

"""!
This script found the anomalies according to the statistical profile

\author
    Ivana Burgetová

\copyright
Copyright (C) 2021  Ivana Burgetová, <burgetova@fit.vutbr.cz>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License.
If not, see <http://www.gnu.org/licenses/>.

Input parameters:
-f input_file with industrial communication (csv format, each line represents one packet).
   First four columns (separated with semicolon) should contain following values:
   TimeStamp; RelativeTime; srcIP; dstIP
-p file with statistical profiles for the given traffic (created with script statistical_modeling.py)
-t size of time window in seconds (default value = 300)

Output:
Numbers of time windows in which the anomaly was found.
"""

from argparse import ArgumentParser
import statistical_modeling_functions as smf



# Global variables:
## dictionary of the lists that capture one-directinal communication
traffic_dict = {}
## dictionary of the lists that capture one-directinal communication with added inter-arrival times
split_traffic_dict = {}
## dictionary of statistical descriptions (one item per each one-directional communication)
profiles_dict = {}
## dictionary of the anomaly time windows.
outliers_by_char_dict = {}

parser = ArgumentParser(description='The argument -f is required to specify the input file.')
parser.add_argument("-f", "--input_file", required=True, help='the input file with IEC104 data in csv format')
parser.add_argument("-p", "--profiles_file", required=True, help='the file with statistical profiles')
parser.add_argument("-t", "--time_window_size", default=300, help='size of the time window in seconds')

args = parser.parse_args()
input_file_name = args.input_file
profiles_file_name = args.profiles_file
time_window_size = args.time_window_size

smf.process_profiles_file(profiles_file_name, profiles_dict)
smf.process_traffic_file(input_file_name, traffic_dict)  # traffic into traffic_dict
smf.add_delta_time_and_split_directions(traffic_dict, split_traffic_dict)
for key in split_traffic_dict:
    if key not in profiles_dict:
        output = key + ": no profile available for the communication."
        print(output)
    elif len(profiles_dict[key]) < 7:
        output = key + ": available profile does not contain all required information."
        output = output + str(len(profiles_dict[key]))
        print(output)
    else:
        outliers_by_char_dict = {}
        smf.detect_all_outliers(split_traffic_dict[key], profiles_dict[key], time_window_size, outliers_by_char_dict)
        output = key + ": detected outliers:"
        print(output)
        for key in outliers_by_char_dict:
            output = "characterictic " + key + " :"
            for item in outliers_by_char_dict[key]:
                output = output + str(item) + " "
            print(output)

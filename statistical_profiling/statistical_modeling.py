# Created in December 2021 by Ivana Burgetova
# Project: BONNET - statistical profiling

"""
This script found the statistical profiles of the communications and their directions in the input file.
Input parameters:
-f input_file
-t size of time window in seconds (default value = 300)

Output: statistical profiles for individual communications and their directions
- each line contains the profile for one comunication and direction
- profile consists of 7 values: split point of inter-arrival times, lower and upper boundaries for:
-- total number of transmitted packets in time window
-- number of packets with split-time smaller than split-point transmitted packets in time window
-- number of packets with split-time grater than or equal to split-point transmitted packets in time window
"""

from argparse import ArgumentParser
import statistical_modeling_functions as smf

traffic_dict = {}
split_traffic_dict = {}
candidate_split_points_dict = {}
profiles_dict = {}
max_time = 0
max_std = 0
best_boundary = 0

# definition of the arguments
parser = ArgumentParser(description='The argument -f is required to specify the input file.')
parser.add_argument("-f", "--input_file", required=True, help='the input file with IEC104 data in csv format')
parser.add_argument("-t", "--time_window_size", default=300, help='size of the time window in seconds')

args = parser.parse_args()
input_file_name = args.input_file
time_window_size = args.time_window_size

smf.process_traffic_file(input_file_name, traffic_dict)  # traffic into traffic_dict
smf.add_delta_time_and_split_directions(traffic_dict, split_traffic_dict)
smf.delta_time_statistics(split_traffic_dict, candidate_split_points_dict)

for key in split_traffic_dict:
    input_list = split_traffic_dict[key]
    split_points_results_dict = {}
    profile_list = []
    for candidate in candidate_split_points_dict[key]:
        if candidate > 0.0:
            max_std = smf.split_point_statistics(input_list, candidate, time_window_size, split_points_results_dict, max_std)
    best_split_point = smf.select_split_point(split_points_results_dict, max_std)
    profile_list = smf.final_traffic_statistics(input_list, best_split_point, time_window_size)
    profiles_dict[key] = profile_list

for key in profiles_dict:
    output = key + ";"
    for item in profiles_dict[key]:
        output = output + str(item) + ";"
    print(output)
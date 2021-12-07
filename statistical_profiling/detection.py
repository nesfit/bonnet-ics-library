# Created in December 2021 by Ivana Burgetova
# Project: BONNET - statistical profiling

"""
This script found the statistical profiles of the communications and their directions in the input file.
Input parameters:
-f input_file
-p file with statistical profiles for the given traffic
-t size of time window in seconds (default value = 300)

Output:
"""

from argparse import ArgumentParser
import statistical_modeling_functions as smf

traffic_dict = {}
split_traffic_dict = {}
profiles_dict = {}
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
        #process outliers_by_char_dict and print the output
        output = key + ": detected outliers:"
        print(output)
        for key in outliers_by_char_dict:
            output = "characterictic " + key + " :"
            for item in outliers_by_char_dict[key]:
                output = output + str(item) + " "
            print(output)

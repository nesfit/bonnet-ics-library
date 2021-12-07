# Created in December 2021 by Ivana Burgetova
# Project: BONNET - statistical profiling

"""
Module with auxiliary functions for statistical_modelling.py and detection.py.
"""

import pandas as pd


def process_traffic_file(file_name, traffic_dict):
"""
"""
    # separate the communications from the input file to the traffic_dict - one key for both directions
    i_file = open(file_name, "r")
    for line in i_file:
        line2 = line.strip()
        fields = line2.split(";")
        if (fields[0] != "TimeStamp" and fields[0] != ""):  # skip header lines
            key1 = fields[2] + ":" + fields[3]  # take IP adresses as a key
            key2 = fields[3] + ":" + fields[2]
            if (key1 in traffic_dict):
                list_in_dict = traffic_dict[key1]
                list_in_dict.append((key1, float(fields[1])))
                traffic_dict[key1] = list_in_dict
            elif (key2 in traffic_dict):
                list_in_dict = traffic_dict[key2]
                list_in_dict.append((key1, float(fields[1])))
                traffic_dict[key2] = list_in_dict
            else:
                traffic_dict[key1] = [(key1, float(fields[1]))]
    i_file.close()


# end of process_traffic_file

def process_profiles_file(file_name, profiles_dict):
    # separate the communications from the input file to the traffic_dict - one key for both directions
    i_file = open(file_name, "r")
    num_of_line = 0
    for line in i_file:
        list_of_boundaries = []
        line2 = line.strip()
        fields = line2.split(";")
        key = fields[0]
        for i in range(1, 8):
            list_of_boundaries.append(float(fields[i]))
        profiles_dict[key] = list_of_boundaries
        num_of_line += 1
    i_file.close()


# end of process_traffic_file


def add_delta_time_and_split_directions(traffic_dict, split_traffic_dict):
    # add delta times and split based on the direction
    for key in traffic_dict:
        list_in_dict = traffic_dict[key]
        list_with_delta_time_1 = []
        list_with_delta_time_2 = []
        old_time = 0
        for item in list_in_dict:  # item is couple: IPadresses, relative_time
            delta_time = item[1] - old_time
            old_time = item[1]
            if item[0] == key:
                list_with_delta_time_1.append((item[0], item[1], delta_time))
            else:
                list_with_delta_time_2.append((item[0], item[1], delta_time))
                key2 = item[0]
        split_traffic_dict[key] = list_with_delta_time_1
        split_traffic_dict[key2] = list_with_delta_time_2


# end add_delta_time_and_split_directions

def delta_time_statistics(input_dict, output_dict):
    for key in input_dict:
        list_in_dict = input_dict[key]
        list_of_delta_time = []
        for item in list_in_dict:  # item is a pair of values(relative time and delta_time)
            list_of_delta_time.append(float(item[2]))
        list_of_delta_time.pop(0)  # delta time of the first packet might be wrong
        df = pd.Series(list_of_delta_time)
        # print(df.describe())
        stats = df.describe()
        output_dict[key] = [stats[4], stats[5], stats[1], stats[6]]


# end of delta_time_statistics

def gather_number_of_packets(input_list, split_point, time_window_size, dict_with_windows):
    no_of_packets = 0
    no_of_packets_range1 = 0
    no_of_packets_range2 = 0
    list_of_no_of_packets = []
    list_of_no_of_packets_range1 = []
    list_of_no_of_packets_range2 = []
    no_of_time_window = 1
    time_window_end = no_of_time_window * time_window_size
    for item in input_list:
        if float(item[1]) < time_window_end:
            no_of_packets += 1
            if float(item[2]) < split_point:
                no_of_packets_range1 += 1
            else:
                no_of_packets_range2 += 1
        else:
            no_of_time_window += 1
            time_window_end = no_of_time_window * time_window_size
            list_of_no_of_packets.append(no_of_packets)
            list_of_no_of_packets_range1.append(no_of_packets_range1)
            list_of_no_of_packets_range2.append(no_of_packets_range2)
            no_of_packets = 1
            no_of_packets_range1 = 0
            no_of_packets_range2 = 0
            if float(item[2]) < split_point:
                no_of_packets_range1 = 1
            else:
                no_of_packets_range2 = 1
    dict_with_windows['total'] = list_of_no_of_packets
    dict_with_windows['range1'] = list_of_no_of_packets_range1
    dict_with_windows['range2'] = list_of_no_of_packets_range2


# end of gather_number_of_packets

def split_point_statistics(input_list, boundary, time_window_size, results_dict, max_std):
    dict_with_windows = {}
    list_of_statistics = []
    gather_number_of_packets(input_list, boundary, time_window_size, dict_with_windows)
    for key in dict_with_windows:
        if key != 'total':
            se = pd.Series(dict_with_windows[key])
            stats = se.describe()
            list_of_statistics.append((stats[1], stats[2]))  # return couple(mean,std) for given boundary
            if stats[2] > max_std:
                max_std = stats[2]
    results_dict[boundary] = list_of_statistics  # return couple(mean,std) for given boundary
    return max_std


# end of split_point_statistics

def final_traffic_statistics(input_list, split_point, time_window_size):
    output_list = [split_point]
    dict_with_windows = {}
    gather_number_of_packets(input_list, split_point, time_window_size, dict_with_windows)
    se = pd.Series(dict_with_windows['total'])
    stats = se.describe()
    if (stats[1] > 0.34):
        lower_boundary = stats[1] - 3 * stats[2]
        upper_boundary = stats[1] + 3 * stats[2]
        # filtering out items from dict_with_windows
        list_total = []
        list_range1 = []
        list_range2 = []
        for i in range(len(dict_with_windows['total'])):
            item = dict_with_windows['total'][i]
            if (item >= lower_boundary) and (item <= upper_boundary):
                list_total.append(item)
                list_range1.append(dict_with_windows['range1'][i])
                list_range2.append(dict_with_windows['range2'][i])
        dict_with_windows['total'] = list_total
        dict_with_windows['range1'] = list_range1
        dict_with_windows['range2'] = list_range2
    # tady se pokracuje stanovenim hranic
    se_total = pd.Series(dict_with_windows['total'])
    stats = se_total.describe()
    if stats[1] < 0.34:
        stats[1] = 0.34
    lower_boundary = stats[1] - 3 * stats[2]
    upper_boundary = stats[1] + 3 * stats[2]
    output_list.append(lower_boundary)
    output_list.append(upper_boundary)
    se_range1 = pd.Series(dict_with_windows['range1'])
    stats = se_range1.describe()
    if stats[1] < 0.34:
        stats[1] = 0.34
    lower_boundary = stats[1] - 3 * stats[2]
    upper_boundary = stats[1] + 3 * stats[2]
    output_list.append(lower_boundary)
    output_list.append(upper_boundary)
    se_range2 = pd.Series(dict_with_windows['range2'])
    stats = se_range2.describe()
    if stats[1] < 0.34:
        stats[1] = 0.34
    lower_boundary = stats[1] - 3 * stats[2]
    upper_boundary = stats[1] + 3 * stats[2]
    output_list.append(lower_boundary)
    output_list.append(upper_boundary)
    return output_list


# end final_traffic_statistics

def select_split_point(statistics_dict, max_std):
    best_std = max_std
    best_boundary = 0
    for key in statistics_dict:
        for couple in statistics_dict[key]:
            if (couple[1] < best_std) and (couple[0] - 3 * couple[1]) > 0:
                best_std = couple[1]
                best_boundary = key
    return best_boundary


def detect_outliers(input_list, lower_boundary, upper_boundary):
    output_list = []
    offset = 0
    for value in input_list:
        offset += 1
        if offset == 1:
            value1 = value
        elif offset == 2:
            value2 = value
        else:
            value3 = value
            outliers_in_window = 0
            if (value1 < lower_boundary) or (value1 > upper_boundary):
                outliers_in_window += 1
            if (value2 < lower_boundary) or (value2 > upper_boundary):
                outliers_in_window += 1
            if (value3 < lower_boundary) or (value3 > upper_boundary):
                outliers_in_window += 1
            if (outliers_in_window > 1):
                output_list.append(offset - 2)
            value1 = value2
            value2 = value3
    return output_list


# end of detect_outliers

def detect_all_outliers(input_list, boundaries_list, time_window_size, output_dict):
    dict_with_windows = {}
    split_point = boundaries_list[0]
    gather_number_of_packets(input_list, split_point, time_window_size, dict_with_windows)
    lower_boundary = boundaries_list[1]
    upper_boundary = boundaries_list[2]
    list_of_outliers = detect_outliers(dict_with_windows["total"], lower_boundary, upper_boundary)
    output_dict["total"] = list_of_outliers
    lower_boundary = boundaries_list[3]
    upper_boundary = boundaries_list[4]
    list_of_outliers = detect_outliers(dict_with_windows["range1"], lower_boundary, upper_boundary)
    output_dict["range1"] = list_of_outliers
    lower_boundary = boundaries_list[5]
    upper_boundary = boundaries_list[6]
    list_of_outliers = detect_outliers(dict_with_windows["range2"], lower_boundary, upper_boundary)
    output_dict["range2"] = list_of_outliers
# end of detect_all_outliers

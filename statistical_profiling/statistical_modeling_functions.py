#!/usr/bin/env python3

"""!
Module with auxiliary functions for statistical_modelling.py and detection.py.

\author
    Ivana Burgetová

\copyright
    Copyright (C) 2021  Ivana Burgetova, <burgetova@fit.vutbr.cz>
"""

import pandas as pd


def process_traffic_file(file_name, traffic_dict):
    """!Separates the communications from the input file to the traffic-dict.

    Communication is identified from third and fourth column of the input file (IP adresses).
    For each pair of devices one item (with one key) in output dictionary is created.
    For each communication - relative time and the direction is stored for each packet.
    Directions are not distinguished yet.

    @param file_name: name of the csv file with network traffic (one line per packet)
    @param traffic_dict: output parameter, dictionary of conversations, stores relative time and directions
   
    @return traffic_dict
   
    """
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
    """!Separates the profiles that will be used for anomaly detection.

    Converts the information about comunnication profiles into dictionary.
    Split point and boundaries of individual characteristics are stored for each conversation.

    @param file_name: name of the file with statistical model
    @param profiles_dict: output parameter, dictionary of statistical models
   
    @return profiles_dict
   
    """    
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
    """!Finds inter-arrival time for each packet and splits the communication into directions.
    
    Inter-arrival times are computed from relative times in bidirectional traffic.
    Next, communications are divided by direction to the output dictionary.
    
    @param traffic_dict: dictionary of bidirectional traffic without inter-arrival times
    @param split_traffic_dict: dictionary of one-directional traffic with inter-arrival times
    
    @return split_traffic_dict
    
    """
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
    """!Finds the quartiles and mean of inter-arrival times.
    
    For each item in input_dict (one-directional traffic), quartiles and mean are found and stored in output_dictinary.
    
    @param input_dict: should contain list of items for individual directions
    @param output_dict: output parameter that returns values of medians and mean as a list of values for each direction
    
    @return output_dict
    
    """
    for key in input_dict:
        list_in_dict = input_dict[key]
        list_of_delta_time = []
        for item in list_in_dict:  # item is a triple of values(IPadresses, relative time and delta_time)
            list_of_delta_time.append(float(item[2]))
        list_of_delta_time.pop(0)  # delta time of the first packet might be wrong
        df = pd.Series(list_of_delta_time)
        stats = df.describe()
        output_dict[key] = [stats[4], stats[5], stats[1], stats[6]]
# end of delta_time_statistics

def gather_number_of_packets(input_list, split_point, time_window_size, dict_with_windows):
    """!Counts the number of packets transmitted within all time windows of a given size.
    
    Converts the time-series of packets into series of number of packets transmitted within consecutive time windows.
    Besides the total number of packets, also the number of packets with inter-arrival time smaller than split-point
    and the number of packets with inter-arrival time greater than or equal to split-point within each time window are found.
    Three resulting series of values are returned in dict_with_windows.
    
    @param input_list: list of transmitted packets with relative time and inter-arrival time
    @param split_point: value used to separate transmitted packets to two groups according to inter-arrival time
    @param time_window_size: window size in seconds
    @param dict_with_windows: output parameter that returns three series of values as a dictinary
     
    @return dict_with_windows
    
    """
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
    """! Finds mean and standard deviation of the series obtained for the given boundary (split-point) value.
    
    The packets from input_list are transformed to series of the number of packets.
    The mean and standard deviation are found for series 'range1' and 'range2'.
    They are used to find the most suitable split-point.
    They are returned in result_dict as values for the key 'boundary'.      
    
    @param input_list: list of packets in one-directional traffic (with inter-arrival times)
    @param boundary: used as a candidate split-point
    @param time_window_size: size of time window in seconds
    @param results_dict: for each boundary (used as a key) contains the mean and standard deviation of resulted series
                        serve as input-output parameter
    @param max_std: largest standard deviation found so far
    
    @return max_std, results_dict
    
    """
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
    """!Finds the final statistical profile of the given one-directional traffic.

    The time-window representation of the traffic is found. Then outliers are removed (using 3-sigma rule).
    Statistical profile consisting of the split-point value and boundaries of the ranges of normal values
    for all three characteristics is found (using 3-sigma rule) and returned.

    @param input_list: list of packets in one-directional traffic (with inter-arrival times)
    @param split_point: value used to separate transmitted packets to two groups according to inter-arrival time
    @param time_window_size: size of time window in seconds

    @return output_list: list of values that compose the profile
    """
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
    """!Select the best split point from the candidates.

    For each candidate split-point (key) the resulting mean and standard deviation are stored in statistics_dict.
    The best split-point leads to the smallest standard deviation
    and defines the lower boundary of the final range of normal values greater then zero.

    @param statistics_dict: dictionary with means and standard deviations for each candidate
    @param max_std: largest standard deviation found so far, used for initialization

    @return best_boundary: value of the best split-point of inter-arrival times
    """
    best_std = max_std
    best_boundary = 0
    for key in statistics_dict:
        for couple in statistics_dict[key]:
            if (couple[1] < best_std) and (couple[0] - 3 * couple[1]) > 0:
                best_std = couple[1]
                best_boundary = key
    return best_boundary
# end select_split_point

def detect_outliers(input_list, lower_boundary, upper_boundary):
    """!Detects outlier values in one characteristic of the traffic.

    Values from input_list are compared with boundaries. 3-value-detection method is used.
    If two out of three consecutive values are outside the specified range, an anomaly is reported
    (added to output_list and returned).

    @param input_list: list of values in which anomalies are searched for
    @param lower_boundary: specifies the lower limit of the range of normal values
    @param upper_boundary: specifies the upper limit of the range of normal values

    @return output_list: list of anomalies
    """
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
    """!Detects outliers in time windows series for all three characteristics.

    The time-window representation of the traffic is found. For each characteristic the method detect_outliers()
    is called. The numbers of windows where anomaly occur are returned in output_dict.

    @param input_list: list of packets in one-directional traffic (with inter-arrival times)
    @param boundaries_list: the list of values that represent tha statistical profile
    @param time_window_size: size of time window in seconds
    @param output_dict: the dictionary with outliers (time windows with anomaly)

    @return output_dict
    """
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

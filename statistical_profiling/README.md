# Statistical profiling

The tool consist of three files:

* `statistical_modeling_functions.py` - a module with functions used by other scripts.
* `statistical_modeling.py` - creates the statistical model(s) for the provided traffic. 
* `detection.py` - detects the anomalies in the given traffic with the respect to the specified profile. 

## Requirements

* Python - version 3.9
* Pandas - version 1.2.4

## Traffic model creation

The statistical model can be created with `statistical_modeling.py` script.
The model consists of the profiles for each pair of IP adresses and for each direction. 
Profiles are printed to standard output, one per line.

### Parameters:

`-f`: specifies the file with IEC104 data in csv format, required parameter
`-t`: allows to specify the size of the time window in seconds, optional parametr, default value = 300 seconds

### Example usage:

```bash
python statistical_modeling.py -f datasets/mega104-17-12-18-ioa.csv > mega104-17-12-18-profile.csv
```

Creates profiles of the communications captured in the file `datasets/mega104-17-12-18-ioa.csv`.
For each pair of IP adresses and for each direction, one profile is derived. Profiles are stored one per line.

## Anomalies detection:

Anomalies can be detected with the `detection.py` script.

### Parameters:

`-f`: specify the file with IEC104 data in csv format, where anomalies should be found, required parameter
`-p`: specify the file with communications profiles, that will be used to find the anomalies, required parametr
`-t`: allows to specify the size of the time window in seconds, optinal parametr, default value = 300 seconds

### Example usage: 

```
python detection.py -f attacks/connection-loss.csv -p 17-12-18-profiles.csv
```

The script compares the traffic captured in file `connection-loss.csv` against the profile stored in file `17-12-18-profiles.csv`.
Time windows that do not fit into ranges defined in profiles are printed to standard output.

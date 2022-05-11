SET EXE=..\..\source\IcsMonitor\bin\Debug\net5.0\IcsMonitor.exe
REM Create normal traffic profiles:
%EXE% Build-Profile -R "bonnet\normal-traffic.csv" -t "00:05:00" -m Centroids -p Iec  -w "profiles\bonnet_normal_traffic.profile.zip"
%EXE% Build-Profile -R "flowmon\01-iec104-normal.csv" -t "00:01:00" -m Centroids -p Iec  -w "profiles\flowmon_normal_traffic.profile.zip"

REM Compute scores for BONNET:
%EXE% Test-Flows -R "bonnet\normal-traffic.csv" -p "profiles\bonnet_normal_traffic.profile.zip" -f csv > "bonnet\normal-traffic.score.csv"

%EXE% Test-Flows -R "bonnet\connection-loss.csv" -p "profiles\bonnet_normal_traffic.profile.zip" -f csv > "bonnet\connection-loss.score.csv"
%EXE% Test-Flows -R "bonnet\dos-attack.csv" -p "profiles\bonnet_normal_traffic.profile.zip" -f csv > "bonnet\dos-attack.score.csv"
%EXE% Test-Flows -R "bonnet\injection-attack.csv" -p "profiles\bonnet_normal_traffic.profile.zip" -f csv > "bonnet\injection-attack.score.csv"
%EXE% Test-Flows -R "bonnet\rogue-device.csv" -p "profiles\bonnet_normal_traffic.profile.zip" -f csv > "bonnet\rogue-device.score.csv"
%EXE% Test-Flows -R "bonnet\scanning-attack.csv" -p "profiles\bonnet_normal_traffic.profile.zip" -f csv > "bonnet\scanning-attack.score.csv"
%EXE% Test-Flows -R "bonnet\switching-attack.csv" -p "profiles\bonnet_normal_traffic.profile.zip" -f csv > "bonnet\switching-attack.score.csv"

REM Compute scores for FLOWMON:
%EXE% Test-Flows -R "flowmon\01-iec104-normal.csv" -p "profiles\flowmon_normal_traffic.profile.zip" -f csv > "flowmon\01-iec104-normal.score.csv"
SET EXE=..\..\source\IcsMonitor\bin\Debug\net5.0\IcsMonitor.exe
REM Create normal traffic profiles:
%EXE% Build-Profile -R "bonnet\normal-traffic.csv" -t "00:05:00" -m Centroids -p Iec  -w "profiles\bonnet_normal_traffic.direct.profile.zip"
%EXE% Build-Profile -R "flowmon\01-iec104-normal.csv" -t "00:01:00" -m Centroids -p Iec  -w "profiles\flowmon_normal_traffic.direct.profile.zip"

REM Compute scores for BONNET:
%EXE% Test-Flows -R "bonnet\normal-traffic.csv" -p "profiles\bonnet_normal_traffic.direct.profile.zip" -f csv > "bonnet\normal-traffic.direct.score.csv"

%EXE% Test-Flows -R "bonnet\connection-loss.csv" -p "profiles\bonnet_normal_traffic.direct.profile.zip" -f csv > "bonnet\connection-loss.direct.score.csv"
%EXE% Test-Flows -R "bonnet\dos-attack.csv" -p "profiles\bonnet_normal_traffic.direct.profile.zip" -f csv > "bonnet\dos-attack.direct.score.csv"
%EXE% Test-Flows -R "bonnet\injection-attack.csv" -p "profiles\bonnet_normal_traffic.direct.profile.zip" -f csv > "bonnet\injection-attack.direct.score.csv"
%EXE% Test-Flows -R "bonnet\rogue-device.csv" -p "profiles\bonnet_normal_traffic.direct.profile.zip" -f csv > "bonnet\rogue-device.direct.score.csv"
%EXE% Test-Flows -R "bonnet\scanning-attack.csv" -p "profiles\bonnet_normal_traffic.direct.profile.zip" -f csv > "bonnet\scanning-attack.direct.score.csv"
%EXE% Test-Flows -R "bonnet\switching-attack.csv" -p "profiles\bonnet_normal_traffic.direct.profile.zip" -f csv > "bonnet\switching-attack.direct.score.csv"

REM Compute scores for FLOWMON:
%EXE% Test-Flows -R "flowmon\01-iec104-normal.csv" -p "profiles\flowmon_normal_traffic.direct.profile.zip" -f csv > "flowmon\01-iec104-normal.direct.score.csv"
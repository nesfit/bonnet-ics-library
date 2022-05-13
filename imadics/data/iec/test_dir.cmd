SET EXE=..\..\source\IcsMonitor\bin\Debug\net5.0\IcsMonitor.exe
SET METHOD=Centroids
SET MODE=dir
SET TH=0.1
REM Create normal traffic profiles:
%EXE% Build-Profile -R "bonnet\normal-traffic.csv" -t "00:05:00" -m %METHOD% -p Iec  -w "profiles\bonnet_normal_traffic.%MODE%.profile.zip"
%EXE% Build-Profile -R "flowmon\01-iec104-normal.csv" -t "00:01:00" -m %METHOD% -p Iec  -w "profiles\flowmon_normal_traffic.%MODE%.profile.zip"

REM Compute scores for BONNET:
%EXE% Test-Flows -R "bonnet\normal-traffic.csv" -p "profiles\bonnet_normal_traffic.%MODE%.profile.zip" -f csv -w "bonnet\normal-traffic.%MODE%.score.csv" -t %TH% -W "bonnet\normal-traffic.%MODE%.t%TH%.csv"
%EXE% Test-Flows -R "bonnet\connection-loss.csv" -p "profiles\bonnet_normal_traffic.%MODE%.profile.zip" -f csv -w "bonnet\connection-loss.%MODE%.score.csv" -t %TH% -W "bonnet\connection-loss.%MODE%.t%TH%.csv"
%EXE% Test-Flows -R "bonnet\dos-attack.csv" -p "profiles\bonnet_normal_traffic.%MODE%.profile.zip" -f csv -w "bonnet\dos-attack.%MODE%.score.csv" -t %TH% -W "bonnet\dos-attack.%MODE%.t%TH%.csv"
%EXE% Test-Flows -R "bonnet\injection-attack.csv" -p "profiles\bonnet_normal_traffic.%MODE%.profile.zip" -f csv -w "bonnet\injection-attack.%MODE%.score.csv" -t %TH% -W "bonnet\injection-attack.%MODE%.t%TH%.csv"
%EXE% Test-Flows -R "bonnet\rogue-device.csv" -p "profiles\bonnet_normal_traffic.%MODE%.profile.zip" -f csv -w "bonnet\rogue-device.%MODE%.score.csv" -t %TH% -W "bonnet\rogue-device.%MODE%.t%TH%.csv"
%EXE% Test-Flows -R "bonnet\scanning-attack.csv" -p "profiles\bonnet_normal_traffic.%MODE%.profile.zip" -f csv -w "bonnet\scanning-attack.%MODE%.score.csv" -t %TH% -W "bonnet\scanning-attack.%MODE%.t%TH%.csv"
%EXE% Test-Flows -R "bonnet\switching-attack.csv" -p "profiles\bonnet_normal_traffic.%MODE%.profile.zip" -f csv -w "bonnet\switching-attack.%MODE%.score.csv" -t %TH% -W "bonnet\switching-attack.%MODE%.t%TH%.csv"
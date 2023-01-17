
<#
.SYNOPSIS

Runs the monitor for the given scene and profile to demonstrate anomaly detection. 

.DESCRIPTION

Run-Monitor command will start capturing packets and start the currently loaded scene in Factory I/O. 
It allows you to create multiple capture files by specifying the desired number of iterations. 

Prerequisities:

This script expects that the corresponding scene is loaded in Factory.IO and MODBUS server is initialized.

Also, web server in Factory I/O must be activated with the app.web_server = True console command.

.PARAMETER Scene
A name of the scene to run in the simulation.

.PARAMETER ModbusServer
A connection string of the modbus server.

.PARAMETER FactoryAPI
End-point of the Factory I/O REST API.

.PARAMETER Profile
A path to system profile used for monitoring.

.PARAMETER Duration
How long to run the demonstration.

.INPUTS

None. You cannot pipe objects to the script.

.OUTPUTS

None. The script does not generate any output.

.EXAMPLE

PS> .\Run-Monitor.ps1 -Scene Assembler -ModbusServer "192.168.111.17:502/1" -FactoryApi "192.168.111.17:7410" -Profile .\profiles\Assembler-Norma-10s.profile -Duration "00:01:00"

#>
param (
    [Parameter(Mandatory=$true)]
    [string] $Scene,

    [Parameter(Mandatory=$true)]
    [string] $ModbusServer,
    
    [Parameter(Mandatory=$true)]
    [string] $FactoryApi,
    
    [Parameter(Mandatory=$true)]
    [string] $Profile,
    
    [Parameter(Mandatory=$true)]
    [TimeSpan] $Duration
)

# Some preparation:
$uri = "http://" + $FactoryApi + "/api/tag/values/by-name"
$factoryStop = @(
    @{
        name = "FACTORY I/O (Run)"
        value = $false
    }
)
$factoryStart = @( 
    @{
        name = "FACTORY I/O (Run)"
        value = $true  
    }
)
$jsonStop = ConvertTo-Json -InputObject $factoryStop
$jsonStart = ConvertTo-Json -InputObject $factoryStart

# Restart the Factory
Invoke-WebRequest $uri -Method Put -Body $jsonStop -ContentType 'application/json'

# Run SampleScene
Start-Process -FilePath "ModbusFactory.exe" -ArgumentList $ModbusServer,$Scene
$p = Get-Process "ModbusFactory"

# Run Factory
Invoke-WebRequest $uri -Method Put -Body $jsonStart -ContentType 'application/json'

icsmonitor.cmd Watch-Traffic -i 0 -p $Profile -f yaml -t $Duration 

Stop-Process $p

Invoke-WebRequest $uri -Method Put -Body $jsonStop -ContentType 'application/json'


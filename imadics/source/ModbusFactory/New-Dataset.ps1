
<#
.SYNOPSIS

Generate a new collection of captures for the specified scene. 

.DESCRIPTION

New-Dataset command will start capturing packets and start the currently loaded scene in Factory I/O. 
It allows you to create multiple capture files by specifying the desired number of iterations. 

This script expects that the corresponding scene is loaded in Factory.IO and MODBUS server is initialized.

.PARAMETER Scene
A name of the scene to run in the simulation.

.PARAMETER ModbusServer
A connection string of the modbus server.

.PARAMETER FactoryAPI
End-point of the Factory I/O REST API.

.PARAMETER OutputFolder
An output folder for PCAP files.

.INPUTS

None. You cannot pipe objects to the script.

.OUTPUTS

None. The script does not generate any output.

.EXAMPLE

PS> .\New-Dataset -Scene Assembler -ModbusServer "192.168.111.17:502/1" -FactoryApi "192.168.111.17:7410" -OutputFolder .\Captures -Duration 00:30:00 -RunCount 3

#>
param (
    [Parameter(Mandatory=$true)]
    [string] $Scene,

    [Parameter(Mandatory=$true)]
    [string] $ModbusServer,
    
    [Parameter(Mandatory=$true)]
    [string] $FactoryApi,
    
    [Parameter(Mandatory=$true)]
    [string] $OutputFolder,
    
    [Parameter(Mandatory=$true)]
    [TimeSpan] $Duration,
    
    [Parameter(Mandatory=$true)]
    [int] $RunCount
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

$runs = 1..$RunCount
foreach ($run in $runs){
    # Restart the Factory
    Invoke-WebRequest $uri -Method Put -Body $jsonStop -ContentType 'application/json'

    # Run SampleScene
    Start-Process -FilePath "ModbusFactory.exe" -ArgumentList $ModbusServer,$Scene
    $p = Get-Process "ModbusFactory"
    
    # Run Factory
    Invoke-WebRequest $uri -Method Put -Body $jsonStart -ContentType 'application/json'

    $outfile = [System.IO.Path]::Combine($OutputFolder, $Scene + "_" + $run + ".pcap")
    
    $runtime = $Duration.TotalSeconds 
    # Run packet capturing 
    tshark.exe -w $outfile -F pcap -a duration:$runtime "port 502"
    
    Stop-Process $p
}
Invoke-WebRequest $uri -Method Put -Body $jsonStop -ContentType 'application/json'


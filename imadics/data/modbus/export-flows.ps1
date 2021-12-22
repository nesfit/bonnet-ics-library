
<#
.SYNOPSIS

Export flows from the given PCAP files. 

.DESCRIPTION

The Export-Flows export flow data from the given collection of PCAP files 
and writes them to CSV files. It produices a number of flow exports for 
each PCAP file.

.PARAMETER InputFolder
An input folder containing the PCAP files.

.PARAMETER OutputFolder
An output folder for CSV files..

.INPUTS

None. You cannot pipe objects to the script.

.OUTPUTS

None. The script does not generate any output.

.EXAMPLE

PS> .\Export-Flows input1.pcap input2.pcap

.EXAMPLE

PS> .\Export-Flows *.pcap

#>
param (
    [Parameter(Mandatory=$true)]
    [string] $InputFolder,
    [Parameter(Mandatory=$true)]
    [string] $OutputFolder
)

$sourceFiles = Get-ChildItem -Path $InputFolder -Recurse -Include *.cap,*.pcap

$scenarios = @(
    [pscustomobject]@{time = "00:00:01"; suffix="1s"}
    [pscustomobject]@{time = "00:00:02"; suffix="2s"}
    [pscustomobject]@{time = "00:00:05"; suffix="5s"}
    [pscustomobject]@{time = "00:00:10"; suffix="10s"}
    [pscustomobject]@{time = "00:00:20"; suffix="20s"}
    [pscustomobject]@{time = "00:00:30"; suffix="30s"}
    [pscustomobject]@{time = "00:00:40"; suffix="40s"}
    [pscustomobject]@{time = "00:00:50"; suffix="50s"}
    [pscustomobject]@{time = "00:01:00"; suffix="1m"}
    [pscustomobject]@{time = "00:02:00"; suffix="2m"}
    [pscustomobject]@{time = "00:05:00"; suffix="5m"}
)

foreach ($f in $sourceFiles){
    $justName = [System.IO.Path]::GetFileNameWithoutExtension($f)
    foreach($p in $scenarios) {
        $outfile = [System.IO.Path]::Join($OutputFolder, $justName + ".flow-" + $p.suffix + ".csv") 
        IcsMonitor Export-Flows -r $f -p modbus -t $p.time -f Csv -w $outfile
    }    
}

Write-Host $sourceFiles
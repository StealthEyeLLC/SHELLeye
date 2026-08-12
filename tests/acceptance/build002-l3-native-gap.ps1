param(
  [Parameter(Mandatory=$true)][string]$PrepareJson,
  [Parameter(Mandatory=$true)][string]$OutputPath,
  [string]$Distro='Ubuntu-24.04'
)
$ErrorActionPreference='Stop'
$wsl='C:\Program Files\WSL\wsl.exe'
$prepare=Get-Content $PrepareJson -Raw | ConvertFrom-Json
$targetPid=[int]$prepare.pid
$expectedStartTicks=[string]$prepare.startTicks
$targetFile=[string]$prepare.filePath

function Invoke-Wsl([string]$Executable,[string[]]$ArgumentList) {
  $raw=& $wsl --distribution $Distro --user root --exec $Executable @ArgumentList 2>&1
  $code=$LASTEXITCODE
  [ordered]@{exitCode=$code;stdout=((($raw|Out-String)-replace [char]0,'').Trim())}
}

$boot=Invoke-Wsl '/bin/cat' @('/proc/sys/kernel/random/boot_id')
$pidns=Invoke-Wsl '/usr/bin/readlink' @('/proc/1/ns/pid')
$init=Invoke-Wsl '/usr/bin/awk' @('{print $22}','/proc/1/stat')
$processTest=Invoke-Wsl '/usr/bin/test' @('-r',"/proc/$targetPid/stat")
if($processTest.exitCode -eq 0) {
  $processStart=Invoke-Wsl '/usr/bin/awk' @('{print $22}',"/proc/$targetPid/stat")
  $processExe=Invoke-Wsl '/usr/bin/readlink' @("/proc/$targetPid/exe")
} else {
  $processStart=[ordered]@{exitCode=$processTest.exitCode;stdout='MISSING'}
  $processExe=[ordered]@{exitCode=$processTest.exitCode;stdout='MISSING'}
}
$fileTest=Invoke-Wsl '/usr/bin/test' @('-e',$targetFile)
if($fileTest.exitCode -eq 0) {
  $fileStat=Invoke-Wsl '/usr/bin/stat' @('-c','%D|%i|%s',$targetFile)
} else {
  $fileStat=[ordered]@{exitCode=$fileTest.exitCode;stdout='MISSING'}
}
$units=Invoke-Wsl '/usr/bin/systemctl' @('list-units','--type=service','--all','--no-legend','shelleye-*.service')
$processExact=($processStart.exitCode -eq 0 -and [string]$processStart.stdout -eq $expectedStartTicks)
$filePresent=($fileTest.exitCode -eq 0)
$providerObservable=($boot.exitCode -eq 0 -and $pidns.exitCode -eq 0 -and $init.exitCode -eq 0)
$passed=($providerObservable -and $processExact -and $filePresent)
$record=[ordered]@{
  classification='BUILD 002 RUN 004 L3 NATIVE GAP OBSERVER'
  passed=$passed
  whoami=(whoami).Trim()
  session=[Diagnostics.Process]::GetCurrentProcess().SessionId
  capturedUtc=[DateTimeOffset]::UtcNow.ToString('O')
  prepareJson=$PrepareJson
  targetPid=$targetPid
  expectedStartTicks=$expectedStartTicks
  targetFile=$targetFile
  providerObservable=$providerObservable
  processExact=$processExact
  filePresent=$filePresent
  bootId=$boot
  pidNamespace=$pidns
  initStartTicks=$init
  processStartTicks=$processStart
  processExecutable=$processExe
  fileStat=$fileStat
  systemdUnits=$units
}
$record | ConvertTo-Json -Depth 20 | Set-Content -Encoding UTF8 $OutputPath
if($passed){exit 0}else{exit 1}

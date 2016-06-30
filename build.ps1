param(
  [int]$buildNumber = 0,
  [string]$task = "default"
  )

if(Test-Path Env:\APPVEYOR_BUILD_NUMBER){
  $buildNumber = [int]$Env:APPVEYOR_BUILD_NUMBER
  Write-Host "Using APPVEYOR_BUILD_NUMBER"
}

"Build number $buildNumber"

$packageConfigs = Get-ChildItem . -Recurse | where{$_.Name -eq "packages.config"}
foreach($packageConfig in $packageConfigs){
  Write-Host "Restoring" $packageConfig.FullName
  nuget.exe install $packageConfig.FullName -o .\packages
}

Import-Module .\packages\psake.4.4.1\tools\psake.psm1
Import-Module .\BuildFunctions.psm1
Invoke-Psake .\default.ps1 $task -framework "4.5.1" -properties @{ buildNumber=$buildNumber }
Remove-Module BuildFunctions
Remove-Module psake
properties {
  $projectName = "NEventSocket"
  $buildNumber = 0
  $rootDir  = Resolve-Path .\
  $buildOutputDir = "$rootDir\build"
  $mergedDir = "$buildOutputDir\merged"
  $reportsDir = "$buildOutputDir\reports"
  $srcDir = "$rootDir\src"
  $solutionFilePath = "$rootDir\$projectName.sln"
  $assemblyInfoFilePath = "$rootDir\SharedAssemblyInfo.cs"
  $ilmerge_path = "$rootDir\packages\ILMerge.2.14.1208\tools\ilmerge.exe"
  $xunit_path = "$rootDir\packages\xunit.runners.1.9.2\tools\xunit.console.clr4.x86.exe"
  $is_appveyor_build = Test-Path Env:\APPVEYOR_BUILD_NUMBER
}

task default -depends Clean, UpdateVersion, RunTests, CreateNuGetPackages

task Clean {
  Remove-Item $buildOutputDir -Force -Recurse -ErrorAction SilentlyContinue
  exec { msbuild /nologo /verbosity:quiet $solutionFilePath /t:Clean /p:platform="Any CPU"}
}

task UpdateVersion {
  $version = Get-Version $assemblyInfoFilePath
  $oldVersion = New-Object Version $version
  $newVersion = New-Object Version ($oldVersion.Major, $oldVersion.Minor, $oldVersion.Build, $buildNumber)
  Update-Version $newVersion $assemblyInfoFilePath
}

task Compile {
  if ($is_appveyor_build){
    exec { msbuild /nologo /verbosity:quiet $solutionFilePath /p:Configuration=Release /p:platform="Any CPU"  /logger:"C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll"}
  }
  else{
    exec { msbuild /nologo /verbosity:quiet $solutionFilePath /p:Configuration=Release /p:platform="Any CPU"}
  }
}

task RunTests -depends Compile {
  New-Item "$reportsDir\xUnit\$project\" -Type Directory -ErrorAction SilentlyContinue

  #if (!($is_appveyor_build)){
    exec { & $xunit_path "$rootDir\test\NEventSocket.Tests\bin\Release\NEventSocket.Tests.dll" /html "$reportsDir\xUnit\$project\index.html"}
  #}
}

task ILMerge -depends Compile {
  New-Item $mergedDir -Type Directory -ErrorAction SilentlyContinue

  $dllDir = "$srcDir\NEventSocket\bin\Release"
  $inputDlls = "$dllDir\NEventSocket.dll "
  @("System.Reactive.Core", "System.Reactive.Interfaces", "System.Reactive.Linq", "System.Reactive.PlatformServices") |% { $inputDlls = "$inputDlls $dllDir\$_.dll" }
  Invoke-Expression "$ilmerge_path /targetplatform:v4 /internalize /allowDup /target:library /log /out:$mergedDir\NEventSocket.dll $inputDlls"
}

task CreateNuGetPackages -depends ILMerge {
  $versionString = Get-Version $assemblyInfoFilePath
  $version = New-Object Version $versionString

  if (-not $is_appveyor_build){
    $packageVersion = $version.Major.ToString() + "." + $version.Minor.ToString() + "." + $version.Build.ToString()
  }
  else{
    $packageVersion = $version.Major.ToString() + "." + $version.Minor.ToString() + "." + $version.Build.ToString() + "-build" + $buildNumber.ToString().PadLeft(5,'0')
  }
  
  $packageVersion
  gci $srcDir -Recurse -Include *.nuspec | % {
    exec { nuget.exe pack $_ -o $buildOutputDir -version $packageVersion }
  }
}

task PublishNugetPackages -depends CreateNuGetPackages {
  Get-ChildItem "$buildOutputDir\*.nupkg" | `
    ForEach-Object {
      Write-Host "Publishing $($_.FullName)"
      exec { nuget.exe push $_.FullName }
    }
}
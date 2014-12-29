#!/usr/bin/env bash
set -e
mono --runtime=v4.0 ./.nuget/NuGet.exe install ./.nuget/packages.config -OutputDirectory packages
mono --runtime=v4.0 ./.nuget/NuGet.exe restore ./NEventSocket.sln
xbuild ./NEventSocket.sln /property:Configuration=Release /nologo /verbosity:minimal
mono --runtime=v4.0 ./packages/xunit.runners.1.9.2/tools/xunit.console.clr4.x86.exe ./test/NEventSocket.Tests/bin/Release/NEventSocket.Tests.dll

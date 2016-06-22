#!/usr/bin/env bash
set -e
nuget restore ./NEventSocket.sln
nuget install xunit.runners -Version 1.9.2
xbuild ./NEventSocket.sln /property:Configuration=Release /nologo /verbosity:minimal
mono  ./xunit.runners.1.9.2/tools/xunit.console.clr4.x86.exe ./test/NEventSocket.Tests/bin/Release/NEventSocket.Tests.dll
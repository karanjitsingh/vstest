

$root = "E:\Agents\a\_work\3\s\"

$vstestargs = @"
E:\Agents\a\_work\3\s\UnitTestProject1\UnitTestProject1\bin\Release\Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.dll
E:\Agents\a\_work\3\s\UnitTestProject1\UnitTestProject1\bin\Release\Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices.Interface.dll
E:\Agents\a\_work\3\s\UnitTestProject1\UnitTestProject1\bin\Release\Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices.dll
E:\Agents\a\_work\3\s\UnitTestProject1\UnitTestProject1\bin\Release\Microsoft.VisualStudio.TestPlatform.TestFramework.Extensions.dll
E:\Agents\a\_work\3\s\UnitTestProject1\UnitTestProject1\bin\Release\Microsoft.VisualStudio.TestPlatform.TestFramework.dll
E:\Agents\a\_work\3\s\UnitTestProject1\UnitTestProject1\bin\Release\UnitTestProject1.dll
/logger:`"trx`"
/TestAdapterPath:`"E:\Agents\a\_work\3\s`"
"@

$vstestConsolePath = "C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"



$script = $PSScriptRoot+"\DTAExecutionHost\dtaexec.ps1"
& $script $root $vstestConsolePath $vstestargs
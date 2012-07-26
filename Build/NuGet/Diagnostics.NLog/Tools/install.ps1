param($installPath, $toolsPath, $package, $project)

."functions.ps1"

$psproj = GetPostSharpProject -project $project -create $true 
$usingPath = [System.IO.Path]::Combine($toolsPath, "PostSharp.Toolkit.Diagnostics.Weaver.NLog.dll")
AddUsing -psproj $psproj -path $usingPath
SetProperty -psproj $psproj -propertyName "LoggingBackEnd" -propertyValue "nlog"

Save -psproj $psproj

﻿param($installPath, $toolsPath, $package, $project)

."functions.ps1"

$psproj = GetPostSharpProject -project $project -create $true 
$usingPath = [System.IO.Path]::Combine($toolsPath, "PostSharp.Toolkit.Threading.Weaver.dll")
AddUsing -psproj $psproj -path $usingPath

Save -psproj $psproj

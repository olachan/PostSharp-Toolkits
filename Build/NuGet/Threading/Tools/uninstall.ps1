﻿param($installPath, $toolsPath, $package, $project)

."functions.ps1"

$psproj = GetPostSharpProject -project $project -create $false

if ( $psproj )
{
	$using = [System.IO.Path]::Combine($toolsPath, "PostSharp.Toolkit.Threading.Weaver.dll") 
	RemoveUsing -psproj $psproj -path $using
	Save -psproj $psproj
}


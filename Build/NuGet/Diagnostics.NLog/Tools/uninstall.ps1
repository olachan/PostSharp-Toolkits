param($installPath, $toolsPath, $package, $project)

."functions.ps1"

$psproj = GetPostSharpProject -project $project -create $false

if ( $psproj )
{
	$using = [System.IO.Path]::Combine($toolsPath, "PostSharp.Toolkit.Diagnostics.Weaver.NLog.dll") 
	RemoveUsing -psproj $psproj -path $using
	SetProperty -psproj $psproj -propertyName "LoggingBackEnd" -propertyValue "trace" -compareValue "nlog"
	Save -psproj $psproj
}


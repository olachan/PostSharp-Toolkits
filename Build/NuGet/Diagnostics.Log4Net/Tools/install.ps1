param($installPath, $toolsPath, $package, $project)

."functions.ps1"

$psproj = GetPostSharpProject -project $project -create $true 
$usingPath = [System.IO.Path]::Combine($toolsPath, "PostSharp.Toolkit.Diagnostics.Weaver.Log4Net.dll")
AddUsing -psproj $psproj -path $usingPath
SetProperty -psproj $psproj -propertyName "LoggingBackEnd" -propertyValue "log4net"

Save -psproj $psproj


$item = $project.ProjectItems.Item("B1927FEF-184F-45BA-A572-9A7C9AFBDCE3.txt")
if ($item)
{
  $item.Delete()
}

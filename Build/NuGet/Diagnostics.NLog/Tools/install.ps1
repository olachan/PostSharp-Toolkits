param($installPath, $toolsPath, $package, $project)

."functions.ps1"

$psproj = GetPostSharpProject -project $project -create $true 
$usingPath = [System.IO.Path]::Combine($toolsPath, "PostSharp.Toolkit.Diagnostics.Weaver.NLog.dll")
AddUsing -psproj $psproj -path $usingPath
SetProperty -psproj $psproj -propertyName "LoggingBackEnd" -propertyValue "nlog"

Save -psproj $psproj


$item = $project.ProjectItems.Item("690CAA32-A6FE-41DB-A645-FED34CD38682.txt")
if ($item)
{
  $item.Delete()
}

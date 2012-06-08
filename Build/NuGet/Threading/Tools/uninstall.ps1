param($installPath, $toolsPath, $package, $project)

$targetsFile = [System.IO.Path]::Combine($toolsPath, 'PostSharp.targets')

# Need to load MSBuild assembly if it's not loaded yet.
Add-Type -AssemblyName 'Microsoft.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'

# Grab the loaded MSBuild project for the project
$msbuild = [Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.GetLoadedProjects($project.FullName) | Select-Object -First 1

# Make the path to the tools directory relative.
$projectUri = new-object Uri('file://' + $project.FullName)
$targetUri = new-object Uri('file://' + $toolsPath + "/PostSharp.Toolkit.Threading.Weaver.dll")
$relativePath = $projectUri.MakeRelativeUri($targetUri).ToString().Replace([System.IO.Path]::AltDirectorySeparatorChar, [System.IO.Path]::DirectorySeparatorChar)

# Remove previous item PostSharpAddIn
$msbuild.Xml.Items | Where-Object {$_.Name.ToLowerInvariant() -eq "postsharpaddin" -and $_.UnevaluatedInclude.ToLowerInvariant() -eq $relativePath.ToLowerInvariant()  } | Foreach { 
	$_.Parent.RemoveChild( $_ ) 
	"Removed item PostSharpAddIn"
}


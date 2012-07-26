param($installPath, $toolsPath, $package, $project)

."functions.ps1"

$psproj = GetPostSharpProject -project $project -create $true 
$usingPath = [System.IO.Path]::Combine($toolsPath, "PostSharp.Toolkit.Diagnostics.Weaver.dll")
AddUsing -psproj $psproj -path $usingPath


# Add LogAttribute
$xml = [xml] $psproj.Content
$data = $xml.Project.Data | where  { $_.Name -eq "XmlMulticast" } | Select-Object -First 1

if ( $data )
{
	$comments = $data.ChildNodes | where { $_.InnerText -like "*NO_MORE_LOGGING*"  }
	
	if ( !$data.LogAttribute -and !$comments )
	{

		$fragment = $xml.CreateDocumentFragment()
		$fragment.InnerXml = @"
	
    <!-- Add tracing to everything -->
    <LogAttribute xmlns="clr-namespace:PostSharp.Toolkit.Diagnostics;assembly:PostSharp.Toolkit.Diagnostics" />
    <!-- But exclude property getters and setters -->
    <LogAttribute xmlns="clr-namespace:PostSharp.Toolkit.Diagnostics;assembly:PostSharp.Toolkit.Diagnostics" AttributeExclude="true" AttributeTargetMembers="regex:get_.*|set_.*" />

"@
	
		$data.AppendChild($fragment)
	}

	if ( $comments )
	{
		foreach ( $comment in $comments )
		{
			$data.RemoveChild($comment)
		}
	}
	
}



Save -psproj $psproj

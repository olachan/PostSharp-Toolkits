param($installPath, $toolsPath, $package, $project)

."functions.ps1"

$psproj = GetPostSharpProject -project $project -create $false

if ( $psproj )
{
	$using = [System.IO.Path]::Combine($toolsPath, "PostSharp.Toolkit.Diagnostics.Weaver.dll") 
	RemoveUsing -psproj $psproj -path $using
	
	$xml = [xml] $psproj.Content
	$data = $xml.Project.Data | where  { $_.Name -eq "XmlMulticast" } | Select-Object -First 1

	if ( $data )
	{
		$comments = $data.ChildrenNodes | where { $_.InnerText -like "*NO_MORE_LOGGING*"  }
		if ( !$data.LogAttribute -and !$comments )
		{
			$comment = $xml.CreateComment( @"
			NO_MORE_LOGGING: This comment prevents the PostSharp.Toolkit.Diagnostics package to 
			add logging to all methods on next update. Remove the comment if you removed the
			PostSharp.Toolkit.Diagnostics package definitively 
"@)
			$data.AppendChild($comment)
		}

	}

	Save -psproj $psproj

}


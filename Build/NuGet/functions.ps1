function GetPostSharpProject($project, [bool] $create)
{
	$xml = [xml] @"
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.postsharp.org/1.0/configuration" ReferenceDirectory="{`$ReferenceDirectory}">
  <Using File="default" />
  <Tasks>
    <XmlMulticast />
  </Tasks>
  <Data Name="XmlMulticast">
  </Data>
</Project>
"@

	$projectName = $project.Name
	
	# Set the psproj name to be the Project's name, i.e. 'ConsoleApplication1.psproj'
	$psprojectName = $project.Name + ".psproj"

	# Check if the file previously existed in the project
	$psproj = $project.ProjectItems | where { $_.Name -eq $psprojectName }

	# If this item already exists, load it
	if ($psproj)
	{
	  $psprojectFile = $psproj.Properties.Item("FullPath").Value
	  
	  Write-Host "Opening existing file $psprojectFile"
	  
	  $xml = [xml](Get-Content $psprojectFile)
	} 
	elseif ( $create )
	{
		# Create a file on disk, write XML, and load it into the project.
		$psprojectFile = [System.IO.Path]::ChangeExtension($project.FileName, ".psproj")
		
		Write-Host "Creating file $psprojectFile"
		
		$xml.Save($psprojectFile)
		$project.ProjectItems.AddFromFile($psprojectFile) | Out-Null
		
	}
	else
	{
		Write-Host "$psprojectName not found."
		return $null
	}
	
	return [hashtable] @{ Content = [xml] $xml; FileName = [string] $psprojectFile } 
}

function AddUsing($psproj, [string] $path)
{
	$xml = $psproj.Content
	$originPath = $psproj.FileName
	
	# Make the path to the targets file relative.
	$projectUri = new-object Uri('file://' + $originPath)
	$targetUri = new-object Uri('file://' + $path)
	$relativePath = [System.Uri]::UnescapeDataString($projectUri.MakeRelativeUri($targetUri).ToString()).Replace([System.IO.Path]::AltDirectorySeparatorChar, [System.IO.Path]::DirectorySeparatorChar)
    $shortFileName = '*' + [System.IO.Path]::GetFileNameWithoutExtension($path) + '*'
	$toolkitWeaver = $xml.Project.Using | where { $_.File -like $shortFileName}
	
	if ($toolkitWeaver)
	{
		Write-Host "Updating the Using element to $relativePath"
	
		$toolkitWeaver.SetAttribute("File", $relativePath)
	} 
	else 
	{
		Write-Host "Adding a Using element to $relativePath"
	
		$defaultUsing = $xml.Project.Using | where { $_.File -eq 'default' }
		$toolkitWeaver = $xml.CreateElement("Using", "http://schemas.postsharp.org/1.0/configuration")
		$toolkitWeaver.SetAttribute("File", $relativePath)
		$xml.Project.InsertAfter($toolkitWeaver, $defaultUsing)
	}

}

function RemoveUsing($psproj, [string] $path)
{
	$xml = $psproj.Content
	
	Write-Host "Removing the Using element to $path"
	
	$shortFileName = '*' + [System.IO.Path]::GetFileNameWithoutExtension($path) + '*'
		$xml.Project.Using | where { $_.File -like $shortFileName } | foreach {
	  $_.ParentNode.RemoveChild($_)
	}
}

function SetProperty($psproj, [string] $propertyName, [string] $propertyValue, [string] $compareValue )
{
	$xml = $psproj.Content
	
	$firstUsing = $xml.Project.Using | Select-Object -First 1

	$property = $xml.Project.Property | where { $_.Name -eq $propertyName }
	if (!$property -and !$compareValue )
	{
		Write-Host "Creating property $propertyName='$propertyValue'."
	    
		$property = $xml.CreateElement("Property", "http://schemas.postsharp.org/1.0/configuration")
		$property.SetAttribute("Name", $propertyName)
		$property.SetAttribute("Value", $propertyValue)
	 	$xml.Project.InsertBefore($property, $firstUsing)
	}
	elseif ( !$compareValue -or $compareValue -eq $property.GetAttribute("Value") )
	{
		Write-Host "Updating property $propertyName='$propertyValue'."
		
		$property.SetAttribute("Value", $propertyValue)
	}

	
}

function Save($psproj)
{
	$filename = $psproj.FileName
	
	Write-Host "Saving file $filename"

	$xml = $psproj.Content
    $xml.Save($psproj.FileName)
}

function CommentOut([System.Xml.XmlNode] $xml)
{
	Write-Host "Commenting out $xml"
	$fragment = $xml.OwnerDocument.CreateDocumentFragment()
	$fragment.InnerXml = "<!--" + $xml.OuterXml + "-->"
	$xml.ParentNode.InsertAfter( $fragment, $xml )
	$xml.ParentNode.RemoveChild( $xml )
}

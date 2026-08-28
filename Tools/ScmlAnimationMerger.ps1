param(
    [Parameter(Position = 0)]
    [string[]] $InputScml,

    [Parameter(Position = 1)]
    [string] $OutputScml,

    [switch] $Force
)

$ErrorActionPreference = 'Stop'

function Get-ScmlPathsFromDialog {
    Add-Type -AssemblyName System.Windows.Forms

    $dialog = New-Object System.Windows.Forms.OpenFileDialog
    $dialog.Title = 'Select SCML files to merge'
    $dialog.Filter = 'Spriter SCML (*.scml)|*.scml'
    $dialog.Multiselect = $true
    $dialog.CheckFileExists = $true

    if ($dialog.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) {
        return @()
    }

    return @($dialog.FileNames)
}

function Get-OutputPathFromDialog([string] $DefaultDirectory, [string] $DefaultName) {
    Add-Type -AssemblyName System.Windows.Forms

    $dialog = New-Object System.Windows.Forms.SaveFileDialog
    $dialog.Title = 'Save merged SCML'
    $dialog.Filter = 'Spriter SCML (*.scml)|*.scml'
    $dialog.DefaultExt = 'scml'
    $dialog.AddExtension = $true
    $dialog.InitialDirectory = $DefaultDirectory
    $dialog.FileName = $DefaultName

    if ($dialog.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) {
        return $null
    }

    return $dialog.FileName
}

function Get-AbsolutePath([string] $Path) {
    return [System.IO.Path]::GetFullPath($Path)
}

function Get-AttributeValue([System.Xml.XmlElement] $Element, [string] $Name) {
    return $Element.GetAttribute($Name)
}

function Get-FolderSignature([System.Xml.XmlElement] $Root) {
    $builder = New-Object System.Text.StringBuilder

    foreach ($folder in $Root.SelectNodes('./folder')) {
        [void] $builder.Append('folder|')
        [void] $builder.Append((Get-AttributeValue $folder 'id'))
        [void] $builder.Append('|')
        [void] $builder.Append((Get-AttributeValue $folder 'name'))
        [void] $builder.Append("`n")

        foreach ($file in $folder.SelectNodes('./file')) {
            [void] $builder.Append('file|')
            [void] $builder.Append((Get-AttributeValue $file 'id'))
            [void] $builder.Append('|')
            [void] $builder.Append((Get-AttributeValue $file 'name'))
            [void] $builder.Append('|')
            [void] $builder.Append((Get-AttributeValue $file 'width'))
            [void] $builder.Append('|')
            [void] $builder.Append((Get-AttributeValue $file 'height'))
            [void] $builder.Append('|')
            [void] $builder.Append((Get-AttributeValue $file 'pivot_x'))
            [void] $builder.Append('|')
            [void] $builder.Append((Get-AttributeValue $file 'pivot_y'))
            [void] $builder.Append("`n")
        }
    }

    return $builder.ToString()
}

function Get-SingleEntity([System.Xml.XmlElement] $Root, [string] $Path) {
    $entities = @($Root.SelectNodes('./entity'))
    if ($entities.Count -ne 1) {
        throw "SCML must contain exactly one entity: $Path"
    }

    return [System.Xml.XmlElement] $entities[0]
}

function Read-ScmlDocument([string] $Path) {
    $document = New-Object System.Xml.XmlDocument
    $document.PreserveWhitespace = $false
    $document.Load($Path)

    if ($null -eq $document.DocumentElement -or
        $document.DocumentElement.Name -ne 'spriter_data') {
        throw "Invalid Spriter SCML root: $Path"
    }

    return $document
}

function Get-PackageRoot([string] $SourcePath, [string] $RelativePath) {
    $absoluteSourcePath = Get-AbsolutePath $SourcePath
    $normalizedRelativePath = $RelativePath -replace '/', [System.IO.Path]::DirectorySeparatorChar
    $suffix = [System.IO.Path]::DirectorySeparatorChar + $normalizedRelativePath
    $index = $absoluteSourcePath.LastIndexOf($suffix, [System.StringComparison]::OrdinalIgnoreCase)
    if ($index -ge 0) {
        return $absoluteSourcePath.Substring(0, $index)
    }

    return Split-Path -Parent $absoluteSourcePath
}

function Resolve-ReferencedImage(
    [string] $ImageName,
    [string] $SourceScmlPath,
    [string] $PreferredPackageRoot = $null) {
    $sourceDirectory = Split-Path -Parent $SourceScmlPath
    $rawPath = $ImageName -replace '/', [System.IO.Path]::DirectorySeparatorChar
    $relativePath = $null
    $sourcePath = $null

    if (-not [System.IO.Path]::IsPathRooted($rawPath)) {
        $relativePath = $rawPath
        $sourcePath = Join-Path $sourceDirectory $relativePath

        if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
            $sourcePath = $null
        }
    }
    else {
        $absolutePath = Get-AbsolutePath $rawPath
        if (Test-Path -LiteralPath $absolutePath -PathType Leaf) {
            $sourcePath = $absolutePath
            $sourcePrefix = (Get-AbsolutePath $sourceDirectory).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
            if ($absolutePath.StartsWith($sourcePrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
                $relativePath = $absolutePath.Substring($sourcePrefix.Length)
            }
            else {
                $relativePath = [System.IO.Path]::GetFileName($absolutePath)
            }
        }
        else {
            # Krane can write an absolute path without the animation package directory.
            # Try the same resource suffix below each parent of the SCML directory.
            $parent = Get-Item -LiteralPath $sourceDirectory
            while ($null -ne $parent) {
                $parentPrefix = $parent.FullName.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
                if ($absolutePath.StartsWith($parentPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
                    $candidateRelativePath = $absolutePath.Substring($parentPrefix.Length)
                    $candidateSourcePath = Join-Path $sourceDirectory $candidateRelativePath
                    if (Test-Path -LiteralPath $candidateSourcePath -PathType Leaf) {
                        $sourcePath = $candidateSourcePath
                        $relativePath = $candidateRelativePath
                        break
                    }
                }

                $parent = $parent.Parent
            }
        }
    }

    if ($null -eq $sourcePath) {
        if ([System.IO.Path]::IsPathRooted($rawPath)) {
            $resourceDirectory = Split-Path -Parent $rawPath
            $relativePath = Join-Path (Split-Path -Leaf $resourceDirectory) (Split-Path -Leaf $rawPath)
        }

        if ($null -ne $PreferredPackageRoot) {
            $preferredPath = Join-Path $PreferredPackageRoot $relativePath
            if (Test-Path -LiteralPath $preferredPath -PathType Leaf) {
                $sourcePath = $preferredPath
            }
        }
    }

    if ($null -eq $sourcePath) {
        $searchSuffix = '/' + $relativePath.Replace('\', '/')
        $fileName = [System.IO.Path]::GetFileName($relativePath)
        $candidates = @(
            Get-ChildItem -LiteralPath $sourceDirectory -Recurse -File -Filter $fileName |
                Where-Object {
                    $_.FullName.Replace('\', '/').EndsWith($searchSuffix, [System.StringComparison]::OrdinalIgnoreCase)
                }
        )

        if ($candidates.Count -gt 0) {
            $sourcePath = $candidates[0].FullName
        }
    }

    if ($null -eq $sourcePath -or -not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
        return $null
    }

    $relativePath = $relativePath.Replace('\', '/')
    if ([System.IO.Path]::IsPathRooted($relativePath) -or $relativePath.StartsWith('../')) {
        throw "SCML image is outside the source resource directory: $ImageName"
    }

    return [PSCustomObject] @{
        SourcePath = Get-AbsolutePath $sourcePath
        RelativePath = $relativePath
        PackageRoot = Get-PackageRoot $sourcePath $relativePath
    }
}

function Test-FilesEqual([string] $FirstPath, [string] $SecondPath) {
    $first = Get-Item -LiteralPath $FirstPath
    $second = Get-Item -LiteralPath $SecondPath
    if ($first.Length -ne $second.Length) {
        return $false
    }

    return (Get-FileHash -LiteralPath $FirstPath -Algorithm SHA256).Hash -eq
        (Get-FileHash -LiteralPath $SecondPath -Algorithm SHA256).Hash
}

function Prepare-ReferencedFiles(
    [System.Xml.XmlDocument] $Document,
    [string] $SourceScmlPath,
    [string] $OutputPath) {
    $outputDirectory = Split-Path -Parent $OutputPath
    $missingFiles = New-Object System.Collections.Generic.List[string]
    $packageRoot = $null

    foreach ($file in $Document.SelectNodes('/spriter_data/folder/file')) {
        $image = Resolve-ReferencedImage (
            Get-AttributeValue $file 'name') $SourceScmlPath $packageRoot
        if ($null -eq $image) {
            [void] $missingFiles.Add((Get-AttributeValue $file 'name'))
            continue
        }

        if ($null -eq $packageRoot) {
            $packageRoot = $image.PackageRoot
        }

        $destinationPath = Join-Path $outputDirectory $image.RelativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
        $destinationDirectory = Split-Path -Parent $destinationPath
        if (-not (Test-Path -LiteralPath $destinationDirectory -PathType Container)) {
            New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
        }

        if (Test-Path -LiteralPath $destinationPath -PathType Leaf) {
            if (-not (Test-FilesEqual $image.SourcePath $destinationPath)) {
                throw "Output image already exists with different content: $destinationPath"
            }
        }
        else {
            Copy-Item -LiteralPath $image.SourcePath -Destination $destinationPath
        }

        $file.SetAttribute('name', $image.RelativePath)
    }

    if ($missingFiles.Count -gt 0) {
        $preview = $missingFiles | Select-Object -First 8
        throw "Cannot find image files next to the selected base SCML:`n$($preview -join "`n")"
    }
}

function Merge-ScmlDocuments([string[]] $Paths, [string] $Destination) {
    $documents = @()
    foreach ($path in $Paths) {
        $documents += Read-ScmlDocument (Get-AbsolutePath $path)
    }

    $baseDocument = $documents[0]
    $baseRoot = $baseDocument.DocumentElement
    $baseEntity = Get-SingleEntity $baseRoot $Paths[0]
    $baseSignature = Get-FolderSignature $baseRoot
    $entityName = Get-AttributeValue $baseEntity 'name'
    $animationNames = New-Object System.Collections.Generic.HashSet[string] ([System.StringComparer]::Ordinal)

    foreach ($animation in $baseEntity.SelectNodes('./animation')) {
        [void] $animationNames.Add((Get-AttributeValue $animation 'name'))
    }

    for ($index = 1; $index -lt $documents.Count; $index++) {
        $sourceRoot = $documents[$index].DocumentElement
        $sourceEntity = Get-SingleEntity $sourceRoot $Paths[$index]

        if ((Get-AttributeValue $sourceEntity 'name') -ne $entityName) {
            throw "Entity name differs between '$($Paths[0])' and '$($Paths[$index])'."
        }

        if ((Get-FolderSignature $sourceRoot) -ne $baseSignature) {
            throw "Folder/file resource indexes differ in '$($Paths[$index])'. Merge stopped to prevent incorrect sprites."
        }

        foreach ($animation in $sourceEntity.SelectNodes('./animation')) {
            $animationName = Get-AttributeValue $animation 'name'
            if (-not $animationNames.Add($animationName)) {
                throw "Duplicate animation name: $animationName"
            }

            $importedAnimation = $baseDocument.ImportNode($animation, $true)
            [void] $baseEntity.AppendChild($importedAnimation)
        }
    }

    Prepare-ReferencedFiles $baseDocument $Paths[0] $Destination

    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Encoding = New-Object System.Text.UTF8Encoding($false)
    $settings.Indent = $true
    $settings.NewLineChars = "`n"
    $settings.NewLineHandling = [System.Xml.NewLineHandling]::Entitize

    $writer = [System.Xml.XmlWriter]::Create($Destination, $settings)
    try {
        $baseDocument.Save($writer)
    }
    finally {
        $writer.Dispose()
    }

    return @($animationNames)
}

try {
    if ($null -eq $InputScml -or $InputScml.Count -eq 0) {
        $InputScml = Get-ScmlPathsFromDialog
    }

    if ($InputScml.Count -lt 2) {
        throw 'Select at least two SCML files.'
    }

    $InputScml = @($InputScml | ForEach-Object { Get-AbsolutePath $_ })
    $duplicatePaths = @($InputScml | Group-Object { $_.ToLowerInvariant() } | Where-Object Count -gt 1)
    if ($duplicatePaths.Count -gt 0) {
        throw 'The same SCML file was selected more than once.'
    }

    if ($null -eq $OutputScml -or [string]::IsNullOrWhiteSpace($OutputScml)) {
        $defaultDirectory = Split-Path -Parent $InputScml[0]
        $defaultName = [System.IO.Path]::GetFileNameWithoutExtension($InputScml[0]) + '_merged.scml'
        $OutputScml = Get-OutputPathFromDialog $defaultDirectory $defaultName
    }

    if ([string]::IsNullOrWhiteSpace($OutputScml)) {
        throw 'Output selection was cancelled.'
    }

    $OutputScml = Get-AbsolutePath $OutputScml
    if ($InputScml -contains $OutputScml) {
        throw 'The output file cannot replace an input SCML file.'
    }

    if ((Test-Path -LiteralPath $OutputScml -PathType Leaf) -and -not $Force) {
        throw "Output already exists. Use -Force to overwrite: $OutputScml"
    }

    $outputDirectory = Split-Path -Parent $OutputScml
    if (-not (Test-Path -LiteralPath $outputDirectory -PathType Container)) {
        throw "Output directory does not exist: $outputDirectory"
    }

    $animationNames = Merge-ScmlDocuments $InputScml $OutputScml
    Write-Host "Merged SCML written to: $OutputScml"
    Write-Host "Animation count: $($animationNames.Count)"
    Write-Host "Animations: $($animationNames -join ', ')"
}
catch {
    Write-Error "$($_.Exception.Message)`n$($_.ScriptStackTrace)"
    exit 1
}

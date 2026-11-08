param(
    [string]$Output = (Join-Path $PSScriptRoot 'export\BBTAG_IM'),
    [switch]$SkipTests,
    [switch]$WithPort
)

$ErrorActionPreference = 'Stop'

$ReleaseFiles = @(
    'BBTAG_Mod_Loader.exe',
    'README.txt',
    'mods\BBTAG-Improvement-Mod\ImprovementMod.dll'
)

$HiddenWords = @(
    'French-Bread', 'FrenchBread', 'MELTY BLOOD', 'UNDER NIGHT IN-BIRTH', 'TYPE LUMINA', 'Hantei', 'Kohaku',
    'CharacterPort', 'PortRunner', 'PortPanel', 'PortJob', 'Core.Porting', 'MoveCatalog', 'Port_'
)

function Invoke-Dotnet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($args -join ' ') failed with exit code $LASTEXITCODE"
    }
}

function Get-RelativeFiles($Folder) {
    Get-ChildItem $Folder -File -Recurse | ForEach-Object { $_.FullName.Substring($Folder.Length + 1) } | Sort-Object
}

function Assert-ReleaseFiles($Folder) {
    $unexpected = Compare-Object ($ReleaseFiles | Sort-Object) (Get-RelativeFiles $Folder)

    if ($unexpected) {
        throw "The release has the wrong files: $($unexpected | Out-String)"
    }
}

function Assert-NoHiddenWords($Files) {
    $latin1 = [Text.Encoding]::GetEncoding(28591)
    $utf16 = [Text.Encoding]::Unicode

    foreach ($file in $Files) {
        $bytes = [IO.File]::ReadAllBytes($file.FullName)
        $texts = @($latin1.GetString($bytes), $utf16.GetString($bytes), $utf16.GetString($bytes, 1, $bytes.Length - 1))

        foreach ($word in $HiddenWords) {
            if ($texts | Where-Object { $_.Contains($word) }) {
                throw "$($file.Name) still has '$word' in it."
            }
        }
    }
}

if (-not $SkipTests) {
    Invoke-Dotnet test (Join-Path $PSScriptRoot 'tests\BBTAG_IM.Core.Tests') -c Release
}

$buildFlags = @()

if ($WithPort) {
    if (Test-Path $Output) {
        Get-ChildItem $Output -File | Remove-Item -Force
    }
}
else {
    $version = ([xml](Get-Content (Join-Path $PSScriptRoot 'Directory.Build.props'))).Project.PropertyGroup.Version | Where-Object { $_ }
    $Output = Join-Path $PSScriptRoot "export\BBTAG_IM-$version-tester"
    $buildFlags = @('-p:TesterBuild=true')

    if (Test-Path $Output) {
        Remove-Item $Output -Recurse -Force
    }
}

$payloadFolder = Join-Path $PSScriptRoot 'src\BBTAG_IM.Payload\bin\Release\net10.0\win-x86\publish'
Invoke-Dotnet publish (Join-Path $PSScriptRoot 'src\BBTAG_IM.Payload') -c Release -o $payloadFolder @buildFlags
Invoke-Dotnet publish (Join-Path $PSScriptRoot 'src\BBTAG_IM.Launcher') -c Release -o $Output "-p:BundledPayload=$(Join-Path $payloadFolder 'BBTAG_IM.dll')" @buildFlags

$improvementMod = Join-Path $Output 'mods\BBTAG-Improvement-Mod'
Invoke-Dotnet publish (Join-Path $PSScriptRoot 'src\BBTAG_IM.ImprovementMod') -c Release -o $improvementMod @buildFlags

$scriptToolFolder = Join-Path $PSScriptRoot 'src\BBTAG_IM.BBScript\bin\Release\net10.0\win-x86\publish'
Invoke-Dotnet publish (Join-Path $PSScriptRoot 'src\BBTAG_IM.BBScript') -c Release -o $scriptToolFolder @buildFlags

if ($WithPort) {
    Copy-Item (Join-Path $scriptToolFolder 'bbscript.exe') $improvementMod -Force
    Copy-Item (Join-Path $PSScriptRoot 'src\BBTAG_IM.BBScript\Python\bbscript.py') $improvementMod -Force
    New-Item -ItemType Directory -Force (Join-Path $improvementMod 'palettes') | Out-Null
    $editorProject = Join-Path $improvementMod 'bbscript\Scripts'
    New-Item -ItemType Directory -Force $editorProject | Out-Null
    Copy-Item (Join-Path $PSScriptRoot 'resource\Scripts\*') $editorProject -Force
}

Get-ChildItem $Output -Filter *.pdb -Recurse | Remove-Item -Force
Copy-Item (Join-Path $PSScriptRoot 'resource\README.txt') $Output -Force

if (-not $WithPort) {
    Assert-ReleaseFiles $Output
    Assert-NoHiddenWords (Get-ChildItem $Output -File -Recurse)
    Assert-NoHiddenWords (Get-Item (Join-Path $scriptToolFolder 'bbscript.exe'))
    Compress-Archive -Path (Join-Path $Output '*') -DestinationPath "$Output.zip" -Force
}

Get-ChildItem $Output -File -Recurse | Format-Table FullName, Length -AutoSize

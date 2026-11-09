param(
    [string]$Output = (Join-Path $PSScriptRoot 'export\BBTAG_IM'),
    [switch]$SkipTests,
    [switch]$WithTools
)

$ErrorActionPreference = 'Stop'

$UpdaterName = 'BBTAG_IM_Updater.exe'
$PlayerDllName = 'dinput8.dll'
$PlayerFiles = @($PlayerDllName, $UpdaterName)
$PlayerExports = @('DirectInput8Create', 'BBTAG_IM_StandaloneStart')

$DevFiles = @(
    'BBTAG_Mod_Loader.exe',
    'mods\BBTAG-Improvement-Mod\ImprovementMod.dll',
    $UpdaterName
)

$PlayerHiddenWords = @(
    'Boss fight', 'Take-Mikazuchi', 'System XX', 'BBTAG-Boss-Fights',
    'Script editing', 'Visual Studio', 'bbscript', 'BBTAG_IM_ModStart'
)

$ScriptTool = Join-Path $PSScriptRoot 'src\BBTAG_IM.BBScript\bin\Release\net10.0\win-x86\publish\bbscript.exe'
$UpdaterFolder = Join-Path $PSScriptRoot 'src\BBTAG_IM.Updater\bin\Release\net10.0\win-x86\publish'

function Invoke-Dotnet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($args -join ' ') failed with exit code $LASTEXITCODE"
    }
}

function Get-RelativeFiles($Folder) {
    Get-ChildItem $Folder -File -Recurse | ForEach-Object { $_.FullName.Substring($Folder.Length + 1) } | Sort-Object
}

function Assert-ReleaseFiles($Folder, $Expected) {
    $unexpected = Compare-Object ($Expected | Sort-Object) @(Get-RelativeFiles $Folder)

    if ($unexpected) {
        throw "The release has the wrong files: $($unexpected | Out-String)"
    }
}

function Get-FileTexts($File) {
    $latin1 = [Text.Encoding]::GetEncoding(28591)
    $utf16 = [Text.Encoding]::Unicode
    $bytes = [IO.File]::ReadAllBytes($File.FullName)

    return @($latin1.GetString($bytes), $utf16.GetString($bytes), $utf16.GetString($bytes, 1, $bytes.Length - 1))
}

function Assert-NoHiddenWords($Files, $Words) {
    foreach ($file in $Files) {
        $texts = Get-FileTexts $file

        foreach ($word in $Words) {
            if ($texts | Where-Object { $_.Contains($word) }) {
                throw "$($file.Name) still has '$word' in it."
            }
        }
    }
}

function Assert-HasWords($File, $Words) {
    $texts = Get-FileTexts $File

    foreach ($word in $Words) {
        if (-not ($texts | Where-Object { $_.Contains($word) })) {
            throw "$($File.Name) does not have '$word' in it."
        }
    }
}

function Remove-Folder($Folder) {
    if (Test-Path $Folder) {
        Remove-Item $Folder -Recurse -Force
    }
}

function Compress-Release($Folder) {
    Compress-Archive -Path (Join-Path $Folder '*') -DestinationPath "$Folder.zip" -Force
}

function Publish-Updater {
    Remove-Folder $UpdaterFolder
    Invoke-Dotnet publish (Join-Path $PSScriptRoot 'src\BBTAG_IM.Updater') -c Release -o $UpdaterFolder
}

function Copy-Updater($Folder) {
    Copy-Item (Join-Path $UpdaterFolder $UpdaterName) $Folder -Force
}

function Publish-LoaderAndMod($Folder) {
    $payloadFolder = Join-Path $PSScriptRoot 'src\BBTAG_IM.Payload\bin\Release\net10.0\win-x86\publish'
    Invoke-Dotnet publish (Join-Path $PSScriptRoot 'src\BBTAG_IM.Payload') -c Release -o $payloadFolder
    Invoke-Dotnet publish (Join-Path $PSScriptRoot 'src\BBTAG_IM.Launcher') -c Release -o $Folder "-p:BundledPayload=$(Join-Path $payloadFolder 'BBTAG_IM.dll')"
    Invoke-Dotnet publish (Join-Path $PSScriptRoot 'src\BBTAG_IM.ImprovementMod') -c Release -o (Join-Path $Folder 'mods\BBTAG-Improvement-Mod')
}

function Remove-DebugSymbols($Folder) {
    Get-ChildItem $Folder -Filter *.pdb -Recurse | Remove-Item -Force
}

function Publish-ToolsEdition($Folder) {
    if (Test-Path $Folder) {
        Get-ChildItem $Folder -File | Remove-Item -Force
    }

    Publish-LoaderAndMod $Folder
    Invoke-Dotnet publish (Join-Path $PSScriptRoot 'src\BBTAG_IM.BBScript') -c Release -o (Split-Path $ScriptTool)
    $improvementMod = Join-Path $Folder 'mods\BBTAG-Improvement-Mod'
    Copy-Item $ScriptTool $improvementMod -Force
    Copy-Item (Join-Path $PSScriptRoot 'src\BBTAG_IM.BBScript\Python\bbscript.py') $improvementMod -Force
    New-Item -ItemType Directory -Force (Join-Path $improvementMod 'palettes') | Out-Null
    $editorProject = Join-Path $improvementMod 'bbscript\Scripts'
    New-Item -ItemType Directory -Force $editorProject | Out-Null
    Copy-Item (Join-Path $PSScriptRoot 'resource\Scripts\*') $editorProject -Force
    Remove-DebugSymbols $Folder
    Copy-Item (Join-Path $PSScriptRoot 'resource\README.txt') $Folder -Force
}

function Publish-DevEdition($Folder) {
    Remove-Folder $Folder
    Publish-LoaderAndMod $Folder
    Copy-Updater $Folder
    Remove-DebugSymbols $Folder
    Assert-ReleaseFiles $Folder $DevFiles
    Compress-Release $Folder
}

function Publish-PlayerEdition($Folder) {
    $publishFolder = Join-Path $PSScriptRoot 'src\BBTAG_IM.ImprovementMod\bin\Release\net10.0\win-x86\public'
    Remove-Folder $Folder
    Remove-Folder $publishFolder
    Invoke-Dotnet publish (Join-Path $PSScriptRoot 'src\BBTAG_IM.ImprovementMod') -c Release -o $publishFolder '-p:PublicBuild=true'
    New-Item -ItemType Directory -Force $Folder | Out-Null
    Copy-Item (Join-Path $publishFolder 'ImprovementMod.dll') (Join-Path $Folder $PlayerDllName)
    Copy-Updater $Folder
    Assert-ReleaseFiles $Folder $PlayerFiles
    $dll = Get-Item (Join-Path $Folder $PlayerDllName)
    Assert-NoHiddenWords $dll $PlayerHiddenWords
    Assert-HasWords $dll $PlayerExports
    Compress-Release $Folder
}

if (-not $SkipTests) {
    Invoke-Dotnet test (Join-Path $PSScriptRoot 'tests\BBTAG_IM.Core.Tests') -c Release
}

if ($WithTools) {
    Publish-ToolsEdition $Output
    Get-ChildItem $Output -File -Recurse | Format-Table FullName, Length -AutoSize
    return
}

$version = ([xml](Get-Content (Join-Path $PSScriptRoot 'Directory.Build.props'))).Project.PropertyGroup.Version | Where-Object { $_ }
$playerFolder = Join-Path $PSScriptRoot "export\BBTAG_IM-$version"
$devFolder = Join-Path $PSScriptRoot "export\BBTAG_IM-$version-dev"

Publish-Updater
Publish-PlayerEdition $playerFolder
Publish-DevEdition $devFolder

Get-ChildItem "$playerFolder.zip", "$devFolder.zip" | Format-Table Name, Length -AutoSize

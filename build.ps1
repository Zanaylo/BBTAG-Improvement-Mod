param(
    [string]$Output = (Join-Path $PSScriptRoot 'export\BBTAG_IM'),
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'

$vsInstaller = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer'
if (Test-Path $vsInstaller) {
    $env:PATH = "$vsInstaller;$env:PATH"
}

function Invoke-Dotnet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($args -join ' ') failed with exit code $LASTEXITCODE"
    }
}

if (-not $SkipTests) {
    Invoke-Dotnet test (Join-Path $PSScriptRoot 'tests\BBTAG_IM.Core.Tests') -c Release
}

if (Test-Path $Output) {
    Get-ChildItem $Output -File | Remove-Item -Force
}

Invoke-Dotnet publish (Join-Path $PSScriptRoot 'src\BBTAG_IM.Payload') -c Release -o $Output
Invoke-Dotnet publish (Join-Path $PSScriptRoot 'src\BBTAG_IM.Launcher') -c Release -o $Output

Get-ChildItem $Output -Filter *.pdb | Remove-Item -Force
New-Item -ItemType Directory -Force (Join-Path $Output 'mods') | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'resource\README.txt') $Output -Force

Get-ChildItem $Output | Format-Table Name, Length -AutoSize

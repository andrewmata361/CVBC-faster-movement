param(
    [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Castlevania Belmont's Curse"
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path (Join-Path $GameDir "BepInEx\interop\Assembly-CSharp.dll"))) {
    throw "BepInEx interop was not found under $GameDir. Install BepInEx 6 IL2CPP and launch the game once before building."
}

dotnet build (Join-Path $PSScriptRoot "RoseSpeed.csproj") -c Release "-p:GameDir=$GameDir"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$built = Join-Path $PSScriptRoot "bin\Release\RoseSpeed.dll"
Copy-Item -Force $built (Join-Path $GameDir "BepInEx\plugins\RoseSpeed.dll")
New-Item -ItemType Directory -Force -Path (Join-Path $PSScriptRoot "release") | Out-Null
Copy-Item -Force $built (Join-Path $PSScriptRoot "release\RoseSpeed.dll")
Write-Output "Installed $(Join-Path $GameDir 'BepInEx\plugins\RoseSpeed.dll')"

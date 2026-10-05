# Builds the release: one self-contained Inset.exe (no .NET install needed) and a zip to hand out.
#
#   powershell -ExecutionPolicy Bypass -File tools\publish.ps1 [-Runtime win-x64|win-arm64]
#
# Output: out\publish\<runtime>\ and out\Inset-<version>-<runtime>.zip (out\ is not tracked).
param([string]$Runtime = "win-x64")
$ErrorActionPreference = "Stop"

$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root "src\LookUp\LookUp.csproj"
$version = ([xml](Get-Content $project -Raw)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
$publish = Join-Path $root "out\publish\$Runtime"
$stage = Join-Path $root "out\stage\Inset"
$zip = Join-Path $root "out\Inset-$version-$Runtime.zip"

foreach ($dir in $publish, (Split-Path $stage -Parent)) {
    if (Test-Path $dir) { Remove-Item $dir -Recurse -Force }
}

# WPF cannot be trimmed; compression roughly halves the self-contained exe.
dotnet publish $project -c Release -r $Runtime --self-contained true -o $publish `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

New-Item -ItemType Directory -Force $stage | Out-Null
Copy-Item (Join-Path $publish "Inset.exe") $stage
Copy-Item (Join-Path $root "README.md") $stage
# The how-to guide, under a Chinese name for the people it is written for. Spelled out by code point
# because Windows PowerShell reads this BOM-less script as ANSI.
$guideName = -join ([char[]](0x4F7F, 0x7528, 0x8AAA, 0x660E)) + ".html"   # "shi yong shuo ming": how to use
Copy-Item (Join-Path $root "src\LookUp\Web\guide.html") (Join-Path $stage $guideName)
New-Item -ItemType Directory -Force (Join-Path $stage "licenses") | Out-Null
Copy-Item (Join-Path $root "src\LookUp\Assets\Fonts\Geist-OFL.txt") (Join-Path $stage "licenses")

if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path $stage -DestinationPath $zip
Remove-Item (Split-Path $stage -Parent) -Recurse -Force

$exe = Get-Item (Join-Path $publish "Inset.exe")
"Inset.exe  {0:N1} MB" -f ($exe.Length / 1MB)
"{0}  {1:N1} MB" -f (Split-Path $zip -Leaf), ((Get-Item $zip).Length / 1MB)

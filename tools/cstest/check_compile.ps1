# Compile every script of the Evolve assembly (Assets/_Project/Scripts, minus Net/) with the Roslyn that ships inside the Unity editor:
# a full "does it build" check with the editor closed or busy, no Unity MCP needed.  Usage:  powershell -File tools/cstest/check_compile.ps1 [-UnityData "D:\unityhub\6000.6.9f1\Editor\Data"]
# Needs Library/ScriptAssemblies from an earlier Unity import (the package assemblies are referenced from there). Prints compiler errors; "exit: 0" means it builds.
param(
    [string]$UnityData = "",
    [string]$Out = "$env:TEMP\Evolve_check.dll"
)
$ErrorActionPreference = "Stop"
$proj = (Resolve-Path "$PSScriptRoot\..\..").Path
if (-not $UnityData) {
    $p = (Get-Process Unity -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowTitle } | Select-Object -First 1).Path
    if ($p) { $UnityData = Join-Path (Split-Path $p) "Data" }
}
if (-not $UnityData -or -not (Test-Path "$UnityData\DotNetSdk")) { throw "Pass -UnityData <...\Editor\Data> (the editor folder that holds DotNetSdk and Managed)." }
$dotnet = "$UnityData\NetCoreRuntime\dotnet.exe"
$csc = (Get-ChildItem "$UnityData\DotNetSdk\sdk" -Directory | Select-Object -First 1).FullName + "\Roslyn\bincore\csc.dll"

$srcs = Get-ChildItem "$proj\Assets\_Project\Scripts" -Recurse -Filter *.cs | Where-Object { $_.FullName -notmatch '\\Scripts\\Net\\' } | ForEach-Object { $_.FullName }
$refs = @(Get-ChildItem "$UnityData\Managed\UnityEngine" -Filter *.dll | ForEach-Object { $_.FullName })
$refs += "$UnityData\NetStandard\ref\2.1.0\netstandard.dll"
$lib = "$proj\Library\ScriptAssemblies"
foreach ($n in @("Unity.InputSystem", "Unity.RenderPipelines.Core.Runtime", "Unity.RenderPipelines.Universal.Runtime", "Evolve.Net", "UnityEngine.UI", "Unity.TextMeshPro", "Unity.Mathematics", "Unity.Collections", "Unity.Burst", "Unity.Netcode.Runtime", "Unity.Networking.Transport", "Unity.Netcode.Components")) {
    $f = "$lib\$n.dll"; if (Test-Path $f) { $refs += $f }
}
$refs += Get-ChildItem "$UnityData\NetStandard\compat\2.1.0\shims\netstandard" -Filter *.dll -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName }

$lines = @("-nologo", "-target:library", "-langversion:9.0", "-nostdlib+", "-nowarn:CS0618,CS0219,CS0414,CS0649,CS0169,CS0162,CS0168,CS1591,UAC1001", "-define:UNITY_EDITOR;UNITY_6000_0_OR_NEWER;UNITY_2022_1_OR_NEWER;UNITY_STANDALONE_WIN", "-out:$Out")
foreach ($r in $refs) { $lines += "-r:`"$r`"" }
foreach ($s in $srcs) { $lines += "`"$s`"" }
Set-Content -Path "$Out.rsp" -Value $lines -Encoding UTF8
& $dotnet $csc "@$Out.rsp" 2>&1 | Select-Object -First 80
"exit: $LASTEXITCODE"

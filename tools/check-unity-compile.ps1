param([string]$UnityCache='D:/gametienhoa')
$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/..").Path
$output=Join-Path $root 'Temp/HudChecks'
New-Item -ItemType Directory -Force -Path $output|Out-Null
$rsp=Get-ChildItem "$UnityCache/Library/Bee/artifacts" -Filter Evolve.rsp -Recurse|Sort-Object LastWriteTime -Descending|Select-Object -First 1
if(!$rsp){throw 'No Unity compiler response file available'}
$sourceSet=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$lines=foreach($line in Get-Content $rsp.FullName){
 if($line -match '^-out:'){ '-out:"'+$output+'/Evolve.dll"' }
 elseif($line -match '^-refout:'){ '-refout:"'+$output+'/Evolve.ref.dll"' }
 elseif($line.Trim('"') -match '^Assets/.*\.cs$'){
  $relative=$line.Trim('"');$source=Join-Path $root $relative
  if(Test-Path -LiteralPath $source){[void]$sourceSet.Add([IO.Path]::GetFullPath($source));'"'+$source+'"'}
 }else{$line}
}
foreach($source in Get-ChildItem "$root/Assets/_Project/Scripts" -Filter *.cs -Recurse){
 if($source.FullName -like '*\Scripts\Net\*'){continue}
 if($sourceSet.Add($source.FullName)){$lines+='"'+$source.FullName+'"'}
}
$response=Join-Path $output 'compile.rsp';$lines|Set-Content $response -Encoding utf8
Push-Location $UnityCache
try{
 & D:/unityhub/6000.6.9f1/Editor/Data/NetCoreRuntime/dotnet.exe D:/unityhub/6000.6.9f1/Editor/Data/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll "@$response"
 if($LASTEXITCODE -ne 0){throw 'Unity source compilation failed'}
}finally{Pop-Location}
Write-Output 'PASS: clone sources compiled with Unity references; shared checkout was read-only'

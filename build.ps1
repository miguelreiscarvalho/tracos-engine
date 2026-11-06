param([switch]$Test)
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$ink=Join-Path ${env:ProgramFiles} 'Common Files\microsoft shared\ink\Microsoft.Ink.dll'
New-Item -ItemType Directory -Force (Join-Path $root 'build') | Out-Null
$refs=@('System.dll','System.Core.dll','System.Drawing.dll','System.Runtime.Serialization.dll','System.Xaml.dll',"$framework\WPF\WindowsBase.dll","$framework\WPF\PresentationCore.dll","$framework\WPF\PresentationFramework.dll",$ink) | ForEach-Object { '/r:'+$_ }
& "$framework\csc.exe" /nologo /target:winexe /out:"$root\build\Tracos.exe" $refs "$root\src\*.cs"
if($LASTEXITCODE -ne 0){throw 'Compilação falhou'}
& "$framework\csc.exe" /nologo /target:exe /out:"$root\build\Tracos.Tests.exe" $refs "$root\src\*.cs"
if($LASTEXITCODE -ne 0){throw 'Compilação dos testes falhou'}
Copy-Item -LiteralPath $ink -Destination "$root\build\Microsoft.Ink.dll"
if($Test){ & "$root\build\Tracos.Tests.exe" --test; if($LASTEXITCODE -ne 0){throw 'Testes falharam'} }

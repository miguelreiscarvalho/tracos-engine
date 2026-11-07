param(
    [switch]$Test,
    [switch]$CoreOnly,
    [string]$InkAssemblyPath = $env:TRACOS_INK_ASSEMBLY_PATH
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'Compilador .NET Framework x64 ausente. Execute em Windows com .NET Framework 4.8.'
}
$output = Join-Path $root 'build'
if ($CoreOnly) { $output = Join-Path $output 'core' }
New-Item -ItemType Directory -Force $output | Out-Null

function Compile([string]$Target, [string]$Name, [string[]]$References, [string[]]$Sources, [string[]]$Options = @()) {
    $arguments = @('/nologo', '/warnaserror+', "/target:$Target", "/out:$(Join-Path $output $Name)")
    $arguments += $Options
    $arguments += $References | ForEach-Object { '/r:' + $_ }
    $arguments += $Sources
    & $compiler @arguments
    if ($LASTEXITCODE -ne 0) { throw "Compilação falhou: $Name" }
}

$baseReferences = @('System.dll', 'System.Core.dll', 'System.Runtime.Serialization.dll')
$core = Join-Path $output 'Tracos.Core.dll'
Compile 'library' 'Tracos.Core.dll' $baseReferences @("$root\src\Core\*.cs")
$testReferences = $baseReferences + @($core)
$testSources = @("$root\tests\Tracos.Tests\*.cs")
$testOptions = @()

if (-not $CoreOnly) {
    if (-not $InkAssemblyPath) {
        $InkAssemblyPath = Join-Path ${env:ProgramFiles} 'Common Files\microsoft shared\ink\Microsoft.Ink.dll'
    }
    if (-not (Test-Path -LiteralPath $InkAssemblyPath)) {
        throw 'Microsoft.Ink.dll ausente. Use -CoreOnly para testar o núcleo ou -InkAssemblyPath para indicar sua instalação Windows Ink.'
    }
    $ink = (Resolve-Path -LiteralPath $InkAssemblyPath).Path
    $adapter = Join-Path $output 'Tracos.WindowsInk.dll'
    Compile 'library' 'Tracos.WindowsInk.dll' ($baseReferences + @($core, 'System.Drawing.dll', $ink)) @("$root\src\WindowsInk\*.cs")
    Copy-Item -LiteralPath $ink -Destination (Join-Path $output 'Microsoft.Ink.dll')
    $wpf = @('System.Xaml.dll', "$framework\WPF\WindowsBase.dll", "$framework\WPF\PresentationCore.dll", "$framework\WPF\PresentationFramework.dll")
    Compile 'winexe' 'Tracos.exe' ($baseReferences + $wpf + @($core, $adapter)) @("$root\examples\Tracos.Demo\*.cs")
    Compile 'exe' 'Tracos.RecognizeInk.exe' ($baseReferences + @($core, $adapter)) @("$root\examples\RecognizeInk\*.cs")
    $testReferences += $adapter
    $testOptions = @('/define:WINDOWS_INK')
} else {
    $testSources = @("$root\tests\Tracos.Tests\Experiments.cs", "$root\tests\Tracos.Tests\Metrics.cs")
}
Compile 'exe' 'Tracos.Tests.exe' $testReferences $testSources $testOptions
if ($Test) {
    Push-Location $root
    try {
        & (Join-Path $output 'Tracos.Tests.exe') --test
        if ($LASTEXITCODE -ne 0) { throw 'Testes falharam' }
    } finally { Pop-Location }
}
Write-Host "Build concluído: $output"

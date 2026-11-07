param([string]$InkAssemblyPath = $env:TRACOS_INK_ASSEMBLY_PATH)
$ErrorActionPreference = 'Stop'
if (-not $InkAssemblyPath) {
    $InkAssemblyPath = Join-Path ${env:ProgramFiles} 'Common Files\microsoft shared\ink\Microsoft.Ink.dll'
}
if (-not (Test-Path -LiteralPath $InkAssemblyPath)) { throw 'Microsoft.Ink.dll not found. Use -InkAssemblyPath.' }
Add-Type -Path $InkAssemblyPath
$recognizers = New-Object Microsoft.Ink.Recognizers
foreach ($recognizer in $recognizers) {
    Write-Output ($recognizer.Name + ' | languages=' + ($recognizer.Languages -join ','))
}
Write-Output 'RecognizerContext methods:'
[Microsoft.Ink.RecognizerContext].GetMethods() | Where-Object Name -Match 'Recognize|Dispose' | ForEach-Object { $_.ToString() }
[Microsoft.Ink.Ink].GetMethods() | Where-Object Name -Match 'CreateStroke' | ForEach-Object { $_.ToString() }
[Microsoft.Ink.RecognitionResult].GetProperties() | ForEach-Object { $_.ToString() }

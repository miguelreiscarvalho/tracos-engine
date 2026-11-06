$ErrorActionPreference = 'Stop'
Add-Type -Path 'C:\Program Files\Common Files\microsoft shared\ink\Microsoft.Ink.dll'
$recognizers = New-Object Microsoft.Ink.Recognizers
foreach ($recognizer in $recognizers) {
    Write-Output ($recognizer.Name + ' | languages=' + ($recognizer.Languages -join ','))
}
Write-Output 'RecognizerContext methods:'
[Microsoft.Ink.RecognizerContext].GetMethods() | Where-Object Name -Match 'Recognize|Dispose' | ForEach-Object { $_.ToString() }
[Microsoft.Ink.Ink].GetMethods() | Where-Object Name -Match 'CreateStroke' | ForEach-Object { $_.ToString() }
[Microsoft.Ink.RecognitionResult].GetProperties() | ForEach-Object { $_.ToString() }

$ErrorActionPreference='Stop'
Push-Location $PSScriptRoot
try {
    if(-not (Test-Path -LiteralPath '.\build\Tracos.exe')) { & .\build.ps1 }
    Start-Process -FilePath (Join-Path $PSScriptRoot 'build\Tracos.exe') -WorkingDirectory $PSScriptRoot -WindowStyle Hidden
} finally { Pop-Location }

$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'The .NET Framework C# compiler was not found. Install .NET Framework 4.8 development tools to build from source.'
}
$output = Join-Path $PSScriptRoot 'DbwPatcher.exe'
& $compiler /nologo /target:winexe /platform:anycpu /r:System.Windows.Forms.dll ("/out:" + $output) (Join-Path $PSScriptRoot 'src\DbwPatcher.cs')
if ($LASTEXITCODE -ne 0) { throw 'Patcher compilation failed.' }
Write-Output "Created $output"

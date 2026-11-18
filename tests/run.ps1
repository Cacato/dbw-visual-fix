param([string[]]$OriginalPaths = @())
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler)) { throw 'The .NET Framework C# compiler is required.' }
$runner = Join-Path ([IO.Path]::GetTempPath()) ('dbw-regression-' + [Guid]::NewGuid().ToString('N') + '.exe')
try {
    & $compiler /nologo /target:exe /platform:x86 /main:RegressionTests /r:System.Windows.Forms.dll ("/out:" + $runner) (Join-Path $root 'src\DbwPatcher.cs') (Join-Path $PSScriptRoot 'RegressionTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
    & $runner @OriginalPaths
    if ($LASTEXITCODE -ne 0) { throw 'Regression tests failed.' }
}
finally { if (Test-Path -LiteralPath $runner) { Remove-Item -LiteralPath $runner } }

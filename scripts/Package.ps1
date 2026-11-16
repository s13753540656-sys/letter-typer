param([Parameter(Mandatory=$true)][string]$Root)
$ErrorActionPreference = 'Stop'
$rootPath = (Resolve-Path -LiteralPath $Root).Path
$version = [IO.File]::ReadAllText((Join-Path $rootPath 'VERSION')).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid VERSION.' }

# Explicit release manifests: never enumerate the workspace or private inputs.
$portableFiles = @(
    'LetterTyper.cs', 'start.cmd', 'verify.cmd', 'config.example.json',
    'examples/sample.txt', 'README.md', 'LICENSE', 'VERSION',
    'THIRD_PARTY_NOTICES.md', 'DEPENDENCIES.md',
    'docs/USAGE.md', 'docs/PRIVACY.md', 'docs/VALIDATION.md', 'docs/images/window.png'
)
$sourceFiles = $portableFiles + @('.gitignore', 'CHANGELOG.md', 'docs/GITHUB.md', 'package.cmd',
    'scripts/Package.ps1', 'scripts/Capture.ps1', 'scripts/KeyboardSmoke.ps1', 'tests/KeyboardSmoke.cs')
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$outputDir = Join-Path $rootPath 'dist'
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$allArchives = @()
foreach ($kind in @('source', 'portable')) {
    $manifest = if ($kind -eq 'source') { $sourceFiles } else { $portableFiles }
    $prefix = "LetterTyper-$version-$kind"
    $zipPath = Join-Path $outputDir "$prefix.zip"
    # Only delete an exact, known build artifact; never a recursive path.
    if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath }
    $stream = [IO.File]::Open($zipPath, [IO.FileMode]::CreateNew)
    $archive = New-Object IO.Compression.ZipArchive($stream, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($relative in $manifest) {
            $filePath = Join-Path $rootPath $relative
            $item = Get-Item -LiteralPath $filePath -ErrorAction Stop
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Links cannot be packaged: $relative" }
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $filePath, "$prefix/$relative", [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    } finally { $archive.Dispose(); $stream.Dispose() }
    $allArchives += $zipPath
}
$hashLines = foreach ($zipPath in $allArchives) {
    $algorithm = [Security.Cryptography.SHA256]::Create()
    $inputStream = [IO.File]::OpenRead($zipPath)
    try { $digest = [BitConverter]::ToString($algorithm.ComputeHash($inputStream)).Replace('-', '').ToLowerInvariant() }
    finally { $inputStream.Dispose(); $algorithm.Dispose() }
    "$digest  $([IO.Path]::GetFileName($zipPath))"
}
[IO.File]::WriteAllLines((Join-Path $outputDir 'SHA256SUMS.txt'), [string[]]$hashLines, [Text.UTF8Encoding]::new($false))
$allArchives | ForEach-Object { Write-Output $_ }

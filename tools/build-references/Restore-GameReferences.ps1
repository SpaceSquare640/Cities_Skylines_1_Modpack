# Signature: OpenAI-Codex
# Requires PowerShell 7.4+; never execute or publish the reference assemblies.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$BundleDirectory,
    [Parameter(Mandatory)][string]$GameRoot
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$allowed = @('ICities.dll','Assembly-CSharp.dll','ColossalManaged.dll','UnityEngine.dll',
             'mscorlib.dll','System.dll','System.Core.dll','System.Xml.dll')
$keyBytes = $null
$plain = $null
$outputs = @()
try {
    if (-not $IsWindows) { throw 'This restore helper supports Windows cloud runners only.' }
    if ([string]::IsNullOrWhiteSpace($env:CS1_GAME_REFERENCES_KEY)) {
        throw 'CS1_GAME_REFERENCES_KEY is not configured.'
    }
    $keyBytes = [Convert]::FromBase64String($env:CS1_GAME_REFERENCES_KEY)
    if ($keyBytes.Length -ne 32) { throw 'The reference key must contain 32 bytes.' }
    if ([string]::IsNullOrWhiteSpace($env:RUNNER_TEMP)) { throw 'RUNNER_TEMP is required.' }
    $tempRoot = [IO.Path]::GetFullPath($env:RUNNER_TEMP).TrimEnd(
        [char[]]@([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar))
    $GameRoot = [IO.Path]::GetFullPath($GameRoot)
    if (-not $GameRoot.StartsWith($tempRoot + [IO.Path]::DirectorySeparatorChar,
                                 [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Reference extraction must stay below RUNNER_TEMP.'
    }
    if ((Get-Item -LiteralPath $tempRoot).Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw 'RUNNER_TEMP must not be a reparse point.'
    }
    $manifest = Get-Content -LiteralPath (Join-Path $BundleDirectory 'manifest.json') -Raw |
        ConvertFrom-Json
    if ($manifest.SchemaVersion -ne 1 -or $manifest.Algorithm -cne 'AES-256-GCM') {
        throw 'Unsupported reference manifest.'
    }
    $files = @($manifest.Files)
    if ($files.Count -ne $allowed.Count -or
        @($files.Name | Sort-Object -Unique).Count -ne $allowed.Count) {
        throw 'The manifest must contain exactly the eight approved reference files.'
    }
    foreach ($file in $files) {
        if ($file.Name -cnotin $allowed -or $file.SHA256 -cnotmatch '^[0-9a-f]{64}$' -or
            [long]$file.Bytes -lt 1 -or [long]$file.Bytes -gt 33554432) {
            throw 'Invalid reference manifest entry.'
        }
    }
    $payloadPath = Join-Path $BundleDirectory 'references.aesgcm'
    $payloadItem = Get-Item -LiteralPath $payloadPath
    if ($payloadItem.Length -le 36 -or $payloadItem.Length -gt 67108864) {
        throw 'Invalid encrypted reference size.'
    }
    $payload = [IO.File]::ReadAllBytes($payloadPath)
    $payloadHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($payload)).ToLowerInvariant()
    if ($payloadHash -cne $manifest.CiphertextSHA256) { throw 'Ciphertext checksum mismatch.' }
    $magic = [Text.Encoding]::ASCII.GetBytes('CS1REF01')
    if ([Text.Encoding]::ASCII.GetString($payload, 0, 8) -cne 'CS1REF01') {
        throw 'Invalid encrypted reference header.'
    }
    [byte[]]$nonce = $payload[8..19]
    [byte[]]$tag = $payload[20..35]
    [byte[]]$cipher = $payload[36..($payload.Length - 1)]
    $plain = [byte[]]::new($cipher.Length)
    $aes = [Security.Cryptography.AesGcm]::new($keyBytes, 16)
    try { $aes.Decrypt($nonce, $cipher, $tag, $plain, $magic) }
    finally { $aes.Dispose() }
    $memory = [IO.MemoryStream]::new($plain, $false)
    $archive = [IO.Compression.ZipArchive]::new($memory, [IO.Compression.ZipArchiveMode]::Read, $true)
    try {
        if ($archive.Entries.Count -ne $allowed.Count) { throw 'Unexpected archive entry count.' }
        $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($entry in $archive.Entries) {
            $match = @($files | Where-Object { ('Cities_Data/Managed/' + $_.Name) -ceq $entry.FullName })
            if ($match.Count -ne 1 -or -not $seen.Add($entry.FullName) -or
                $entry.Length -ne [long]$match[0].Bytes) {
                throw 'Unexpected, duplicate or incorrectly sized archive entry.'
            }
            $inputStream = $entry.Open()
            $outputStream = [IO.MemoryStream]::new()
            try { $inputStream.CopyTo($outputStream); $data = $outputStream.ToArray() }
            finally { $inputStream.Dispose(); $outputStream.Dispose() }
            $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($data)).ToLowerInvariant()
            if ($hash -cne $match[0].SHA256) { throw 'Reference checksum mismatch.' }
            $outputs += @{ Name = $match[0].Name; Data = $data }
        }
    } finally { $archive.Dispose(); $memory.Dispose() }
    # Check every existing ancestor inside RUNNER_TEMP before creating any files.
    $parent = [IO.DirectoryInfo]::new($GameRoot)
    while ($null -ne $parent -and -not $parent.FullName.Equals($tempRoot, [StringComparison]::OrdinalIgnoreCase)) {
        if (Test-Path -LiteralPath $parent.FullName) {
            $item = Get-Item -LiteralPath $parent.FullName
            if (-not $item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
                throw 'Extraction ancestors must be ordinary directories.'
            }
        }
        $parent = $parent.Parent
    }
    $managed = Join-Path $GameRoot 'Cities_Data/Managed'
    foreach ($directory in @($GameRoot, (Join-Path $GameRoot 'Cities_Data'), $managed)) {
        if (Test-Path -LiteralPath $directory) {
            $item = Get-Item -LiteralPath $directory
            if (-not $item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
                throw 'Extraction directories must be ordinary directories.'
            }
        } else { New-Item -ItemType Directory -Path $directory -Force | Out-Null }
    }
    foreach ($output in $outputs) {
        if (Test-Path -LiteralPath (Join-Path $managed $output.Name)) {
            throw 'Reference extraction refuses to overwrite existing files.'
        }
    }
    foreach ($output in $outputs) {
        $stream = [IO.File]::Open((Join-Path $managed $output.Name), [IO.FileMode]::CreateNew)
        try { $stream.Write($output.Data, 0, $output.Data.Length) } finally { $stream.Dispose() }
    }
    Write-Output 'Verified and restored eight game-reference files into the isolated runner directory.'
} finally {
    if ($null -ne $keyBytes) { [Array]::Clear($keyBytes, 0, $keyBytes.Length) }
    if ($null -ne $plain) { [Array]::Clear($plain, 0, $plain.Length) }
    foreach ($output in $outputs) { [Array]::Clear($output.Data, 0, $output.Data.Length) }
}

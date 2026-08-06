[CmdletBinding()]
param(
    [string]$ProjectPath = (Split-Path -Parent $PSScriptRoot),
    [string]$UnityExe = "E:\Unity\2022.3.62f2c1\Editor\Unity.exe",
    [ValidateRange(1, 60)]
    [int]$TimeoutMinutes = 60,
    [switch]$ProbeOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$expectedHeadCommit = "57d50e091978836e0aa02f4740cf969c2079dc10"
$expectedUnityVersion = "2022.3.62f2c1"
$expectedScenePath = "Assets/Scenes/Bootstrap/Bootstrap.unity"
$expectedSceneGuid = "d5ba7b6c1b4ae954b9bbab4fb20481a2"
$addressableSettingsPath = "Assets/AddressableAssetsData/AddressableAssetSettings.asset"
$expectedAddressablesBuildOptionName = "BuildWithPlayer"
$expectedAddressablesBuildOptionValue = 1
$executeMethod = "Game.Editor.Build.ElementWarWindows64Build.Build"
$freshnessToleranceMilliseconds = [long]2000
$allowedBurstDebugDirectoryName = "ElementWar_BurstDebugInformation_DoNotShip"
$allowedBurstDebugRelativePath = "$allowedBurstDebugDirectoryName/Data/Plugins/x86_64/lib_burst_generated.txt"
$shutdownQuietTimeoutMilliseconds = 15000
$shutdownQuietPollIntervalMilliseconds = 250
$shutdownQuietRequiredConsecutiveAbsent = 3
$offlineReplayRunId = "20260806-085558518Z-aa758c37"
$approvedFormalPaths = @(
    "Docs/Features/Windows64AutomationBaseline.md",
    "Assets/Scripts/Editor/Build.meta",
    "Assets/Scripts/Editor/Build/ElementWarWindows64Build.cs",
    "Assets/Scripts/Editor/Build/ElementWarWindows64Build.cs.meta",
    "Tools/Verify-ElementWarWindows64.ps1",
    "Assets/AddressableAssetsData/AddressableAssetSettings.asset"
)
$knownSideEffectPaths = @(
    "Assembly-CSharp.csproj",
    "Assets/AddressableAssetsData/Windows/addressables_content_state.bin",
    "Assets/AddressableAssetsData/link.xml",
    "Assets/AddressableAssetsData/link.xml.meta"
)

if ($PSVersionTable.PSVersion.Major -lt 7 -or
    -not (Get-Command ConvertFrom-Json).Parameters.ContainsKey("DateKind")) {
    throw "PowerShell 7 with ConvertFrom-Json -DateKind support is required. Current version: $($PSVersionTable.PSVersion)"
}

function Resolve-NormalizedPath {
    param([Parameter(Mandatory)][string]$Path)

    return [IO.Path]::GetFullPath($Path)
}

function New-VerificationRunId {
    $timestamp = [DateTimeOffset]::UtcNow.ToString(
        "yyyyMMdd-HHmmssfff'Z'",
        [Globalization.CultureInfo]::InvariantCulture)
    return "$timestamp-$([Guid]::NewGuid().ToString('N').Substring(0, 8))"
}

function Get-FileSha256 {
    param([Parameter(Mandatory)][string]$Path)

    $stream = [IO.File]::Open(
        $Path,
        [IO.FileMode]::Open,
        [IO.FileAccess]::Read,
        ([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
    try {
        return [Convert]::ToHexString(
            [Security.Cryptography.SHA256]::HashData($stream)
        ).ToLowerInvariant()
    }
    finally {
        $stream.Dispose()
    }
}

function Get-StringSha256 {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Value)

    $bytes = [Text.UTF8Encoding]::new($false).GetBytes($Value)
    return [Convert]::ToHexString(
        [Security.Cryptography.SHA256]::HashData($bytes)
    ).ToLowerInvariant()
}

function Test-IsPathWithinOrEqualRoot {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Root
    )

    $normalizedPath = (Resolve-NormalizedPath -Path $Path).TrimEnd("\", "/")
    $normalizedRoot = (Resolve-NormalizedPath -Path $Root).TrimEnd("\", "/")
    if ([string]::Equals(
            $normalizedPath,
            $normalizedRoot,
            [StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }

    return $normalizedPath.StartsWith(
        "$normalizedRoot$([IO.Path]::DirectorySeparatorChar)",
        [StringComparison]::OrdinalIgnoreCase)
}

function Assert-NoReparsePointInPath {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Label
    )

    $normalizedPath = Resolve-NormalizedPath -Path $Path
    $pathRoot = [IO.Path]::GetPathRoot($normalizedPath)
    if ([string]::IsNullOrWhiteSpace($pathRoot)) {
        throw "$Label has no filesystem root: $normalizedPath"
    }

    if (Test-Path -LiteralPath $pathRoot) {
        $rootItem = Get-Item -LiteralPath $pathRoot -Force
        if (($rootItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "$Label has a reparse-point root: $pathRoot"
        }
    }

    $relative = $normalizedPath.Substring($pathRoot.Length)
    $segments = @($relative -split "[\\/]" | Where-Object { $_.Length -gt 0 })
    $current = $pathRoot
    foreach ($segment in $segments) {
        $current = Join-Path $current $segment
        if (Test-Path -LiteralPath $current) {
            $item = Get-Item -LiteralPath $current -Force
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "$Label contains a reparse point: $current"
            }
        }
    }
}

function New-ExactDirectory {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Label
    )

    $normalizedPath = Resolve-NormalizedPath -Path $Path
    Assert-NoReparsePointInPath -Path $normalizedPath -Label $Label
    if (Test-Path -LiteralPath $normalizedPath) {
        throw "$Label collision; refusing to overwrite or delete: $normalizedPath"
    }
    [void][IO.Directory]::CreateDirectory($normalizedPath)
    Assert-NoReparsePointInPath -Path $normalizedPath -Label $Label
    return $normalizedPath
}

function Write-NewJsonFile {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)]$Value,
        [int]$Depth = 32
    )

    $normalizedPath = Resolve-NormalizedPath -Path $Path
    Assert-NoReparsePointInPath -Path $normalizedPath -Label "JSON evidence path"
    if (Test-Path -LiteralPath $normalizedPath) {
        throw "Refusing to overwrite JSON evidence: $normalizedPath"
    }
    $parent = [IO.Path]::GetDirectoryName($normalizedPath)
    if ([string]::IsNullOrWhiteSpace($parent) -or
        -not (Test-Path -LiteralPath $parent -PathType Container)) {
        throw "JSON evidence parent does not exist: $parent"
    }

    $temporaryPath = "$normalizedPath.tmp-$PID-$([Guid]::NewGuid().ToString('N'))"
    try {
        $json = $Value | ConvertTo-Json -Depth $Depth
        [IO.File]::WriteAllText(
            $temporaryPath,
            $json,
            [Text.UTF8Encoding]::new($false))
        Move-Item -LiteralPath $temporaryPath -Destination $normalizedPath
    }
    finally {
        if (Test-Path -LiteralPath $temporaryPath -PathType Leaf) {
            Remove-Item -LiteralPath $temporaryPath -Force
        }
    }
}

function Write-JsonFileAtomically {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)]$Value,
        [int]$Depth = 40
    )

    $normalizedPath = Resolve-NormalizedPath -Path $Path
    Assert-NoReparsePointInPath -Path $normalizedPath -Label "Atomic JSON evidence path"
    $parent = [IO.Path]::GetDirectoryName($normalizedPath)
    if ([string]::IsNullOrWhiteSpace($parent) -or
        -not (Test-Path -LiteralPath $parent -PathType Container)) {
        throw "Atomic JSON evidence parent does not exist: $parent"
    }

    $temporaryPath = "$normalizedPath.tmp-$PID-$([Guid]::NewGuid().ToString('N'))"
    $replacementBackupPath = "$normalizedPath.replace-backup-$PID-$([Guid]::NewGuid().ToString('N'))"
    try {
        $json = $Value | ConvertTo-Json -Depth $Depth
        [IO.File]::WriteAllText(
            $temporaryPath,
            $json,
            [Text.UTF8Encoding]::new($false))
        if (Test-Path -LiteralPath $normalizedPath -PathType Leaf) {
            [IO.File]::Replace($temporaryPath, $normalizedPath, $replacementBackupPath)
        }
        else {
            Move-Item -LiteralPath $temporaryPath -Destination $normalizedPath
        }
    }
    finally {
        if (Test-Path -LiteralPath $temporaryPath -PathType Leaf) {
            Remove-Item -LiteralPath $temporaryPath -Force
        }
        if (Test-Path -LiteralPath $replacementBackupPath -PathType Leaf) {
            Remove-Item -LiteralPath $replacementBackupPath -Force
        }
    }
}

function ConvertTo-StableArray {
    param([Parameter(Mandatory)][AllowNull()][AllowEmptyCollection()]$Value)

    $array = [Collections.Generic.List[object]]::new()
    if ($null -ne $Value) {
        foreach ($item in @($Value)) {
            [void]$array.Add($item)
        }
    }
    Write-Output -NoEnumerate $array
}

function Get-PathState {
    param(
        [Parameter(Mandatory)][string]$Path,
        [string]$RelativePath
    )

    $normalizedPath = Resolve-NormalizedPath -Path $Path
    Assert-NoReparsePointInPath -Path $normalizedPath -Label "CAS path"
    if (Test-Path -LiteralPath $normalizedPath -PathType Leaf) {
        $file = Get-Item -LiteralPath $normalizedPath -Force
        return [pscustomobject][ordered]@{
            path = $RelativePath
            absolutePath = $normalizedPath
            state = "File"
            length = [long]$file.Length
            sha256 = Get-FileSha256 -Path $normalizedPath
        }
    }
    if (Test-Path -LiteralPath $normalizedPath -PathType Container) {
        return [pscustomobject][ordered]@{
            path = $RelativePath
            absolutePath = $normalizedPath
            state = "Directory"
            length = $null
            sha256 = $null
        }
    }

    return [pscustomobject][ordered]@{
        path = $RelativePath
        absolutePath = $normalizedPath
        state = "Missing"
        length = $null
        sha256 = $null
    }
}

function Test-PathStatesEqual {
    param(
        [Parameter(Mandatory)]$Expected,
        [Parameter(Mandatory)]$Actual
    )

    return [string]$Expected.state -ceq [string]$Actual.state -and
        $Expected.length -eq $Actual.length -and
        [string]$Expected.sha256 -ceq [string]$Actual.sha256
}

function Assert-CasPathState {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)]$Expected,
        [Parameter(Mandatory)][string]$Label
    )

    $actual = Get-PathState -Path $Path -RelativePath ([string]$Expected.path)
    if (-not (Test-PathStatesEqual -Expected $Expected -Actual $actual)) {
        throw "$Label CAS mismatch. Expected=$($Expected.state)/$($Expected.length)/$($Expected.sha256); actual=$($actual.state)/$($actual.length)/$($actual.sha256)."
    }
    return $actual
}

function Get-LockedFileState {
    param(
        [Parameter(Mandatory)][IO.FileStream]$Stream,
        [Parameter(Mandatory)][string]$Path,
        [string]$RelativePath
    )

    if (-not $Stream.CanRead -or -not $Stream.CanSeek) {
        throw "CAS file handle must be readable and seekable: $Path"
    }

    $originalPosition = $Stream.Position
    try {
        $Stream.Position = 0
        $length = [long]$Stream.Length
        $sha256 = [Convert]::ToHexString(
            [Security.Cryptography.SHA256]::HashData($Stream)
        ).ToLowerInvariant()
        if ([long]$Stream.Length -ne $length) {
            throw "CAS file length changed while its exclusive handle was held: $Path"
        }
        return [pscustomobject][ordered]@{
            path = $RelativePath
            absolutePath = Resolve-NormalizedPath -Path $Path
            state = "File"
            length = $length
            sha256 = $sha256
        }
    }
    finally {
        if ($Stream.CanSeek) {
            $Stream.Position = $originalPosition
        }
    }
}

function Invoke-CasPhaseHook {
    param(
        [AllowNull()][scriptblock]$Hook,
        [Parameter(Mandatory)][string]$Phase,
        [Parameter(Mandatory)]$Context
    )

    if ($null -ne $Hook) {
        $null = & $Hook $Phase $Context
    }
}

function Copy-VerifiedNewFile {
    param(
        [Parameter(Mandatory)][string]$Source,
        [Parameter(Mandatory)][string]$Destination,
        [Parameter(Mandatory)][string]$Label
    )

    $sourceState = Get-PathState -Path $Source -RelativePath $null
    if ($sourceState.state -cne "File") {
        throw "$Label source is not a file: $Source"
    }
    $normalizedDestination = Resolve-NormalizedPath -Path $Destination
    Assert-NoReparsePointInPath -Path $normalizedDestination -Label "$Label destination"
    if (Test-Path -LiteralPath $normalizedDestination) {
        throw "$Label destination collision: $normalizedDestination"
    }
    $parent = [IO.Path]::GetDirectoryName($normalizedDestination)
    [void][IO.Directory]::CreateDirectory($parent)
    Assert-NoReparsePointInPath -Path $parent -Label "$Label destination parent"
    [IO.File]::Copy($sourceState.absolutePath, $normalizedDestination, $false)
    $destinationState = Get-PathState -Path $normalizedDestination -RelativePath $null
    if (-not (Test-PathStatesEqual -Expected $sourceState -Actual $destinationState)) {
        throw "$Label byte verification failed: $normalizedDestination"
    }

    return [pscustomobject][ordered]@{
        source = $sourceState.absolutePath
        destination = $normalizedDestination
        length = $destinationState.length
        sha256 = $destinationState.sha256
        byteIdentical = $true
    }
}

function Get-GitStatusRecords {
    param([Parameter(Mandatory)][string]$RepositoryPath)

    $nativeOutput = & git -C $RepositoryPath status --porcelain=v1 -z --untracked-files=all
    if ($LASTEXITCODE -ne 0) {
        throw "git status failed with exit code $LASTEXITCODE."
    }
    $raw = if ($null -eq $nativeOutput) { "" } else { @($nativeOutput) -join "" }
    $records = @($raw.Split([char]0, [StringSplitOptions]::RemoveEmptyEntries))
    $result = foreach ($record in $records) {
        if ($record.Length -lt 4) {
            throw "Unexpected git status record: '$record'"
        }
        $status = $record.Substring(0, 2)
        if ($status.Contains("R") -or $status.Contains("C")) {
            throw "Rename/copy status is unsupported by the frozen-state guard: '$record'"
        }
        [pscustomobject][ordered]@{
            status = $status
            path = $record.Substring(3).Replace("\", "/")
        }
    }
    return @($result | Sort-Object path)
}

function Get-RepositoryStateSnapshot {
    param([Parameter(Mandatory)][string]$RepositoryPath)

    $snapshot = foreach ($record in @(Get-GitStatusRecords -RepositoryPath $RepositoryPath)) {
        $absolutePath = Resolve-NormalizedPath -Path (Join-Path $RepositoryPath $record.path)
        if (-not (Test-IsPathWithinOrEqualRoot -Path $absolutePath -Root $RepositoryPath)) {
            throw "Git status path escapes the repository: $($record.path)"
        }
        $state = Get-PathState -Path $absolutePath -RelativePath $record.path
        [pscustomobject][ordered]@{
            status = $record.status
            path = $record.path
            state = $state.state
            length = $state.length
            sha256 = $state.sha256
        }
    }
    return @($snapshot | Sort-Object path)
}

function Get-RepositoryStateFingerprint {
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$Snapshot
    )

    $canonical = @($Snapshot | Sort-Object path | ForEach-Object {
        "$($_.status)|$($_.path)|$($_.state)|$($_.length)|$($_.sha256)"
    }) -join [Environment]::NewLine
    return Get-StringSha256 -Value $canonical
}

function Compare-RepositoryStateSnapshots {
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$Before,
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$After
    )

    $beforeByPath = @{}
    foreach ($item in $Before) {
        if ($beforeByPath.ContainsKey([string]$item.path)) {
            throw "Duplicate before-state path: $($item.path)"
        }
        $beforeByPath[[string]$item.path] = $item
    }
    $afterByPath = @{}
    foreach ($item in $After) {
        if ($afterByPath.ContainsKey([string]$item.path)) {
            throw "Duplicate after-state path: $($item.path)"
        }
        $afterByPath[[string]$item.path] = $item
    }

    $allPaths = @((@($beforeByPath.Keys) + @($afterByPath.Keys)) | Sort-Object -Unique)
    $changes = foreach ($path in $allPaths) {
        $beforeItem = $beforeByPath[$path]
        $afterItem = $afterByPath[$path]
        $different = $null -eq $beforeItem -or
            $null -eq $afterItem -or
            [string]$beforeItem.status -cne [string]$afterItem.status -or
            [string]$beforeItem.state -cne [string]$afterItem.state -or
            $beforeItem.length -ne $afterItem.length -or
            [string]$beforeItem.sha256 -cne [string]$afterItem.sha256
        if ($different) {
            [pscustomobject][ordered]@{
                path = $path
                beforeStatus = if ($null -eq $beforeItem) { $null } else { $beforeItem.status }
                beforeState = if ($null -eq $beforeItem) { $null } else { $beforeItem.state }
                beforeLength = if ($null -eq $beforeItem) { $null } else { $beforeItem.length }
                beforeSha256 = if ($null -eq $beforeItem) { $null } else { $beforeItem.sha256 }
                afterStatus = if ($null -eq $afterItem) { $null } else { $afterItem.status }
                afterState = if ($null -eq $afterItem) { $null } else { $afterItem.state }
                afterLength = if ($null -eq $afterItem) { $null } else { $afterItem.length }
                afterSha256 = if ($null -eq $afterItem) { $null } else { $afterItem.sha256 }
            }
        }
    }
    return @($changes)
}

function Assert-IndexEmpty {
    param([Parameter(Mandatory)][string]$RepositoryPath)

    & git -C $RepositoryPath diff --cached --quiet --
    $exitCode = $LASTEXITCODE
    if ($exitCode -eq 0) {
        return
    }
    if ($exitCode -eq 1) {
        $staged = @(& git -C $RepositoryPath diff --cached --name-only --)
        throw "Git index is not empty: $($staged -join ', ')"
    }
    throw "git diff --cached --quiet failed with exit code $exitCode."
}

function Get-FormalFileSnapshot {
    param(
        [Parameter(Mandatory)][string]$RepositoryPath,
        [Parameter(Mandatory)][string[]]$RelativePaths
    )

    $snapshot = foreach ($relativePath in $RelativePaths) {
        $absolutePath = Resolve-NormalizedPath -Path (Join-Path $RepositoryPath $relativePath)
        if (-not (Test-IsPathWithinOrEqualRoot -Path $absolutePath -Root $RepositoryPath)) {
            throw "Formal file escapes the repository: $relativePath"
        }
        $state = Get-PathState -Path $absolutePath -RelativePath $relativePath
        if ($state.state -cne "File") {
            throw "Formal file is missing or not regular: $relativePath"
        }
        [pscustomobject][ordered]@{
            path = $relativePath
            length = $state.length
            sha256 = $state.sha256
        }
    }
    return @($snapshot)
}

function Compare-FileSnapshots {
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$Before,
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$After
    )

    $beforeByPath = @{}
    foreach ($item in $Before) { $beforeByPath[[string]$item.path] = $item }
    $afterByPath = @{}
    foreach ($item in $After) { $afterByPath[[string]$item.path] = $item }
    $allPaths = @((@($beforeByPath.Keys) + @($afterByPath.Keys)) | Sort-Object -Unique)
    return @($allPaths | ForEach-Object {
        $path = $_
        $beforeItem = $beforeByPath[$path]
        $afterItem = $afterByPath[$path]
        if ($null -eq $beforeItem -or
            $null -eq $afterItem -or
            $beforeItem.length -ne $afterItem.length -or
            [string]$beforeItem.sha256 -cne [string]$afterItem.sha256) {
            [pscustomobject][ordered]@{
                path = $path
                beforeLength = if ($null -eq $beforeItem) { $null } else { $beforeItem.length }
                beforeSha256 = if ($null -eq $beforeItem) { $null } else { $beforeItem.sha256 }
                afterLength = if ($null -eq $afterItem) { $null } else { $afterItem.length }
                afterSha256 = if ($null -eq $afterItem) { $null } else { $afterItem.sha256 }
            }
        }
    })
}

function Get-GeneratedProjectFileSnapshot {
    param([Parameter(Mandatory)][string]$RepositoryPath)

    $files = @(Get-ChildItem -LiteralPath $RepositoryPath -File -Force |
        Where-Object { $_.Extension -in @(".sln", ".csproj") } |
        Sort-Object Name)
    return @($files | ForEach-Object {
        Assert-NoReparsePointInPath -Path $_.FullName -Label "Generated project file"
        [pscustomobject][ordered]@{
            path = $_.Name
            length = [long]$_.Length
            sha256 = Get-FileSha256 -Path $_.FullName
        }
    })
}

function Get-KnownSideEffectSnapshot {
    param(
        [Parameter(Mandatory)][string]$RepositoryPath,
        [Parameter(Mandatory)][string[]]$RelativePaths
    )

    return @($RelativePaths | ForEach-Object {
        Get-PathState -Path (Join-Path $RepositoryPath $_) -RelativePath $_
    })
}

function Initialize-SideEffectBackup {
    param(
        [Parameter(Mandatory)][string]$RepositoryPath,
        [Parameter(Mandatory)][string]$EvidenceDirectory,
        [Parameter(Mandatory)][string[]]$RelativePaths
    )

    $root = New-ExactDirectory -Path (Join-Path $EvidenceDirectory "side-effects") -Label "Side-effect evidence directory"
    $prebuildDirectory = New-ExactDirectory -Path (Join-Path $root "prebuild") -Label "Prebuild side-effect backup directory"
    $states = @(Get-KnownSideEffectSnapshot -RepositoryPath $RepositoryPath -RelativePaths $RelativePaths)
    $files = foreach ($state in $states) {
        if ($state.state -ceq "Directory") {
            throw "Known side-effect path is a directory: $($state.path)"
        }
        $backupPath = $null
        $backupEvidence = $null
        if ($state.state -ceq "File") {
            $backupPath = Join-Path $prebuildDirectory $state.path
            $backupEvidence = Copy-VerifiedNewFile -Source $state.absolutePath -Destination $backupPath -Label "Prebuild side-effect backup"
        }
        [pscustomobject][ordered]@{
            path = $state.path
            absolutePath = $state.absolutePath
            state = $state.state
            length = $state.length
            sha256 = $state.sha256
            backupPath = $backupPath
            backup = $backupEvidence
        }
    }

    $capturedAt = [DateTimeOffset]::UtcNow
    $manifest = [ordered]@{
        schemaVersion = 1
        capturedAtUtc = $capturedAt.ToString("o")
        capturedAtUnixMilliseconds = [long]$capturedAt.ToUnixTimeMilliseconds()
        files = @($files)
    }
    $manifestPath = Join-Path $root "prebuild-manifest.json"
    Write-NewJsonFile -Path $manifestPath -Value $manifest
    return [pscustomobject][ordered]@{
        root = $root
        prebuildDirectory = $prebuildDirectory
        manifestPath = $manifestPath
        files = @($files)
    }
}

function Write-PostbuildSideEffectManifest {
    param(
        [Parameter(Mandatory)]$Backup,
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$PostbuildStates
    )

    $capturedAt = [DateTimeOffset]::UtcNow
    $manifestPath = Join-Path $Backup.root "postbuild-manifest.json"
    Write-NewJsonFile -Path $manifestPath -Value ([ordered]@{
        schemaVersion = 1
        capturedAtUtc = $capturedAt.ToString("o")
        capturedAtUnixMilliseconds = [long]$capturedAt.ToUnixTimeMilliseconds()
        files = @($PostbuildStates)
    })
    return $manifestPath
}

function Restore-KnownSideEffectsWithCas {
    param(
        [Parameter(Mandatory)]$Backup,
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$PostbuildStates,
        [AllowNull()][scriptblock]$PhaseHook
    )

    $startedAt = [DateTimeOffset]::UtcNow
    $resultPath = Join-Path $Backup.root "recovery-result.json"
    $postbuildDirectory = Join-Path $Backup.root "postbuild"
    $pathResults = [Collections.Generic.List[object]]::new()
    $preservedFiles = [Collections.Generic.List[object]]::new()
    $resultPathOwned = $false
    $completedCount = 0
    $mutationOccurred = $false
    $result = [ordered]@{
        schemaVersion = 2
        status = "InProgress"
        startedAtUtc = $startedAt.ToString("o")
        startedAtUnixMilliseconds = [long]$startedAt.ToUnixTimeMilliseconds()
        finishedAtUtc = $null
        finishedAtUnixMilliseconds = $null
        resultPath = $resultPath
        postbuildDirectory = $postbuildDirectory
        paths = $pathResults
        preservedPostbuildFiles = $preservedFiles
        restoredStates = @()
        error = $null
        resultWriteError = $null
    }

    try {
        Assert-NoReparsePointInPath -Path $Backup.root -Label "Side-effect recovery root"
        if (Test-Path -LiteralPath $resultPath) {
            throw "Side-effect recovery result collision: $resultPath"
        }
        $resultPathOwned = $true

        $preByPath = @{}
        $orderedPaths = [Collections.Generic.List[string]]::new()
        foreach ($item in @($Backup.files)) {
            $path = [string]$item.path
            if ([string]::IsNullOrWhiteSpace($path) -or $preByPath.ContainsKey($path)) {
                throw "Duplicate or empty prebuild side-effect path: '$path'"
            }
            $preByPath[$path] = $item
            [void]$orderedPaths.Add($path)
        }
        $postByPath = @{}
        foreach ($item in $PostbuildStates) {
            $path = [string]$item.path
            if ([string]::IsNullOrWhiteSpace($path) -or $postByPath.ContainsKey($path)) {
                throw "Duplicate or empty postbuild side-effect path: '$path'"
            }
            $postByPath[$path] = $item
        }
        if ($preByPath.Count -ne $postByPath.Count) {
            throw "Known side-effect pre/post path count mismatch."
        }

        foreach ($path in $orderedPaths) {
            $pre = $preByPath[$path]
            $post = $postByPath[$path]
            if ($null -eq $post) {
                throw "Postbuild side-effect snapshot is missing: $path"
            }
            $action = if ($pre.state -ceq "File") {
                "RestoreFromPrebuildBackup"
            }
            elseif ($pre.state -ceq "Missing" -and $post.state -ceq "File") {
                "MoveGeneratedToEvidence"
            }
            elseif ($pre.state -ceq "Missing" -and $post.state -ceq "Missing") {
                "KeepMissing"
            }
            else {
                "Unsupported"
            }
            $pathResult = [pscustomobject][ordered]@{
                path = $path
                action = $action
                status = "Skipped"
                immediatelyBefore = $null
                expectedPrebuild = [ordered]@{
                    state = $pre.state
                    length = $pre.length
                    sha256 = $pre.sha256
                }
                expectedPostbuild = [ordered]@{
                    state = $post.state
                    length = $post.length
                    sha256 = $post.sha256
                }
                phase = "Pending"
                failedAtPhase = $null
                replacementPath = $null
                replacementLockedState = $null
                replacementInstalled = $false
                quarantinePath = $null
                quarantinedState = $null
                targetLockedState = $null
                mutationOccurred = $false
                evidencePath = $null
                final = $null
                startedAtUtc = $null
                finishedAtUtc = $null
                error = $null
            }
            [void]$pathResults.Add($pathResult)
        }

        Write-JsonFileAtomically -Path $resultPath -Value $result

        for ($index = 0; $index -lt $orderedPaths.Count; $index++) {
            $path = $orderedPaths[$index]
            $post = $postByPath[$path]
            try {
                $observed = Assert-CasPathState -Path $post.absolutePath -Expected $post -Label "Pre-recovery $path"
                $pathResults[$index].immediatelyBefore = $observed
            }
            catch {
                $pathResults[$index].status = "Failed"
                $pathResults[$index].error = $_.Exception.Message
                throw
            }
        }

        [void](New-ExactDirectory -Path $postbuildDirectory -Label "Postbuild side-effect evidence directory")

        for ($index = 0; $index -lt $orderedPaths.Count; $index++) {
            $path = $orderedPaths[$index]
            $pre = $preByPath[$path]
            $post = $postByPath[$path]
            $sourcePath = [string]$post.absolutePath
            $pathResult = $pathResults[$index]
            $pathResult.status = "InProgress"
            $pathResult.phase = "Preparing"
            $pathResult.startedAtUtc = [DateTimeOffset]::UtcNow.ToString("o")
            Write-JsonFileAtomically -Path $resultPath -Value $result
            $targetStream = $null
            $replacementStream = $null
            try {
                if ($post.state -ceq "Directory") {
                    throw "Known postbuild side-effect became a directory: $path"
                }

                $replacementPath = $null
                if ($pre.state -ceq "File") {
                    [void](Assert-CasPathState -Path $pre.backupPath -Expected $pre -Label "Immediate prebuild backup $path")
                    $replacementName = ".$([IO.Path]::GetFileName($sourcePath)).elementwar-replacement-$([Guid]::NewGuid().ToString('N')).tmp"
                    $replacementPath = Resolve-NormalizedPath -Path (Join-Path ([IO.Path]::GetDirectoryName($sourcePath)) $replacementName)
                    if (-not (Test-IsPathWithinOrEqualRoot -Path $replacementPath -Root ([IO.Path]::GetDirectoryName($sourcePath)))) {
                        throw "CAS replacement escaped the target directory: $replacementPath"
                    }
                    $pathResult.replacementPath = $replacementPath
                    [void](Copy-VerifiedNewFile -Source $pre.backupPath -Destination $replacementPath -Label "CAS replacement preparation")
                    $replacementStream = [IO.File]::Open(
                        $replacementPath,
                        [IO.FileMode]::Open,
                        [IO.FileAccess]::Read,
                        [IO.FileShare]::Delete)
                    $replacementLockedState = Get-LockedFileState -Stream $replacementStream -Path $replacementPath -RelativePath $path
                    $pathResult.replacementLockedState = $replacementLockedState
                    if (-not (Test-PathStatesEqual -Expected $pre -Actual $replacementLockedState)) {
                        throw "Locked CAS replacement mismatch for '$path'. Expected=$($pre.state)/$($pre.length)/$($pre.sha256); actual=$($replacementLockedState.state)/$($replacementLockedState.length)/$($replacementLockedState.sha256)."
                    }
                }

                $quarantinePath = $null
                if ($post.state -ceq "File") {
                    $quarantinePath = Resolve-NormalizedPath -Path (Join-Path (Join-Path $postbuildDirectory "quarantine") $path)
                    if (-not (Test-IsPathWithinOrEqualRoot -Path $quarantinePath -Root $postbuildDirectory)) {
                        throw "CAS quarantine escaped the evidence directory: $quarantinePath"
                    }
                    Assert-NoReparsePointInPath -Path $quarantinePath -Label "CAS quarantine path"
                    if (Test-Path -LiteralPath $quarantinePath) {
                        throw "CAS quarantine collision: $quarantinePath"
                    }
                    if (-not [string]::Equals(
                            [IO.Path]::GetPathRoot($sourcePath),
                            [IO.Path]::GetPathRoot($quarantinePath),
                            [StringComparison]::OrdinalIgnoreCase)) {
                        throw "CAS quarantine must be on the target volume: $quarantinePath"
                    }
                    $quarantineParent = [IO.Path]::GetDirectoryName($quarantinePath)
                    [void][IO.Directory]::CreateDirectory($quarantineParent)
                    Assert-NoReparsePointInPath -Path $quarantineParent -Label "CAS quarantine parent"
                    $pathResult.quarantinePath = $quarantinePath
                }

                $hookContext = [pscustomobject][ordered]@{
                    path = $path
                    sourcePath = $sourcePath
                    replacementPath = $replacementPath
                    quarantinePath = $quarantinePath
                    expectedPrebuild = $pre
                    expectedPostbuild = $post
                }
                $pathResult.phase = "BeforeFinalTargetLock"
                Invoke-CasPhaseHook -Hook $PhaseHook -Phase "BeforeFinalTargetLock" -Context $hookContext

                if ($post.state -ceq "File") {
                    $pathResult.phase = "LockingAndHashingTarget"
                    $targetStream = [IO.File]::Open(
                        $sourcePath,
                        [IO.FileMode]::Open,
                        [IO.FileAccess]::Read,
                        [IO.FileShare]::Delete)
                    $lockedTargetState = Get-LockedFileState -Stream $targetStream -Path $sourcePath -RelativePath $path
                    $pathResult.targetLockedState = $lockedTargetState
                    $pathResult.immediatelyBefore = $lockedTargetState
                    if (-not (Test-PathStatesEqual -Expected $post -Actual $lockedTargetState)) {
                        throw "Locked target CAS mismatch for '$path'. Expected=$($post.state)/$($post.length)/$($post.sha256); actual=$($lockedTargetState.state)/$($lockedTargetState.length)/$($lockedTargetState.sha256)."
                    }

                    $pathResult.phase = "QuarantiningLockedTarget"
                    [IO.File]::Move($sourcePath, $quarantinePath, $false)
                    $mutationOccurred = $true
                    $pathResult.mutationOccurred = $true
                    $pathResult.evidencePath = $quarantinePath
                    $targetStream.Dispose()
                    $targetStream = $null

                    $quarantinedState = Assert-CasPathState -Path $quarantinePath -Expected $post -Label "Quarantined side effect $path"
                    $pathResult.quarantinedState = $quarantinedState
                    [void]$preservedFiles.Add([pscustomobject][ordered]@{
                        source = $sourcePath
                        destination = $quarantinePath
                        length = $quarantinedState.length
                        sha256 = $quarantinedState.sha256
                        byteIdentical = $true
                        moved = $true
                        quarantined = $true
                    })

                    $pathResult.phase = "TargetQuarantined"
                    Invoke-CasPhaseHook -Hook $PhaseHook -Phase "AfterTargetQuarantined" -Context $hookContext
                }
                elseif ($post.state -cne "Missing") {
                    throw "Unsupported side-effect transition for '$path': $($pre.state) -> $($post.state)"
                }

                if ($pre.state -ceq "File") {
                    $pathResult.phase = "InstallingReplacementWithoutOverwrite"
                    [IO.File]::Move($replacementPath, $sourcePath, $false)
                    $pathResult.replacementInstalled = $true
                    $replacementStream.Dispose()
                    $replacementStream = $null
                }
                elseif ($pre.state -cne "Missing") {
                    throw "Unsupported prebuild side-effect state for '$path': $($pre.state)"
                }

                $finalState = Assert-CasPathState -Path $pre.absolutePath -Expected $pre -Label "Recovered $path"
                $pathResult.final = $finalState
                $pathResult.phase = "Completed"
                $pathResult.status = "Succeeded"
                $completedCount++
            }
            catch {
                $originalException = $_.Exception
                $pathResult.failedAtPhase = $pathResult.phase
                $streamCloseErrors = [Collections.Generic.List[string]]::new()
                if ($null -ne $targetStream) {
                    try { $targetStream.Dispose() } catch { [void]$streamCloseErrors.Add("Target handle close failed: $($_.Exception.Message)") }
                    $targetStream = $null
                }
                if ($null -ne $replacementStream) {
                    try { $replacementStream.Dispose() } catch { [void]$streamCloseErrors.Add("Replacement handle close failed: $($_.Exception.Message)") }
                    $replacementStream = $null
                }
                $pathResult.status = "Failed"
                $pathResult.error = $originalException.Message
                if ($streamCloseErrors.Count -gt 0) {
                    $pathResult.error = "$($pathResult.error) $($streamCloseErrors -join ' ')"
                }
                try {
                    $pathResult.final = Get-PathState -Path $pre.absolutePath -RelativePath $path
                }
                catch {
                    $pathResult.error = "$($pathResult.error) Final-state capture failed: $($_.Exception.Message)"
                }
                throw [InvalidOperationException]::new($pathResult.error, $originalException)
            }
            finally {
                if ($null -ne $targetStream) {
                    $targetStream.Dispose()
                }
                if ($null -ne $replacementStream) {
                    $replacementStream.Dispose()
                }
                $pathResult.finishedAtUtc = [DateTimeOffset]::UtcNow.ToString("o")
                try {
                    Write-JsonFileAtomically -Path $resultPath -Value $result
                }
                catch {
                    $result.resultWriteError = "Intermediate recovery-result write failed: $($_.Exception.Message)"
                }
            }
        }

        $restoredStates = foreach ($path in $orderedPaths) {
            Assert-CasPathState -Path $preByPath[$path].absolutePath -Expected $preByPath[$path] -Label "Final side-effect state $path"
        }
        $result.restoredStates = @($restoredStates)
        $result.status = "Succeeded"
    }
    catch {
        $result.status = if ($completedCount -gt 0 -or $mutationOccurred) { "Partial" } else { "Failed" }
        $result.error = $_.Exception.Message
    }
    finally {
        $finishedAt = [DateTimeOffset]::UtcNow
        $result.finishedAtUtc = $finishedAt.ToString("o")
        $result.finishedAtUnixMilliseconds = [long]$finishedAt.ToUnixTimeMilliseconds()
        if ($resultPathOwned) {
            try {
                Write-JsonFileAtomically -Path $resultPath -Value $result
            }
            catch {
                $result.resultWriteError = $_.Exception.Message
                if ($result.status -ceq "Succeeded") {
                    $result.status = "Failed"
                    $result.error = "CAS completed but recovery result could not be written: $($result.resultWriteError)"
                }
            }
        }
    }

    return [pscustomobject]$result
}

function ConvertTo-Int64Strict {
    param(
        [Parameter(Mandatory)]$Value,
        [Parameter(Mandatory)][string]$Label
    )

    $text = [Convert]::ToString($Value, [Globalization.CultureInfo]::InvariantCulture)
    if ($text -notmatch "^-?[0-9]+$") {
        throw "$Label is not an Int64 integer: $text"
    }
    try {
        return [long]::Parse(
            $text,
            [Globalization.NumberStyles]::Integer,
            [Globalization.CultureInfo]::InvariantCulture)
    }
    catch {
        throw "$Label is outside Int64 range: $text"
    }
}

function Assert-IsoMatchesUnixMilliseconds {
    param(
        [Parameter(Mandatory)]$IsoValue,
        [Parameter(Mandatory)]$UnixMilliseconds,
        [Parameter(Mandatory)][string]$Label
    )

    if ($IsoValue -isnot [string]) {
        throw "$Label ISO value must remain a JSON string; actual type: $($IsoValue.GetType().FullName)"
    }
    try {
        $parsed = [DateTimeOffset]::Parse(
            $IsoValue,
            [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::RoundtripKind)
    }
    catch {
        throw "$Label ISO value is invalid: $IsoValue"
    }
    $unixValue = ConvertTo-Int64Strict -Value $UnixMilliseconds -Label "$Label Unix milliseconds"
    $parsedUnix = [long]$parsed.ToUnixTimeMilliseconds()
    $difference = [Math]::Abs([decimal]$parsedUnix - [decimal]$unixValue)
    if ($difference -gt 1) {
        throw "$Label ISO/Unix mismatch exceeds 1 ms. parsed=$parsedUnix numeric=$unixValue"
    }
    return [pscustomobject][ordered]@{
        label = $Label
        iso = $IsoValue
        unixMilliseconds = $unixValue
        parsedUnixMilliseconds = $parsedUnix
        differenceMilliseconds = [long]$difference
    }
}

function Assert-UnixMillisecondOrder {
    param(
        [Parameter(Mandatory)][Collections.IDictionary]$OrderedValues,
        [Parameter(Mandatory)][string]$Label
    )

    $previousName = $null
    $previousValue = $null
    $evidence = @()
    foreach ($name in $OrderedValues.Keys) {
        $value = ConvertTo-Int64Strict -Value $OrderedValues[$name] -Label "$Label $name"
        if ($null -ne $previousValue -and $value -lt $previousValue) {
            throw "$Label out of order: $name=$value is earlier than $previousName=$previousValue."
        }
        $evidence += [pscustomobject][ordered]@{
            name = [string]$name
            unixMilliseconds = $value
        }
        $previousName = [string]$name
        $previousValue = $value
    }
    return @($evidence)
}

function ConvertTo-ProcessCreationTime {
    param(
        [Parameter(Mandatory)]$Value,
        [Parameter(Mandatory)][string]$Label
    )

    try {
        if ($Value -is [DateTimeOffset]) {
            return ([DateTimeOffset]$Value).ToUniversalTime()
        }
        if ($Value -is [DateTime]) {
            $dateTime = [DateTime]$Value
            if ($dateTime.Kind -eq [DateTimeKind]::Unspecified) {
                $offset = [TimeZoneInfo]::Local.GetUtcOffset($dateTime)
                return ([DateTimeOffset]::new($dateTime, $offset)).ToUniversalTime()
            }
            return ([DateTimeOffset]$dateTime).ToUniversalTime()
        }
        $text = [string]$Value
        if ($text -match '^[0-9]{14}\.') {
            return ([DateTimeOffset][Management.ManagementDateTimeConverter]::ToDateTime($text)).ToUniversalTime()
        }
        return [DateTimeOffset]::Parse(
            $text,
            [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::AssumeLocal).ToUniversalTime()
    }
    catch {
        throw "$Label has an invalid process creation time: $Value"
    }
}

function Get-ProcessIdentityKey {
    param([Parameter(Mandatory)]$Identity)

    return "$([int]$Identity.processId)|$([long]$Identity.creationTimeUnixMilliseconds)"
}

function Get-ProcessIdentitySnapshot {
    $capturedAt = [DateTimeOffset]::UtcNow
    try {
        $processes = @(Get-CimInstance Win32_Process -ErrorAction Stop | ForEach-Object {
            $creation = ConvertTo-ProcessCreationTime -Value $_.CreationDate -Label "PID $($_.ProcessId)"
            [pscustomobject][ordered]@{
                processId = [int]$_.ProcessId
                parentProcessId = [int]$_.ParentProcessId
                creationTimeUtc = $creation.ToString("o")
                creationTimeUnixMilliseconds = [long]$creation.ToUnixTimeMilliseconds()
                name = [string]$_.Name
                executablePath = [string]$_.ExecutablePath
                commandLine = [string]$_.CommandLine
            }
        })
        return [pscustomobject][ordered]@{
            capturedAtUtc = $capturedAt.ToString("o")
            capturedAtUnixMilliseconds = [long]$capturedAt.ToUnixTimeMilliseconds()
            querySucceeded = $true
            queryError = $null
            processes = @($processes)
        }
    }
    catch {
        return [pscustomobject][ordered]@{
            capturedAtUtc = $capturedAt.ToString("o")
            capturedAtUnixMilliseconds = [long]$capturedAt.ToUnixTimeMilliseconds()
            querySucceeded = $false
            queryError = $_.Exception.Message
            processes = @()
        }
    }
}

function Test-ProcessIdentityPresent {
    param(
        [Parameter(Mandatory)]$Identity,
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$Processes
    )

    return @($Processes | Where-Object {
        [int]$_.processId -eq [int]$Identity.processId -and
            [long]$_.creationTimeUnixMilliseconds -eq [long]$Identity.creationTimeUnixMilliseconds
    }).Count -gt 0
}

function Get-DescendantProcessIdentities {
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$RootIdentities,
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$Processes
    )

    $knownPids = [Collections.Generic.HashSet[int]]::new()
    $rootKeys = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($root in $RootIdentities) {
        [void]$knownPids.Add([int]$root.processId)
        [void]$rootKeys.Add((Get-ProcessIdentityKey -Identity $root))
    }
    $descendantByKey = @{}
    $found = $true
    while ($found) {
        $found = $false
        foreach ($process in $Processes) {
            $key = Get-ProcessIdentityKey -Identity $process
            if ($knownPids.Contains([int]$process.parentProcessId) -and
                -not $rootKeys.Contains($key) -and
                -not $descendantByKey.ContainsKey($key)) {
                $descendantByKey[$key] = $process
                [void]$knownPids.Add([int]$process.processId)
                $found = $true
            }
        }
    }
    return @($descendantByKey.Values | Sort-Object processId, creationTimeUnixMilliseconds)
}

function Invoke-UnityProcess {
    param(
        [Parameter(Mandatory)][string]$ExecutablePath,
        [Parameter(Mandatory)][string[]]$Arguments,
        [Parameter(Mandatory)][int]$MaximumMinutes
    )

    Write-Host "[Windows64] $ExecutablePath $($Arguments -join ' ')" -ForegroundColor Cyan
    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $ExecutablePath
    $startInfo.UseShellExecute = $false
    foreach ($argument in $Arguments) { [void]$startInfo.ArgumentList.Add($argument) }
    $startedAt = [DateTimeOffset]::UtcNow
    $process = [Diagnostics.Process]::Start($startInfo)
    if ($null -eq $process) {
        throw "Failed to start Unity: $ExecutablePath"
    }

    $processId = $process.Id
    $creationTime = ([DateTimeOffset]$process.StartTime.ToUniversalTime()).ToUniversalTime()
    $processIdentity = [pscustomobject][ordered]@{
        processId = $processId
        creationTimeUtc = $creationTime.ToString("o")
        creationTimeUnixMilliseconds = [long]$creationTime.ToUnixTimeMilliseconds()
        name = [IO.Path]::GetFileName($ExecutablePath)
        executablePath = $ExecutablePath
    }
    $trackedChildren = @{}
    $trackingErrors = [Collections.Generic.List[string]]::new()
    $timedOut = $false
    $wasTerminated = $false
    $terminationIdentityVerified = $false
    $terminationError = $null
    $exitCode = $null
    $hasExited = $false
    $stopwatch = [Diagnostics.Stopwatch]::StartNew()
    try {
        $maximumMilliseconds = [long][TimeSpan]::FromMinutes($MaximumMinutes).TotalMilliseconds
        while ($true) {
            $snapshot = Get-ProcessIdentitySnapshot
            if ($snapshot.querySucceeded) {
                foreach ($child in @(Get-DescendantProcessIdentities -RootIdentities @($processIdentity) -Processes @($snapshot.processes))) {
                    $trackedChildren[(Get-ProcessIdentityKey -Identity $child)] = $child
                }
            }
            elseif (-not $trackingErrors.Contains([string]$snapshot.queryError)) {
                [void]$trackingErrors.Add([string]$snapshot.queryError)
            }

            if ($process.WaitForExit($shutdownQuietPollIntervalMilliseconds)) {
                $hasExited = $true
                break
            }
            if ($stopwatch.ElapsedMilliseconds -ge $maximumMilliseconds) {
                $timedOut = $true
                break
            }
        }

        $finalTrackingSnapshot = Get-ProcessIdentitySnapshot
        if ($finalTrackingSnapshot.querySucceeded) {
            foreach ($child in @(Get-DescendantProcessIdentities -RootIdentities @($processIdentity) -Processes @($finalTrackingSnapshot.processes))) {
                $trackedChildren[(Get-ProcessIdentityKey -Identity $child)] = $child
            }
        }
        elseif (-not $trackingErrors.Contains([string]$finalTrackingSnapshot.queryError)) {
            [void]$trackingErrors.Add([string]$finalTrackingSnapshot.queryError)
        }

        if ($timedOut -and -not $process.HasExited) {
            try {
                $currentCreation = ([DateTimeOffset]$process.StartTime.ToUniversalTime()).ToUniversalTime()
                $terminationIdentityVerified = $process.Id -eq $processId -and
                    [long]$currentCreation.ToUnixTimeMilliseconds() -eq [long]$processIdentity.creationTimeUnixMilliseconds
                if (-not $terminationIdentityVerified) {
                    throw "Launched Unity process identity no longer matches PID/start time; refusing termination."
                }
                $process.Kill()
                $wasTerminated = $true
                $hasExited = $process.WaitForExit(10000)
            }
            catch {
                $terminationError = $_.Exception.Message
            }
        }
        if ($process.HasExited) {
            $process.WaitForExit()
            $exitCode = $process.ExitCode
            $hasExited = $true
        }
        $finishedAt = [DateTimeOffset]::UtcNow
        return [pscustomobject][ordered]@{
            processId = $processId
            processIdentity = $processIdentity
            startedAtUtc = $startedAt.ToString("o")
            startedAtUnixMilliseconds = [long]$startedAt.ToUnixTimeMilliseconds()
            finishedAtUtc = $finishedAt.ToString("o")
            finishedAtUnixMilliseconds = [long]$finishedAt.ToUnixTimeMilliseconds()
            exitCode = $exitCode
            timedOut = $timedOut
            wasTerminated = $wasTerminated
            terminationIdentityVerified = $terminationIdentityVerified
            terminationError = $terminationError
            hasExited = $hasExited
            discoveredChildProcesses = @($trackedChildren.Values | Sort-Object processId, creationTimeUnixMilliseconds)
            processTrackingErrors = @($trackingErrors)
        }
    }
    finally {
        $stopwatch.Stop()
        $process.Dispose()
    }
}

function Get-ShutdownObservation {
    param([Parameter(Mandatory)][string]$LockPath)

    $snapshot = Get-ProcessIdentitySnapshot
    $lockQuerySucceeded = $true
    $lockQueryError = $null
    $lockfileExists = $false
    try {
        $lockfileExists = Test-Path -LiteralPath $LockPath -PathType Leaf
    }
    catch {
        $lockQuerySucceeded = $false
        $lockQueryError = $_.Exception.Message
    }
    return [pscustomobject][ordered]@{
        capturedAtUtc = $snapshot.capturedAtUtc
        capturedAtUnixMilliseconds = $snapshot.capturedAtUnixMilliseconds
        processQuerySucceeded = $snapshot.querySucceeded
        processQueryError = $snapshot.queryError
        processes = @($snapshot.processes)
        lockQuerySucceeded = $lockQuerySucceeded
        lockQueryError = $lockQueryError
        lockfileExists = $lockfileExists
    }
}

function Wait-UnityShutdownQuietPeriod {
    param(
        [Parameter(Mandatory)]$MainProcessIdentity,
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$TrackedChildIdentities,
        [Parameter(Mandatory)][string]$LockPath,
        [ValidateRange(1, 60000)][int]$TimeoutMilliseconds = 15000,
        [ValidateRange(1, 5000)][int]$PollIntervalMilliseconds = 250,
        [ValidateRange(1, 20)][int]$RequiredConsecutiveAbsent = 3,
        [scriptblock]$ObservationProvider,
        [scriptblock]$SleepAction
    )

    if ($null -eq $ObservationProvider) {
        $ObservationProvider = { Get-ShutdownObservation -LockPath $LockPath }
    }
    if ($null -eq $SleepAction) {
        $SleepAction = { param([int]$Milliseconds) Start-Sleep -Milliseconds $Milliseconds }
    }

    $startedAt = [DateTimeOffset]::UtcNow
    $trackedByKey = @{}
    foreach ($identity in $TrackedChildIdentities) {
        $trackedByKey[(Get-ProcessIdentityKey -Identity $identity)] = $identity
    }
    $childEvidenceByKey = @{}
    foreach ($key in $trackedByKey.Keys) {
        $childEvidenceByKey[$key] = [pscustomobject][ordered]@{
            identity = $trackedByKey[$key]
            lastSeenAtUtc = $null
            lastSeenAtUnixMilliseconds = $null
            firstAbsentAtUtc = $null
            firstAbsentAtUnixMilliseconds = $null
        }
    }
    $unrelatedByKey = @{}
    $queryErrors = [Collections.Generic.List[string]]::new()
    $pollCount = 0
    $consecutiveAbsent = 0
    $lastSeenAtUtc = $null
    $lastSeenAtUnix = $null
    $firstAbsentAtUtc = $null
    $firstAbsentAtUnix = $null
    $achievedAtUtc = $null
    $achievedAtUnix = $null
    $mainLastSeenAtUtc = $null
    $mainLastSeenAtUnix = $null
    $mainFirstAbsentAtUtc = $null
    $mainFirstAbsentAtUnix = $null
    $lockLastSeenAtUtc = $null
    $lockLastSeenAtUnix = $null
    $lockFirstAbsentAtUtc = $null
    $lockFirstAbsentAtUnix = $null
    $timedOut = $false
    $maximumPolls = [Math]::Max(1, [int][Math]::Ceiling($TimeoutMilliseconds / [double]$PollIntervalMilliseconds) + 1)

    while ($true) {
        $pollCount++
        $observation = & $ObservationProvider
        if ($null -eq $observation) {
            throw "Shutdown observation provider returned null."
        }
        $nowUtc = [string]$observation.capturedAtUtc
        $nowUnix = ConvertTo-Int64Strict -Value $observation.capturedAtUnixMilliseconds -Label "Shutdown observation time"
        $processQuerySucceeded = [bool]$observation.processQuerySucceeded
        $lockQuerySucceeded = [bool]$observation.lockQuerySucceeded
        $processes = @($observation.processes)
        if (-not $processQuerySucceeded -and
            -not [string]::IsNullOrWhiteSpace([string]$observation.processQueryError) -and
            -not $queryErrors.Contains([string]$observation.processQueryError)) {
            [void]$queryErrors.Add([string]$observation.processQueryError)
        }
        if (-not $lockQuerySucceeded -and
            -not [string]::IsNullOrWhiteSpace([string]$observation.lockQueryError) -and
            -not $queryErrors.Contains([string]$observation.lockQueryError)) {
            [void]$queryErrors.Add([string]$observation.lockQueryError)
        }

        if ($processQuerySucceeded) {
            foreach ($child in @(Get-DescendantProcessIdentities -RootIdentities @($MainProcessIdentity) -Processes $processes)) {
                $key = Get-ProcessIdentityKey -Identity $child
                if (-not $trackedByKey.ContainsKey($key)) {
                    $trackedByKey[$key] = $child
                    $childEvidenceByKey[$key] = [pscustomobject][ordered]@{
                        identity = $child
                        lastSeenAtUtc = $null
                        lastSeenAtUnixMilliseconds = $null
                        firstAbsentAtUtc = $null
                        firstAbsentAtUnixMilliseconds = $null
                    }
                }
            }
            foreach ($processEntry in $processes) {
                if ([string]$processEntry.name -ieq "Unity.exe" -and
                    (Get-ProcessIdentityKey -Identity $processEntry) -cne (Get-ProcessIdentityKey -Identity $MainProcessIdentity)) {
                    $unrelatedByKey[(Get-ProcessIdentityKey -Identity $processEntry)] = $processEntry
                }
            }
        }

        $mainPresent = $processQuerySucceeded -and (Test-ProcessIdentityPresent -Identity $MainProcessIdentity -Processes $processes)
        if ($processQuerySucceeded) {
            if ($mainPresent) {
                $mainLastSeenAtUtc = $nowUtc
                $mainLastSeenAtUnix = $nowUnix
            }
            elseif ($null -eq $mainFirstAbsentAtUnix) {
                $mainFirstAbsentAtUtc = $nowUtc
                $mainFirstAbsentAtUnix = $nowUnix
            }
        }

        $presentChildCount = 0
        foreach ($key in @($trackedByKey.Keys)) {
            $present = $processQuerySucceeded -and (Test-ProcessIdentityPresent -Identity $trackedByKey[$key] -Processes $processes)
            if ($processQuerySucceeded) {
                if ($present) {
                    $presentChildCount++
                    $childEvidenceByKey[$key].lastSeenAtUtc = $nowUtc
                    $childEvidenceByKey[$key].lastSeenAtUnixMilliseconds = $nowUnix
                }
                elseif ($null -eq $childEvidenceByKey[$key].firstAbsentAtUnixMilliseconds) {
                    $childEvidenceByKey[$key].firstAbsentAtUtc = $nowUtc
                    $childEvidenceByKey[$key].firstAbsentAtUnixMilliseconds = $nowUnix
                }
            }
        }

        $lockPresent = $lockQuerySucceeded -and [bool]$observation.lockfileExists
        if ($lockQuerySucceeded) {
            if ($lockPresent) {
                $lockLastSeenAtUtc = $nowUtc
                $lockLastSeenAtUnix = $nowUnix
            }
            elseif ($null -eq $lockFirstAbsentAtUnix) {
                $lockFirstAbsentAtUtc = $nowUtc
                $lockFirstAbsentAtUnix = $nowUnix
            }
        }

        $guardSeen = $mainPresent -or $presentChildCount -gt 0 -or $lockPresent
        if ($guardSeen) {
            $lastSeenAtUtc = $nowUtc
            $lastSeenAtUnix = $nowUnix
        }
        $allAbsent = $processQuerySucceeded -and $lockQuerySucceeded -and
            -not $mainPresent -and $presentChildCount -eq 0 -and -not $lockPresent
        if ($allAbsent) {
            if ($null -eq $firstAbsentAtUnix) {
                $firstAbsentAtUtc = $nowUtc
                $firstAbsentAtUnix = $nowUnix
            }
            $consecutiveAbsent++
            if ($consecutiveAbsent -ge $RequiredConsecutiveAbsent) {
                $achievedAtUtc = $nowUtc
                $achievedAtUnix = $nowUnix
                break
            }
        }
        else {
            $consecutiveAbsent = 0
        }

        $elapsed = $nowUnix - [long]$startedAt.ToUnixTimeMilliseconds()
        if ($elapsed -ge $TimeoutMilliseconds -or $pollCount -ge $maximumPolls) {
            $timedOut = $true
            break
        }
        & $SleepAction $PollIntervalMilliseconds
    }

    $finishedAt = [DateTimeOffset]::UtcNow
    return [pscustomobject][ordered]@{
        status = if ($timedOut) { "TimedOut" } else { "Succeeded" }
        succeeded = -not $timedOut
        timedOut = $timedOut
        timeoutMilliseconds = $TimeoutMilliseconds
        pollIntervalMilliseconds = $PollIntervalMilliseconds
        requiredConsecutiveAbsent = $RequiredConsecutiveAbsent
        pollCount = $pollCount
        startedAtUtc = $startedAt.ToString("o")
        startedAtUnixMilliseconds = [long]$startedAt.ToUnixTimeMilliseconds()
        finishedAtUtc = $finishedAt.ToString("o")
        finishedAtUnixMilliseconds = [long]$finishedAt.ToUnixTimeMilliseconds()
        lastSeenAtUtc = $lastSeenAtUtc
        lastSeenAtUnixMilliseconds = $lastSeenAtUnix
        firstAbsentAtUtc = $firstAbsentAtUtc
        firstAbsentAtUnixMilliseconds = $firstAbsentAtUnix
        quietPeriodAchievedAtUtc = $achievedAtUtc
        quietPeriodAchievedAtUnixMilliseconds = $achievedAtUnix
        mainProcess = [ordered]@{
            identity = $MainProcessIdentity
            lastSeenAtUtc = $mainLastSeenAtUtc
            lastSeenAtUnixMilliseconds = $mainLastSeenAtUnix
            firstAbsentAtUtc = $mainFirstAbsentAtUtc
            firstAbsentAtUnixMilliseconds = $mainFirstAbsentAtUnix
        }
        trackedChildren = @($childEvidenceByKey.Values | Sort-Object { $_.identity.processId }, { $_.identity.creationTimeUnixMilliseconds })
        lockfile = [ordered]@{
            path = $LockPath
            lastSeenAtUtc = $lockLastSeenAtUtc
            lastSeenAtUnixMilliseconds = $lockLastSeenAtUnix
            firstAbsentAtUtc = $lockFirstAbsentAtUtc
            firstAbsentAtUnixMilliseconds = $lockFirstAbsentAtUnix
        }
        unrelatedUnityProcessesObserved = @($unrelatedByKey.Values | Sort-Object processId, creationTimeUnixMilliseconds)
        queryErrors = @($queryErrors)
    }
}

function Get-FileLengthTotal {
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$Files
    )

    $total = [long]0
    foreach ($file in $Files) {
        $length = [Convert]::ToInt64($file.Length)
        if ($length -lt 0 -or [long]::MaxValue - $total -lt $length) {
            throw "Invalid or overflowing file length: $length"
        }
        $total += $length
    }
    return $total
}

function Assert-NoChildReparsePoints {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$Label
    )

    $reparsePoints = @(Get-ChildItem -LiteralPath $Root -Recurse -Force |
        Where-Object { ($_.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0 })
    if ($reparsePoints.Count -gt 0) {
        throw "$Label contains reparse points: $(@($reparsePoints.FullName) -join ', ')"
    }
}

function Get-PartialOutputEvidence {
    param([Parameter(Mandatory)][string]$OutputDirectory)

    if (-not (Test-Path -LiteralPath $OutputDirectory -PathType Container)) {
        return [ordered]@{
            exists = $false
            path = $OutputDirectory
            fileCount = 0
            totalBytes = 0
            files = @()
            topLevelEntries = @()
        }
    }
    Assert-NoReparsePointInPath -Path $OutputDirectory -Label "Partial output"
    Assert-NoChildReparsePoints -Root $OutputDirectory -Label "Partial output"
    $files = @(Get-ChildItem -LiteralPath $OutputDirectory -Recurse -File -Force | Sort-Object FullName)
    $fileEvidence = @($files | ForEach-Object {
        [pscustomobject][ordered]@{
            path = $_.FullName
            relativePath = [IO.Path]::GetRelativePath($OutputDirectory, $_.FullName).Replace("\", "/")
            length = [long]$_.Length
            sha256 = Get-FileSha256 -Path $_.FullName
        }
    })
    return [ordered]@{
        exists = $true
        path = $OutputDirectory
        fileCount = $files.Count
        totalBytes = Get-FileLengthTotal -Files $files
        files = $fileEvidence
        topLevelEntries = @(Get-ChildItem -LiteralPath $OutputDirectory -Force |
            Sort-Object Name |
            ForEach-Object { $_.Name })
    }
}

function Assert-NonEmptyFile {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Label
    )

    Assert-NoReparsePointInPath -Path $Path -Label $Label
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Label is missing: $Path"
    }
    $file = Get-Item -LiteralPath $Path -Force
    if ($file.Length -le 0) {
        throw "$Label is empty: $Path"
    }
    return [ordered]@{
        label = $Label
        path = $file.FullName
        length = [long]$file.Length
        sha256 = Get-FileSha256 -Path $file.FullName
    }
}

function Get-Amd64PeEvidence {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Label
    )

    $fileEvidence = Assert-NonEmptyFile -Path $Path -Label $Label
    $stream = [IO.File]::Open(
        $Path,
        [IO.FileMode]::Open,
        [IO.FileAccess]::Read,
        ([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
    $reader = [IO.BinaryReader]::new($stream)
    try {
        if ($reader.ReadUInt16() -ne 0x5A4D) { throw "$Label has no MZ header: $Path" }
        $stream.Position = 0x3C
        $peOffset = $reader.ReadInt32()
        if ($peOffset -lt 0 -or $peOffset + 6 -gt $stream.Length) {
            throw "$Label has an invalid PE offset: $Path"
        }
        $stream.Position = $peOffset
        if ($reader.ReadUInt32() -ne 0x00004550) { throw "$Label has no PE signature: $Path" }
        $machine = $reader.ReadUInt16()
        if ($machine -ne 0x8664) {
            throw ("$Label is not AMD64. PE machine=0x{0:X4}: {1}" -f $machine, $Path)
        }
        return [ordered]@{
            label = $Label
            path = $fileEvidence.path
            length = $fileEvidence.length
            sha256 = $fileEvidence.sha256
            machine = ("0x{0:X4}" -f $machine)
            architecture = "AMD64"
        }
    }
    finally {
        $reader.Dispose()
        $stream.Dispose()
    }
}

function Assert-FreshArtifactFileUnix {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Label,
        [Parameter(Mandatory)]$WindowStartUnixMilliseconds,
        [Parameter(Mandatory)]$WindowEndUnixMilliseconds,
        [long]$ToleranceMilliseconds = 0
    )

    $fileEvidence = Assert-NonEmptyFile -Path $Path -Label $Label
    $start = ConvertTo-Int64Strict -Value $WindowStartUnixMilliseconds -Label "$Label window start"
    $end = ConvertTo-Int64Strict -Value $WindowEndUnixMilliseconds -Label "$Label window end"
    $file = Get-Item -LiteralPath $Path -Force
    $lastWriteUnix = [long]([DateTimeOffset]$file.LastWriteTimeUtc).ToUnixTimeMilliseconds()
    if ($lastWriteUnix -lt ($start - $ToleranceMilliseconds) -or
        $lastWriteUnix -gt ($end + $ToleranceMilliseconds)) {
        throw "$Label was not written in the authoritative Unix-ms window: $Path"
    }
    return [ordered]@{
        path = $fileEvidence.path
        length = $fileEvidence.length
        sha256 = $fileEvidence.sha256
        lastWriteTimeUtc = $file.LastWriteTimeUtc.ToString("o")
        lastWriteUnixMilliseconds = $lastWriteUnix
        windowStartUnixMilliseconds = $start
        windowEndUnixMilliseconds = $end
        toleranceMilliseconds = $ToleranceMilliseconds
    }
}

function Get-OutputAllowlistEvaluation {
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][string[]]$ReportedRelativePaths,
        [Parameter(Mandatory)][AllowEmptyCollection()][string[]]$ActualRelativePaths,
        [Parameter(Mandatory)][string]$AllowedExtraFile
    )

    $reported = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($path in $ReportedRelativePaths) { [void]$reported.Add($path.Replace("\", "/")) }
    $actual = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($path in $ActualRelativePaths) { [void]$actual.Add($path.Replace("\", "/")) }
    $missing = @($reported | Where-Object { -not $actual.Contains($_) } | Sort-Object)
    $extra = @($actual | Where-Object { -not $reported.Contains($_) } | Sort-Object)
    $allowed = @($extra | Where-Object {
        [string]::Equals($_, $AllowedExtraFile, [StringComparison]::OrdinalIgnoreCase)
    })
    $disallowed = @($extra | Where-Object {
        -not [string]::Equals($_, $AllowedExtraFile, [StringComparison]::OrdinalIgnoreCase)
    })
    return [ordered]@{
        missingReportedFiles = $missing
        extraFiles = $extra
        allowedExtraFiles = $allowed
        disallowedExtraFiles = $disallowed
    }
}

function Assert-Windows64Output {
    param(
        [Parameter(Mandatory)][string]$OutputDirectory,
        [Parameter(Mandatory)][string]$PlayerPath,
        [Parameter(Mandatory)][string]$ScriptingBackend,
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$BuildFiles
    )

    if (-not (Test-Path -LiteralPath $OutputDirectory -PathType Container)) {
        throw "Windows64 output directory is missing: $OutputDirectory"
    }
    Assert-NoReparsePointInPath -Path $OutputDirectory -Label "Windows64 output"
    Assert-NoChildReparsePoints -Root $OutputDirectory -Label "Windows64 output"

    $requiredArtifacts = @()
    $peArtifacts = @()
    $playerEvidence = Get-Amd64PeEvidence -Path $PlayerPath -Label "Windows64 Player"
    $requiredArtifacts += $playerEvidence
    $peArtifacts += $playerEvidence
    $unityPlayerEvidence = Get-Amd64PeEvidence -Path (Join-Path $OutputDirectory "UnityPlayer.dll") -Label "UnityPlayer.dll"
    $requiredArtifacts += $unityPlayerEvidence
    $peArtifacts += $unityPlayerEvidence
    $requiredArtifacts += Assert-NonEmptyFile -Path (Join-Path $OutputDirectory "UnityCrashHandler64.exe") -Label "UnityCrashHandler64.exe"

    $dataDirectory = Join-Path $OutputDirectory "ElementWar_Data"
    if (-not (Test-Path -LiteralPath $dataDirectory -PathType Container)) {
        throw "ElementWar_Data directory is missing: $dataDirectory"
    }
    $dataFiles = @(Get-ChildItem -LiteralPath $dataDirectory -Recurse -File -Force)
    if ($dataFiles.Count -le 0) { throw "ElementWar_Data contains no files: $dataDirectory" }
    $requiredArtifacts += Assert-NonEmptyFile -Path (Join-Path $dataDirectory "globalgamemanagers") -Label "ElementWar_Data/globalgamemanagers"

    switch -CaseSensitive ($ScriptingBackend) {
        "Mono2x" {
            $requiredArtifacts += Assert-NonEmptyFile -Path (Join-Path $OutputDirectory "MonoBleedingEdge\EmbedRuntime\mono-2.0-bdwgc.dll") -Label "Mono runtime"
            $requiredArtifacts += Assert-NonEmptyFile -Path (Join-Path $dataDirectory "Managed\Assembly-CSharp.dll") -Label "Assembly-CSharp.dll"
        }
        "IL2CPP" {
            $gameAssembly = Get-Amd64PeEvidence -Path (Join-Path $OutputDirectory "GameAssembly.dll") -Label "GameAssembly.dll"
            $requiredArtifacts += $gameAssembly
            $peArtifacts += $gameAssembly
            $requiredArtifacts += Assert-NonEmptyFile -Path (Join-Path $dataDirectory "il2cpp_data\Metadata\global-metadata.dat") -Label "IL2CPP global metadata"
        }
        default {
            throw "Unsupported Standalone scripting backend: $ScriptingBackend"
        }
    }
    if ($BuildFiles.Count -le 0) { throw "C# BuildReport contains no output files." }

    $reportedSet = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $manifestChecks = foreach ($buildFile in $BuildFiles) {
        $manifestPath = Resolve-NormalizedPath -Path ([string]$buildFile.path)
        if (-not (Test-IsPathWithinOrEqualRoot -Path $manifestPath -Root $OutputDirectory) -or
            [string]::Equals($manifestPath, $OutputDirectory, [StringComparison]::OrdinalIgnoreCase)) {
            throw "BuildReport file is outside output: $manifestPath"
        }
        Assert-NoReparsePointInPath -Path $manifestPath -Label "BuildReport output file"
        if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
            throw "BuildReport file is missing: $manifestPath"
        }
        if (-not $reportedSet.Add($manifestPath)) {
            throw "BuildReport contains duplicate file: $manifestPath"
        }
        $actualLength = [long](Get-Item -LiteralPath $manifestPath -Force).Length
        $reportedLength = ConvertTo-Int64Strict -Value $buildFile.sizeBytes -Label "BuildReport size for $manifestPath"
        if ($actualLength -ne $reportedLength) {
            throw "BuildReport size mismatch for '$manifestPath'. Reported=$reportedLength actual=$actualLength"
        }
        [pscustomobject][ordered]@{
            path = $manifestPath
            relativePath = [IO.Path]::GetRelativePath($OutputDirectory, $manifestPath).Replace("\", "/")
            role = [string]$buildFile.role
            sizeBytes = $actualLength
            sha256 = Get-FileSha256 -Path $manifestPath
        }
    }

    $outputFiles = @(Get-ChildItem -LiteralPath $OutputDirectory -Recurse -File -Force | Sort-Object FullName)
    $actualFiles = @($outputFiles | ForEach-Object {
        [pscustomobject][ordered]@{
            path = $_.FullName
            relativePath = [IO.Path]::GetRelativePath($OutputDirectory, $_.FullName).Replace("\", "/")
            length = [long]$_.Length
            sha256 = Get-FileSha256 -Path $_.FullName
        }
    })
    $allowlist = Get-OutputAllowlistEvaluation -ReportedRelativePaths @($manifestChecks.relativePath) -ActualRelativePaths @($actualFiles.relativePath) -AllowedExtraFile $allowedBurstDebugRelativePath
    if ($allowlist.missingReportedFiles.Count -gt 0) {
        throw "BuildReport files are missing: $($allowlist.missingReportedFiles -join ', ')"
    }
    if ($allowlist.disallowedExtraFiles.Count -gt 0) {
        throw "Unapproved extra output files: $($allowlist.disallowedExtraFiles -join ', ')"
    }
    if ($allowlist.allowedExtraFiles.Count -gt 1) {
        throw "More than one approved Burst debug file was found."
    }
    $allowedBurstEvidence = $null
    if ($allowlist.allowedExtraFiles.Count -eq 1) {
        $allowedBurstPath = Join-Path $OutputDirectory $allowedBurstDebugRelativePath
        $allowedBurstEvidence = Assert-NonEmptyFile -Path $allowedBurstPath -Label "Approved Burst debug file"
    }

    $allowedDirectories = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($manifestFile in $manifestChecks) {
        $parent = [IO.Path]::GetDirectoryName($manifestFile.path)
        while (-not [string]::Equals($parent, $OutputDirectory, [StringComparison]::OrdinalIgnoreCase)) {
            [void]$allowedDirectories.Add($parent)
            $parent = [IO.Path]::GetDirectoryName($parent)
            if ([string]::IsNullOrWhiteSpace($parent)) {
                throw "BuildReport directory escaped output: $($manifestFile.path)"
            }
        }
    }
    if ($null -ne $allowedBurstEvidence) {
        $allowedBurstParent = [IO.Path]::GetDirectoryName((Join-Path $OutputDirectory $allowedBurstDebugRelativePath))
        while (-not [string]::Equals($allowedBurstParent, $OutputDirectory, [StringComparison]::OrdinalIgnoreCase)) {
            [void]$allowedDirectories.Add($allowedBurstParent)
            $allowedBurstParent = [IO.Path]::GetDirectoryName($allowedBurstParent)
            if ([string]::IsNullOrWhiteSpace($allowedBurstParent)) {
                throw "Approved Burst debug path escaped output."
            }
        }
    }
    $unexpectedDirectories = @(Get-ChildItem -LiteralPath $OutputDirectory -Recurse -Directory -Force |
        Where-Object {
            -not $allowedDirectories.Contains($_.FullName)
        } |
        ForEach-Object { [IO.Path]::GetRelativePath($OutputDirectory, $_.FullName).Replace("\", "/") })
    if ($unexpectedDirectories.Count -gt 0) {
        throw "Unapproved extra output directories: $($unexpectedDirectories -join ', ')"
    }

    $totalBytes = Get-FileLengthTotal -Files $outputFiles
    if ($outputFiles.Count -le 0 -or $totalBytes -le 0) {
        throw "Windows64 output is empty: $OutputDirectory"
    }
    $gameEditorDllPath = Join-Path $dataDirectory "Managed\Game.Editor.dll"
    $gameEditorDebt = if (Test-Path -LiteralPath $gameEditorDllPath -PathType Leaf) {
        Assert-NonEmptyFile -Path $gameEditorDllPath -Label "Game.Editor.dll architecture debt"
    }
    else {
        $null
    }

    return [ordered]@{
        outputDirectory = $OutputDirectory
        player = $playerEvidence
        dataDirectory = [ordered]@{
            path = $dataDirectory
            fileCount = $dataFiles.Count
            totalBytes = Get-FileLengthTotal -Files $dataFiles
        }
        requiredArtifacts = $requiredArtifacts
        peArtifacts = $peArtifacts
        buildReportFileCount = $manifestChecks.Count
        buildReportFiles = @($manifestChecks)
        actualFileCount = $outputFiles.Count
        actualTotalBytes = $totalBytes
        actualFiles = $actualFiles
        allowedExtraFile = $allowedBurstDebugRelativePath
        allowedExtraFiles = $allowlist.allowedExtraFiles
        allowedBurstDebugFile = $allowedBurstEvidence
        unexpectedDirectories = $unexpectedDirectories
        gameEditorAssemblyDebt = $gameEditorDebt
        topLevelEntries = @(Get-ChildItem -LiteralPath $OutputDirectory -Force |
            Sort-Object Name |
            ForEach-Object { $_.Name })
    }
}

function Get-UnityLogDiagnostics {
    param([Parameter(Mandatory)][string]$LogPath)

    if (-not (Test-Path -LiteralPath $LogPath -PathType Leaf)) { return $null }
    $lines = @(Get-Content -LiteralPath $LogPath)
    $compilerWarnings = @($lines | Where-Object { $_ -match "(?i)\bwarning\s+CS[0-9]{4}\b" })
    $compilerErrors = @($lines | Where-Object { $_ -match "(?i)\berror\s+CS[0-9]{4}\b" })
    $warningTokenLines = @($lines | Where-Object { $_ -match "(?i)\bwarning\b" })
    $errorTokenLines = @($lines | Where-Object { $_ -match "(?i)\berror\b" })
    $licenseErrorLines = @($errorTokenLines | Where-Object { $_ -match "^\[Licensing::" })
    $fatalErrorLines = @($lines | Where-Object {
        $_ -notmatch "^\[Licensing::" -and (
            $_ -match "(?i)\berror\s+CS[0-9]{4}\b" -or
            $_ -match "(?i)^Aborting batchmode" -or
            $_ -match "(?i)^Unhandled exception" -or
            $_ -match "(?i)^Error building Player" -or
            $_ -match "(?i)^Scripts have compiler errors" -or
            $_ -match "(?i)BuildFailedException" -or
            $_ -match "(?i)executeMethod.*(failed|exception)"
        )
    })
    return [ordered]@{
        lineCount = $lines.Count
        warningTokenLineCount = $warningTokenLines.Count
        errorTokenLineCount = $errorTokenLines.Count
        compilerWarningOccurrenceCount = $compilerWarnings.Count
        compilerWarningUniqueCount = @($compilerWarnings | Sort-Object -Unique).Count
        compilerWarningLines = $compilerWarnings
        compilerErrorOccurrenceCount = $compilerErrors.Count
        compilerErrorUniqueCount = @($compilerErrors | Sort-Object -Unique).Count
        compilerErrorLines = $compilerErrors
        licenseErrorLineCount = $licenseErrorLines.Count
        licenseErrorLines = $licenseErrorLines
        fatalErrorLineCount = $fatalErrorLines.Count
        fatalErrorLines = $fatalErrorLines
    }
}

function Add-Failure {
    param(
        [Parameter(Mandatory)]$Summary,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [Collections.Generic.List[string]]$FailureMessages,
        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string]$Message
    )

    $stableMessage = if ([string]::IsNullOrWhiteSpace($Message)) {
        "Unspecified Windows64 verification failure."
    }
    else {
        $Message.Trim()
    }
    if (-not $FailureMessages.Contains($stableMessage)) {
        [void]$FailureMessages.Add($stableMessage)
    }
    $Summary.result = "Failed"
    $Summary.error = @($FailureMessages) -join " "
}

function Add-FailureSafely {
    param(
        [Parameter(Mandatory)]$Summary,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [Collections.Generic.List[string]]$FailureMessages,
        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string]$Message
    )

    try {
        Add-Failure -Summary $Summary -FailureMessages $FailureMessages -Message $Message
    }
    catch {
        $fallback = "Failure recording itself failed: $($_.Exception.Message)"
        try {
            if (-not $FailureMessages.Contains($fallback)) {
                [void]$FailureMessages.Add($fallback)
            }
        }
        catch {
        }
        try {
            $Summary.result = "Failed"
            $Summary.error = @($FailureMessages) -join " "
        }
        catch {
        }
    }
}

function Write-FinalSummarySafely {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)]$Summary,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [Collections.Generic.List[string]]$FailureMessages,
        [scriptblock]$PrimaryWriter
    )

    $result = [ordered]@{
        written = $false
        path = $Path
        usedFailSafe = $false
        primaryError = $null
        failSafeError = $null
    }
    try {
        if ($null -eq $PrimaryWriter) {
            $PrimaryWriter = {
                param($SummaryPath, $SummaryValue)
                Write-NewJsonFile -Path $SummaryPath -Value $SummaryValue -Depth 40
            }
        }
        & $PrimaryWriter $Path $Summary
        $result.written = Test-Path -LiteralPath $Path -PathType Leaf
        return [pscustomobject]$result
    }
    catch {
        $result.primaryError = $_.Exception.Message
        Add-FailureSafely -Summary $Summary -FailureMessages $FailureMessages -Message "Primary PowerShell summary writer failed: $($result.primaryError)"
    }

    $result.usedFailSafe = $true
    try {
        $parent = [IO.Path]::GetDirectoryName((Resolve-NormalizedPath -Path $Path))
        if ([string]::IsNullOrWhiteSpace($parent) -or
            -not (Test-Path -LiteralPath $parent -PathType Container)) {
            throw "Fail-safe summary parent does not exist: $parent"
        }
        $destination = if (Test-Path -LiteralPath $Path -PathType Leaf) {
            Join-Path $parent "Windows64-verification-summary-failsafe.json"
        }
        else {
            $Path
        }
        if (Test-Path -LiteralPath $destination) {
            throw "Fail-safe summary collision: $destination"
        }
        $temporaryPath = "$destination.tmp-$PID-$([Guid]::NewGuid().ToString('N'))"
        try {
            $json = $Summary | ConvertTo-Json -Depth 40
            [IO.File]::WriteAllText($temporaryPath, $json, [Text.UTF8Encoding]::new($false))
            Move-Item -LiteralPath $temporaryPath -Destination $destination
        }
        finally {
            if (Test-Path -LiteralPath $temporaryPath -PathType Leaf) {
                Remove-Item -LiteralPath $temporaryPath -Force
            }
        }
        $result.path = $destination
        $result.written = Test-Path -LiteralPath $destination -PathType Leaf
        return [pscustomobject]$result
    }
    catch {
        $result.failSafeError = $_.Exception.Message
        Add-FailureSafely -Summary $Summary -FailureMessages $FailureMessages -Message "Fail-safe PowerShell summary writer failed: $($result.failSafeError)"
    }

    try {
        $parent = [IO.Path]::GetDirectoryName((Resolve-NormalizedPath -Path $Path))
        $minimalPath = Join-Path $parent "Windows64-verification-summary-minimal.json"
        if (-not (Test-Path -LiteralPath $minimalPath)) {
            $minimalRunId = $null
            try { $minimalRunId = [string]$Summary.runId } catch { }
            $minimal = [ordered]@{
                schemaVersion = 2
                runId = $minimalRunId
                result = "Failed"
                error = @($FailureMessages) -join " "
                failureMessages = @($FailureMessages)
                fullSummaryPath = $Path
                primaryWriterError = $result.primaryError
                failSafeWriterError = $result.failSafeError
                writtenAtUtc = [DateTimeOffset]::UtcNow.ToString("o")
            }
            [IO.File]::WriteAllText(
                $minimalPath,
                ($minimal | ConvertTo-Json -Depth 8),
                [Text.UTF8Encoding]::new($false))
            $result.path = $minimalPath
            $result.written = $true
        }
    }
    catch {
    }
    return [pscustomobject]$result
}

function Invoke-OfflineTestCase {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][scriptblock]$Action,
        [Parameter(Mandatory)][AllowEmptyCollection()][Collections.Generic.List[object]]$Results
    )

    $started = [DateTimeOffset]::UtcNow
    try {
        [void](& $Action)
        [void]$Results.Add([pscustomobject][ordered]@{
            name = $Name
            result = "PASS"
            error = $null
            durationMilliseconds = [long]([DateTimeOffset]::UtcNow - $started).TotalMilliseconds
        })
    }
    catch {
        [void]$Results.Add([pscustomobject][ordered]@{
            name = $Name
            result = "FAIL"
            error = $_.Exception.Message
            durationMilliseconds = [long]([DateTimeOffset]::UtcNow - $started).TotalMilliseconds
        })
    }
}

function New-ProbeShutdownObservation {
    param(
        [Parameter(Mandatory)][DateTimeOffset]$At,
        [Parameter(Mandatory)][AllowNull()][AllowEmptyCollection()][object[]]$Processes,
        [Parameter(Mandatory)][bool]$LockfileExists
    )

    return [pscustomobject][ordered]@{
        capturedAtUtc = $At.ToUniversalTime().ToString("o")
        capturedAtUnixMilliseconds = [long]$At.ToUniversalTime().ToUnixTimeMilliseconds()
        processQuerySucceeded = $true
        processQueryError = $null
        processes = if ($null -eq $Processes) { [object[]]::new(0) } else { @($Processes) }
        lockQuerySucceeded = $true
        lockQueryError = $null
        lockfileExists = $LockfileExists
    }
}

function Invoke-StaticProbes {
    param([Parameter(Mandatory)][string]$RepositoryPath)

    $tempRoot = Resolve-NormalizedPath -Path ([IO.Path]::GetTempPath())
    Assert-NoReparsePointInPath -Path $tempRoot -Label "System temporary directory"
    $probeDirectory = Join-Path $tempRoot "ElementWarWindows64Probe-$([Guid]::NewGuid().ToString('N'))"
    if (-not (Test-IsPathWithinOrEqualRoot -Path $probeDirectory -Root $tempRoot)) {
        throw "Probe directory escaped the system temporary root: $probeDirectory"
    }
    [void](New-ExactDirectory -Path $probeDirectory -Label "Offline probe directory")
    $tests = [Collections.Generic.List[object]]::new()

    try {
        Invoke-OfflineTestCase -Name "Unix time ordering and ISO cross-check" -Results $tests -Action {
            $fixed = [DateTimeOffset]::Parse("2026-08-06T08:00:00.123+00:00", [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::RoundtripKind)
            $fixedUnix = [long]$fixed.ToUnixTimeMilliseconds()
            [void](Assert-IsoMatchesUnixMilliseconds -IsoValue $fixed.ToString("o") -UnixMilliseconds $fixedUnix -Label "Probe fixed time")
            [void](Assert-UnixMillisecondOrder -OrderedValues ([ordered]@{ entryStart=$fixedUnix; pipelineStart=$fixedUnix+1; pipelineEnd=$fixedUnix+2; entryEnd=$fixedUnix+3 }) -Label "Probe ordered time")
            $isoRejected = $false
            try { [void](Assert-IsoMatchesUnixMilliseconds -IsoValue $fixed.ToString("o") -UnixMilliseconds ($fixedUnix+2) -Label "Probe mismatch") } catch { $isoRejected = $true }
            $orderRejected = $false
            try { [void](Assert-UnixMillisecondOrder -OrderedValues ([ordered]@{ start=$fixedUnix+1; end=$fixedUnix }) -Label "Probe reversed") } catch { $orderRejected = $true }
            if (-not $isoRejected -or -not $orderRejected) { throw "Invalid Unix/ISO evidence was not rejected." }
        }

        Invoke-OfflineTestCase -Name "Tracked and untracked repository state comparison" -Results $tests -Action {
            $before = @(
                [pscustomobject]@{ status=" M"; path="tracked.txt"; state="File"; length=1; sha256="aa" },
                [pscustomobject]@{ status="??"; path="untracked.txt"; state="File"; length=2; sha256="bb" })
            $equal = @($before | ForEach-Object { $_.PSObject.Copy() })
            if (@(Compare-RepositoryStateSnapshots -Before $before -After $equal).Count -ne 0) { throw "Equal repository states differed." }
            $changed = @($before[0].PSObject.Copy(), [pscustomobject]@{ status="??"; path="untracked.txt"; state="File"; length=3; sha256="cc" })
            $delta = @(Compare-RepositoryStateSnapshots -Before $before -After $changed)
            if ($delta.Count -ne 1 -or $delta[0].path -cne "untracked.txt") { throw "Untracked byte change was missed." }
        }

        Invoke-OfflineTestCase -Name "Add-Failure accepts the first empty list" -Results $tests -Action {
            $summaryProbe = [ordered]@{ result="Running"; error=$null }
            $messages = [Collections.Generic.List[string]]::new()
            Add-Failure -Summary $summaryProbe -FailureMessages $messages -Message "first"
            if ($messages.Count -ne 1 -or $messages[0] -cne "first" -or $summaryProbe.result -cne "Failed") { throw "First failure was not recorded." }
        }

        Invoke-OfflineTestCase -Name "Add-Failure records single duplicate second and empty fallback" -Results $tests -Action {
            $summaryProbe = [ordered]@{ result="Running"; error=$null }
            $messages = [Collections.Generic.List[string]]::new()
            Add-Failure -Summary $summaryProbe -FailureMessages $messages -Message "first"
            Add-Failure -Summary $summaryProbe -FailureMessages $messages -Message "first"
            Add-Failure -Summary $summaryProbe -FailureMessages $messages -Message "second"
            Add-Failure -Summary $summaryProbe -FailureMessages $messages -Message ""
            if ($messages.Count -ne 3 -or $messages[0] -cne "first" -or $messages[1] -cne "second" -or $messages[2] -cne "Unspecified Windows64 verification failure.") { throw "Failure de-duplication/order/fallback is unstable." }
        }

        Invoke-OfflineTestCase -Name "Original and finally failures survive fail-safe summary" -Results $tests -Action {
            $caseDirectory = New-ExactDirectory -Path (Join-Path $probeDirectory "summary-failsafe") -Label "Summary fail-safe probe"
            $summaryPathProbe = Join-Path $caseDirectory "Windows64-verification-summary.json"
            $summaryProbe = [ordered]@{ schemaVersion=2; runId="offline"; result="Running"; error=$null }
            $messages = [Collections.Generic.List[string]]::new()
            Add-Failure -Summary $summaryProbe -FailureMessages $messages -Message "Primary build failure."
            Add-Failure -Summary $summaryProbe -FailureMessages $messages -Message "Final Git check failure."
            $write = Write-FinalSummarySafely -Path $summaryPathProbe -Summary $summaryProbe -FailureMessages $messages -PrimaryWriter { throw "Injected primary writer failure." }
            if (-not $write.written) { throw "Fail-safe summary was not written." }
            $parsed = Get-Content -Raw -LiteralPath $write.path | ConvertFrom-Json -DateKind String
            if ([string]$parsed.error -notmatch [regex]::Escape("Primary build failure.") -or [string]$parsed.error -notmatch [regex]::Escape("Final Git check failure.")) { throw "Final summary lost primary or finally error." }
        }

        $baseTime = [DateTimeOffset]::UtcNow
        $mainIdentity = [pscustomobject]@{ processId=41001; parentProcessId=1; creationTimeUtc=$baseTime.AddMinutes(-1).ToString("o"); creationTimeUnixMilliseconds=[long]$baseTime.AddMinutes(-1).ToUnixTimeMilliseconds(); name="Unity.exe"; executablePath="C:\Mock\Unity.exe" }

        Invoke-OfflineTestCase -Name "Lockfile disappears on third poll and quiet period succeeds" -Results $tests -Action {
            $queue = [Collections.Generic.Queue[object]]::new()
            for ($i=0; $i -lt 5; $i++) { $queue.Enqueue((New-ProbeShutdownObservation -At $baseTime.AddMilliseconds($i*250) -Processes @() -LockfileExists ($i -lt 2))) }
            $provider = { $queue.Dequeue() }
            $quiet = Wait-UnityShutdownQuietPeriod -MainProcessIdentity $mainIdentity -TrackedChildIdentities @() -LockPath "C:\Mock\UnityLockfile" -ObservationProvider $provider -SleepAction { param($Milliseconds) }
            if (-not $quiet.succeeded -or $quiet.pollCount -ne 5 -or $null -eq $quiet.lockfile.lastSeenAtUnixMilliseconds -or $null -eq $quiet.quietPeriodAchievedAtUnixMilliseconds) { throw "Lockfile quiet-period evidence is incorrect." }
        }

        Invoke-OfflineTestCase -Name "Tracked child exits later and quiet period succeeds" -Results $tests -Action {
            $child = [pscustomobject]@{ processId=41002; parentProcessId=41001; creationTimeUtc=$baseTime.AddSeconds(-30).ToString("o"); creationTimeUnixMilliseconds=[long]$baseTime.AddSeconds(-30).ToUnixTimeMilliseconds(); name="UnityShaderCompiler.exe"; executablePath="C:\Mock\Child.exe" }
            $queue = [Collections.Generic.Queue[object]]::new()
            for ($i=0; $i -lt 5; $i++) { $queue.Enqueue((New-ProbeShutdownObservation -At $baseTime.AddMilliseconds($i*250) -Processes $(if($i -lt 2){@($child)}else{@()}) -LockfileExists $false)) }
            $provider = { $queue.Dequeue() }
            $quiet = Wait-UnityShutdownQuietPeriod -MainProcessIdentity $mainIdentity -TrackedChildIdentities @($child) -LockPath "C:\Mock\UnityLockfile" -ObservationProvider $provider -SleepAction { param($Milliseconds) }
            if (-not $quiet.succeeded -or $quiet.pollCount -ne 5 -or $null -eq $quiet.trackedChildren[0].lastSeenAtUnixMilliseconds) { throw "Child-process quiet-period evidence is incorrect." }
        }

        Invoke-OfflineTestCase -Name "Unrelated Unity process is diagnostic only" -Results $tests -Action {
            $otherUnity = [pscustomobject]@{ processId=41999; parentProcessId=1; creationTimeUtc=$baseTime.AddSeconds(-10).ToString("o"); creationTimeUnixMilliseconds=[long]$baseTime.AddSeconds(-10).ToUnixTimeMilliseconds(); name="Unity.exe"; executablePath="D:\Other\Unity.exe" }
            $queue = [Collections.Generic.Queue[object]]::new()
            for ($i=0; $i -lt 3; $i++) { $queue.Enqueue((New-ProbeShutdownObservation -At $baseTime.AddMilliseconds($i*250) -Processes @($otherUnity) -LockfileExists $false)) }
            $provider = { $queue.Dequeue() }
            $quiet = Wait-UnityShutdownQuietPeriod -MainProcessIdentity $mainIdentity -TrackedChildIdentities @() -LockPath "C:\Mock\UnityLockfile" -ObservationProvider $provider -SleepAction { param($Milliseconds) }
            if (-not $quiet.succeeded -or @($quiet.unrelatedUnityProcessesObserved).Count -ne 1) { throw "Unrelated Unity incorrectly blocked or was not recorded." }
        }

        Invoke-OfflineTestCase -Name "Quiet timeout skips CAS and writes Failed summary" -Results $tests -Action {
            $caseDirectory = New-ExactDirectory -Path (Join-Path $probeDirectory "quiet-timeout") -Label "Quiet-timeout probe"
            $queue = [Collections.Generic.Queue[object]]::new()
            for ($i=0; $i -lt 5; $i++) { $queue.Enqueue((New-ProbeShutdownObservation -At $baseTime.AddMilliseconds($i*250) -Processes @() -LockfileExists $true)) }
            $provider = { $queue.Dequeue() }
            $quiet = Wait-UnityShutdownQuietPeriod -MainProcessIdentity $mainIdentity -TrackedChildIdentities @() -LockPath "C:\Mock\UnityLockfile" -TimeoutMilliseconds 1000 -ObservationProvider $provider -SleepAction { param($Milliseconds) }
            $summaryProbe = [ordered]@{ schemaVersion=2; runId="timeout"; result="Running"; error=$null; sideEffects=[ordered]@{ recovery=[ordered]@{ status="Skipped"; reason="ShutdownQuietPeriodTimedOut"; paths=@() } } }
            $messages = [Collections.Generic.List[string]]::new()
            Add-Failure -Summary $summaryProbe -FailureMessages $messages -Message "Shutdown quiet period timed out; CAS skipped."
            $write = Write-FinalSummarySafely -Path (Join-Path $caseDirectory "Windows64-verification-summary.json") -Summary $summaryProbe -FailureMessages $messages
            $parsed = Get-Content -Raw -LiteralPath $write.path | ConvertFrom-Json -DateKind String
            if (-not $quiet.timedOut -or [string]$parsed.result -cne "Failed" -or [string]$parsed.sideEffects.recovery.status -cne "Skipped") { throw "Quiet timeout did not preserve Failed/Skipped evidence." }
        }

        Invoke-OfflineTestCase -Name "CAS full recovery succeeds" -Results $tests -Action {
            $caseDirectory = New-ExactDirectory -Path (Join-Path $probeDirectory "cas-success") -Label "CAS success probe"
            $evidenceRoot = New-ExactDirectory -Path (Join-Path $caseDirectory "evidence") -Label "CAS success evidence"
            $preDirectory = New-ExactDirectory -Path (Join-Path $evidenceRoot "prebuild") -Label "CAS success prebuild"
            $tracked = Join-Path $caseDirectory "tracked.bin"; $trackedBackup = Join-Path $preDirectory "tracked.bin"
            [IO.File]::WriteAllBytes($tracked,[byte[]](1,2,3)); [IO.File]::Copy($tracked,$trackedBackup,$false)
            $preTracked = Get-PathState -Path $tracked -RelativePath "tracked.bin"; $preTracked | Add-Member backupPath $trackedBackup
            [IO.File]::WriteAllBytes($tracked,[byte[]](4,5,6)); $postTracked = Get-PathState -Path $tracked -RelativePath "tracked.bin"
            $generated = Join-Path $caseDirectory "generated.bin"; $preGenerated = Get-PathState -Path $generated -RelativePath "generated.bin"; $preGenerated | Add-Member backupPath $null
            [IO.File]::WriteAllBytes($generated,[byte[]](7,8,9)); $postGenerated = Get-PathState -Path $generated -RelativePath "generated.bin"
            $backup = [pscustomobject]@{ root=$evidenceRoot; files=@($preTracked,$preGenerated) }
            $recovery = Restore-KnownSideEffectsWithCas -Backup $backup -PostbuildStates @($postTracked,$postGenerated)
            $trackedFinalHash=Get-FileSha256 -Path $tracked; $generatedExists=Test-Path -LiteralPath $generated
            if ($recovery.status -cne "Succeeded" -or $trackedFinalHash -cne $preTracked.sha256 -or $generatedExists) { throw "CAS success path did not restore exact prebuild state. status=$($recovery.status) error=$($recovery.error) tracked=$trackedFinalHash expected=$($preTracked.sha256) generatedExists=$generatedExists" }
            $stored = Get-Content -Raw -LiteralPath $recovery.resultPath | ConvertFrom-Json -DateKind String
            if (@($stored.paths | Where-Object status -ne "Succeeded").Count -ne 0) { throw "CAS success result lacks per-path success." }
        }

        Invoke-OfflineTestCase -Name "CAS race after initial check preserves concurrent bytes" -Results $tests -Action {
            $caseDirectory = New-ExactDirectory -Path (Join-Path $probeDirectory "cas-race-before-lock") -Label "CAS pre-lock race probe"
            $evidenceRoot = New-ExactDirectory -Path (Join-Path $caseDirectory "evidence") -Label "CAS pre-lock race evidence"
            $preDirectory = New-ExactDirectory -Path (Join-Path $evidenceRoot "prebuild") -Label "CAS pre-lock race prebuild"
            $source = Join-Path $caseDirectory "source.bin"; $backupPath = Join-Path $preDirectory "source.bin"
            [IO.File]::WriteAllBytes($source,[byte[]](1,2,3)); [IO.File]::Copy($source,$backupPath,$false)
            $pre = Get-PathState -Path $source -RelativePath "source.bin"; $pre | Add-Member backupPath $backupPath
            [IO.File]::WriteAllBytes($source,[byte[]](4,5,6)); $post = Get-PathState -Path $source -RelativePath "source.bin"
            $concurrentBytes = [byte[]](9,8,7,6)
            $concurrentHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($concurrentBytes)).ToLowerInvariant()
            $phaseHook = {
                param($Phase,$Context)
                if ($Phase -ceq "BeforeFinalTargetLock" -and $Context.path -ceq "source.bin") {
                    [IO.File]::WriteAllBytes($Context.sourcePath,$concurrentBytes)
                }
            }.GetNewClosure()
            $recovery = Restore-KnownSideEffectsWithCas -Backup ([pscustomobject]@{root=$evidenceRoot;files=@($pre)}) -PostbuildStates @($post) -PhaseHook $phaseHook
            $sourceState = Get-PathState -Path $source -RelativePath "source.bin"
            $quarantineState = if ([string]::IsNullOrWhiteSpace([string]$recovery.paths[0].quarantinePath)) { $null } else { Get-PathState -Path $recovery.paths[0].quarantinePath -RelativePath "source.bin" }
            $concurrentBytesPreserved = $sourceState.sha256 -ceq $concurrentHash -or ($null -ne $quarantineState -and $quarantineState.sha256 -ceq $concurrentHash)
            if ($recovery.status -ceq "Succeeded" -or -not $concurrentBytesPreserved -or $sourceState.sha256 -cne $concurrentHash) {
                throw "Pre-lock race overwrote or lost concurrent bytes. status=$($recovery.status) source=$($sourceState.sha256) quarantine=$(if($null -eq $quarantineState){'none'}else{$quarantineState.sha256})"
            }
        }

        Invoke-OfflineTestCase -Name "CAS target held by writer fails without content change" -Results $tests -Action {
            $caseDirectory = New-ExactDirectory -Path (Join-Path $probeDirectory "cas-writer-held") -Label "CAS writer-held probe"
            $evidenceRoot = New-ExactDirectory -Path (Join-Path $caseDirectory "evidence") -Label "CAS writer-held evidence"
            $preDirectory = New-ExactDirectory -Path (Join-Path $evidenceRoot "prebuild") -Label "CAS writer-held prebuild"
            $source = Join-Path $caseDirectory "source.bin"; $backupPath = Join-Path $preDirectory "source.bin"
            [IO.File]::WriteAllBytes($source,[byte[]](1,2,3)); [IO.File]::Copy($source,$backupPath,$false)
            $pre = Get-PathState -Path $source -RelativePath "source.bin"; $pre | Add-Member backupPath $backupPath
            [IO.File]::WriteAllBytes($source,[byte[]](4,5,6)); $post = Get-PathState -Path $source -RelativePath "source.bin"
            $writer = [IO.File]::Open($source,[IO.FileMode]::Open,[IO.FileAccess]::Write,[IO.FileShare]::ReadWrite)
            try {
                $recovery = Restore-KnownSideEffectsWithCas -Backup ([pscustomobject]@{root=$evidenceRoot;files=@($pre)}) -PostbuildStates @($post)
            }
            finally {
                $writer.Dispose()
            }
            $sourceState = Get-PathState -Path $source -RelativePath "source.bin"
            if ($recovery.status -ceq "Succeeded" -or $sourceState.sha256 -cne $post.sha256 -or $recovery.paths[0].mutationOccurred) {
                throw "Writer-held target was changed or reported successful. status=$($recovery.status) source=$($sourceState.sha256) expected=$($post.sha256)"
            }
        }

        Invoke-OfflineTestCase -Name "CAS path recreated after quarantine is never overwritten" -Results $tests -Action {
            $caseDirectory = New-ExactDirectory -Path (Join-Path $probeDirectory "cas-race-recreate") -Label "CAS recreate race probe"
            $evidenceRoot = New-ExactDirectory -Path (Join-Path $caseDirectory "evidence") -Label "CAS recreate race evidence"
            $preDirectory = New-ExactDirectory -Path (Join-Path $evidenceRoot "prebuild") -Label "CAS recreate race prebuild"
            $source = Join-Path $caseDirectory "source.bin"; $backupPath = Join-Path $preDirectory "source.bin"
            [IO.File]::WriteAllBytes($source,[byte[]](1,2,3)); [IO.File]::Copy($source,$backupPath,$false)
            $pre = Get-PathState -Path $source -RelativePath "source.bin"; $pre | Add-Member backupPath $backupPath
            [IO.File]::WriteAllBytes($source,[byte[]](4,5,6)); $post = Get-PathState -Path $source -RelativePath "source.bin"
            $concurrentBytes = [byte[]](7,7,7,7)
            $concurrentHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($concurrentBytes)).ToLowerInvariant()
            $phaseHook = {
                param($Phase,$Context)
                if ($Phase -ceq "AfterTargetQuarantined" -and $Context.path -ceq "source.bin") {
                    [IO.File]::WriteAllBytes($Context.sourcePath,$concurrentBytes)
                }
            }.GetNewClosure()
            $recovery = Restore-KnownSideEffectsWithCas -Backup ([pscustomobject]@{root=$evidenceRoot;files=@($pre)}) -PostbuildStates @($post) -PhaseHook $phaseHook
            $sourceState = Get-PathState -Path $source -RelativePath "source.bin"
            $quarantineState = Get-PathState -Path $recovery.paths[0].quarantinePath -RelativePath "source.bin"
            $replacementState = Get-PathState -Path $recovery.paths[0].replacementPath -RelativePath "source.bin"
            if ($recovery.status -ceq "Succeeded" -or $sourceState.sha256 -cne $concurrentHash -or $quarantineState.sha256 -cne $post.sha256 -or $replacementState.sha256 -cne $pre.sha256) {
                throw "Recreated target was overwritten or recoverable bytes were lost. status=$($recovery.status) source=$($sourceState.sha256) quarantine=$($quarantineState.sha256) replacement=$($replacementState.sha256)"
            }
        }

        Invoke-OfflineTestCase -Name "CAS hash mismatch is rejected without overwrite" -Results $tests -Action {
            $caseDirectory = New-ExactDirectory -Path (Join-Path $probeDirectory "cas-mismatch") -Label "CAS mismatch probe"
            $evidenceRoot = New-ExactDirectory -Path (Join-Path $caseDirectory "evidence") -Label "CAS mismatch evidence"
            $preDirectory = New-ExactDirectory -Path (Join-Path $evidenceRoot "prebuild") -Label "CAS mismatch prebuild"
            $source = Join-Path $caseDirectory "source.bin"; $backupPath = Join-Path $preDirectory "source.bin"
            [IO.File]::WriteAllBytes($source,[byte[]](1)); [IO.File]::Copy($source,$backupPath,$false)
            $pre = Get-PathState -Path $source -RelativePath "source.bin"; $pre | Add-Member backupPath $backupPath
            [IO.File]::WriteAllBytes($source,[byte[]](2)); $post = Get-PathState -Path $source -RelativePath "source.bin"
            [IO.File]::WriteAllBytes($source,[byte[]](3)); $interferenceHash = Get-FileSha256 -Path $source
            $recovery = Restore-KnownSideEffectsWithCas -Backup ([pscustomobject]@{root=$evidenceRoot;files=@($pre)}) -PostbuildStates @($post)
            if ($recovery.status -cne "Failed" -or (Get-FileSha256 -Path $source) -cne $interferenceHash) { throw "CAS mismatch was not rejected and preserved." }
        }

        Invoke-OfflineTestCase -Name "CAS partial recovery is reported" -Results $tests -Action {
            $caseDirectory = New-ExactDirectory -Path (Join-Path $probeDirectory "cas-partial") -Label "CAS partial probe"
            $evidenceRoot = New-ExactDirectory -Path (Join-Path $caseDirectory "evidence") -Label "CAS partial evidence"
            $preDirectory = New-ExactDirectory -Path (Join-Path $evidenceRoot "prebuild") -Label "CAS partial prebuild"
            $first = Join-Path $caseDirectory "first.bin"; $firstBackup = Join-Path $preDirectory "first.bin"
            [IO.File]::WriteAllBytes($first,[byte[]](1)); [IO.File]::Copy($first,$firstBackup,$false)
            $preFirst=Get-PathState -Path $first -RelativePath "first.bin"; $preFirst | Add-Member backupPath $firstBackup
            [IO.File]::WriteAllBytes($first,[byte[]](2)); $postFirst=Get-PathState -Path $first -RelativePath "first.bin"
            $second = Join-Path $caseDirectory "second.bin"
            [IO.File]::WriteAllBytes($second,[byte[]](3)); $preSecond=Get-PathState -Path $second -RelativePath "second.bin"; $preSecond | Add-Member backupPath (Join-Path $preDirectory "missing-second.bin")
            [IO.File]::WriteAllBytes($second,[byte[]](4)); $postSecond=Get-PathState -Path $second -RelativePath "second.bin"
            $recovery=Restore-KnownSideEffectsWithCas -Backup ([pscustomobject]@{root=$evidenceRoot;files=@($preFirst,$preSecond)}) -PostbuildStates @($postFirst,$postSecond)
            $firstFinalHash=Get-FileSha256 -Path $first; $secondFinalHash=Get-FileSha256 -Path $second
            if ($recovery.status -cne "Partial" -or $firstFinalHash -cne $preFirst.sha256 -or $secondFinalHash -cne $postSecond.sha256) { throw "CAS partial result/final states are wrong. status=$($recovery.status) error=$($recovery.error) first=$firstFinalHash/$($preFirst.sha256) second=$secondFinalHash/$($postSecond.sha256)" }
        }

        Invoke-OfflineTestCase -Name "Empty directory statistics and Failed summary are stable" -Results $tests -Action {
            $caseDirectory = New-ExactDirectory -Path (Join-Path $probeDirectory "empty-output") -Label "Empty-output probe"
            $evidence = Get-PartialOutputEvidence -OutputDirectory $caseDirectory
            if (-not $evidence.exists -or $evidence.fileCount -ne 0 -or $evidence.totalBytes -ne 0) { throw "Empty output did not report 0 files/bytes." }
            $summaryProbe=[ordered]@{schemaVersion=2;runId="empty";result="Running";error=$null}; $messages=[Collections.Generic.List[string]]::new()
            Add-Failure -Summary $summaryProbe -FailureMessages $messages -Message "empty probe failure"
            $write=Write-FinalSummarySafely -Path (Join-Path $caseDirectory "summary.json") -Summary $summaryProbe -FailureMessages $messages
            if (-not $write.written) { throw "Failed summary was not written." }
        }

        Invoke-OfflineTestCase -Name "Burst exact optional file allowlist rejects sibling" -Results $tests -Action {
            $absent=Get-OutputAllowlistEvaluation -ReportedRelativePaths @("ElementWar.exe") -ActualRelativePaths @("ElementWar.exe") -AllowedExtraFile $allowedBurstDebugRelativePath
            $good=Get-OutputAllowlistEvaluation -ReportedRelativePaths @("ElementWar.exe") -ActualRelativePaths @("ElementWar.exe",$allowedBurstDebugRelativePath) -AllowedExtraFile $allowedBurstDebugRelativePath
            $rogue="$allowedBurstDebugDirectoryName/Data/Plugins/x86_64/rogue.bin"
            $bad=Get-OutputAllowlistEvaluation -ReportedRelativePaths @("ElementWar.exe") -ActualRelativePaths @("ElementWar.exe",$allowedBurstDebugRelativePath,$rogue) -AllowedExtraFile $allowedBurstDebugRelativePath
            if ($absent.disallowedExtraFiles.Count -ne 0 -or $good.allowedExtraFiles.Count -ne 1 -or $bad.disallowedExtraFiles.Count -ne 1 -or $bad.disallowedExtraFiles[0] -cne $rogue) { throw "Burst exact-file allowlist is too broad or unstable." }
        }

        Invoke-OfflineTestCase -Name "Null empty and single arrays serialize stably" -Results $tests -Action {
            $payload=[ordered]@{ nullValue=(ConvertTo-StableArray -Value $null); emptyValue=(ConvertTo-StableArray -Value @()); singleValue=(ConvertTo-StableArray -Value @("one")) }
            $json=$payload | ConvertTo-Json -Depth 4 -Compress
            $parsed=$json | ConvertFrom-Json -DateKind String
            $nullCount=@($parsed.nullValue).Count; $emptyCount=@($parsed.emptyValue).Count; $singleCount=@($parsed.singleValue).Count
            if ($nullCount -ne 0 -or $emptyCount -ne 0 -or $singleCount -ne 1 -or [string]$parsed.singleValue[0] -cne "one") { throw "Array JSON shape collapsed or changed. null=$nullCount empty=$emptyCount single=$singleCount json=$json" }
        }

        Invoke-OfflineTestCase -Name "Replay latest successful physical build evidence" -Results $tests -Action {
            $evidenceDirectory=Join-Path $RepositoryPath "Logs\Verification\$offlineReplayRunId-windows64"
            $reportPath=Join-Path $evidenceDirectory "Windows64-build-report.json"
            $logPathProbe=Join-Path $evidenceDirectory "Windows64.log"
            $report=Get-Content -Raw -LiteralPath $reportPath | ConvertFrom-Json -DateKind String
            if ([int]$report.schemaVersion -ne 2 -or [string]$report.status -cne "Succeeded" -or [string]$report.buildResult -cne "Succeeded" -or [string]$report.buildTarget -cne "StandaloneWindows64" -or [int]$report.totalErrors -ne 0) { throw "Replay C# report status/target/errors gate failed." }
            $entryStart=ConvertTo-Int64Strict $report.entryStartedAtUnixMilliseconds "Replay entry start"; $pipelineStart=ConvertTo-Int64Strict $report.buildPipelineStartedAtUnixMilliseconds "Replay pipeline start"; $pipelineEnd=ConvertTo-Int64Strict $report.buildPipelineFinishedAtUnixMilliseconds "Replay pipeline end"; $entryEnd=ConvertTo-Int64Strict $report.entryFinishedAtUnixMilliseconds "Replay entry end"
            [void](Assert-IsoMatchesUnixMilliseconds $report.entryStartedAtUtc $entryStart "Replay entry start"); [void](Assert-IsoMatchesUnixMilliseconds $report.buildPipelineStartedAtUtc $pipelineStart "Replay pipeline start"); [void](Assert-IsoMatchesUnixMilliseconds $report.buildPipelineFinishedAtUtc $pipelineEnd "Replay pipeline end"); [void](Assert-IsoMatchesUnixMilliseconds $report.entryFinishedAtUtc $entryEnd "Replay entry end")
            [void](Assert-UnixMillisecondOrder ([ordered]@{entryStart=$entryStart;pipelineStart=$pipelineStart;pipelineEnd=$pipelineEnd;entryEnd=$entryEnd}) "Replay C# time")
            $scenes=@($report.scenes); if($scenes.Count -ne 1 -or [string]$scenes[0].path -cne $expectedScenePath -or [string]$scenes[0].guid -cne $expectedSceneGuid){throw "Replay Bootstrap scene gate failed."}
            if([string]$report.scriptingBackend -cne "Mono2x"){throw "Replay Mono2x gate failed."}
            if([string]$report.addressablesBuildWithPlayer.effectiveOption -cne $expectedAddressablesBuildOptionName -or [int]$report.addressablesBuildWithPlayer.serializedValue -ne $expectedAddressablesBuildOptionValue){throw "Replay Addressables gate failed."}
            $player=Resolve-NormalizedPath ([string]$report.playerPath); $output=Split-Path -Parent $player
            [void](Assert-Windows64Output -OutputDirectory $output -PlayerPath $player -ScriptingBackend ([string]$report.scriptingBackend) -BuildFiles @($report.files))
            [void](Assert-FreshArtifactFileUnix -Path $player -Label "Replay Player" -WindowStartUnixMilliseconds $pipelineStart -WindowEndUnixMilliseconds $pipelineEnd -ToleranceMilliseconds $freshnessToleranceMilliseconds)
            $log=Get-UnityLogDiagnostics -LogPath $logPathProbe
            if($null -eq $log -or $log.compilerErrorOccurrenceCount -ne 0 -or $log.fatalErrorLineCount -ne 0){throw "Replay Unity log blocking gate failed."}
        }
    }
    finally {
        $cleanupStarted=[DateTimeOffset]::UtcNow
        try {
            $normalizedProbe=Resolve-NormalizedPath -Path $probeDirectory
            $safeName=[IO.Path]::GetFileName($normalizedProbe).StartsWith("ElementWarWindows64Probe-",[StringComparison]::Ordinal)
            if(-not (Test-IsPathWithinOrEqualRoot -Path $normalizedProbe -Root $tempRoot) -or [string]::Equals($normalizedProbe,$tempRoot,[StringComparison]::OrdinalIgnoreCase) -or -not $safeName){throw "Refusing unsafe recursive cleanup: $normalizedProbe"}
            Assert-NoReparsePointInPath -Path $normalizedProbe -Label "Probe cleanup"
            if(Test-Path -LiteralPath $normalizedProbe -PathType Container){[IO.Directory]::Delete($normalizedProbe,$true)}
            if(Test-Path -LiteralPath $normalizedProbe){throw "Probe directory still exists after cleanup."}
            [void]$tests.Add([pscustomobject][ordered]@{name="System temporary probe cleanup";result="PASS";error=$null;durationMilliseconds=[long]([DateTimeOffset]::UtcNow-$cleanupStarted).TotalMilliseconds})
        }
        catch {
            [void]$tests.Add([pscustomobject][ordered]@{name="System temporary probe cleanup";result="FAIL";error=$_.Exception.Message;durationMilliseconds=[long]([DateTimeOffset]::UtcNow-$cleanupStarted).TotalMilliseconds})
        }
    }

    $failed=@($tests | Where-Object result -ceq "FAIL")
    return [pscustomobject][ordered]@{
        result=if($failed.Count -eq 0){"Passed"}else{"Failed"}
        testCount=$tests.Count
        passedCount=@($tests | Where-Object result -ceq "PASS").Count
        failedCount=$failed.Count
        tests=@($tests)
        systemTemporaryRoot=$tempRoot
        probeDirectory=$probeDirectory
        cleaned=-not (Test-Path -LiteralPath $probeDirectory)
    }
}

if ($ProbeOnly) {
    $probeRepository = Resolve-NormalizedPath -Path $ProjectPath
    $probeResult = Invoke-StaticProbes -RepositoryPath $probeRepository
    foreach ($test in @($probeResult.tests)) {
        Write-Host ("[{0}] {1}{2}" -f $test.result, $test.name, $(if ($null -eq $test.error) { "" } else { ": $($test.error)" }))
    }
    $probeResult | ConvertTo-Json -Depth 12
    if ($probeResult.result -ceq "Passed") { exit 0 }
    exit 1
}

$ProjectPath = Resolve-NormalizedPath -Path $ProjectPath
$UnityExe = Resolve-NormalizedPath -Path $UnityExe
if (-not (Test-Path -LiteralPath $ProjectPath -PathType Container)) {
    throw "Project path does not exist: $ProjectPath"
}
Assert-NoReparsePointInPath -Path $ProjectPath -Label "Unity project"

$runId = New-VerificationRunId
$buildsRoot = Resolve-NormalizedPath -Path (Join-Path $ProjectPath "Builds\Windows64")
$outputDirectory = Resolve-NormalizedPath -Path (Join-Path $buildsRoot $runId)
$playerPath = Resolve-NormalizedPath -Path (Join-Path $outputDirectory "ElementWar.exe")
$artifactsRoot = Resolve-NormalizedPath -Path (Join-Path $ProjectPath "Logs\Verification")
$artifactsDirectory = Resolve-NormalizedPath -Path (Join-Path $artifactsRoot "$runId-windows64")
$logPath = Join-Path $artifactsDirectory "Windows64.log"
$buildReportPath = Join-Path $artifactsDirectory "Windows64-build-report.json"
$summaryPath = Join-Path $artifactsDirectory "Windows64-verification-summary.json"

foreach ($pathCheck in @(
    [pscustomobject]@{ path = $buildsRoot; label = "Builds root" },
    [pscustomobject]@{ path = $outputDirectory; label = "Windows64 output" },
    [pscustomobject]@{ path = $artifactsRoot; label = "Verification root" },
    [pscustomobject]@{ path = $artifactsDirectory; label = "Windows64 evidence" }
)) {
    Assert-NoReparsePointInPath -Path $pathCheck.path -Label $pathCheck.label
}
if (Test-Path -LiteralPath $outputDirectory) {
    throw "Output path collision; refusing overwrite/delete: $outputDirectory"
}
if (Test-Path -LiteralPath $artifactsDirectory) {
    throw "Evidence path collision; refusing overwrite/delete: $artifactsDirectory"
}
if (-not (Test-Path -LiteralPath $artifactsRoot -PathType Container)) {
    [void][IO.Directory]::CreateDirectory($artifactsRoot)
}
[void](New-ExactDirectory -Path $artifactsDirectory -Label "Windows64 evidence directory")

$scriptStartedAt = [DateTimeOffset]::UtcNow
$summary = [ordered]@{
    schemaVersion = 2
    mode = "Windows64"
    runId = $runId
    projectPath = $ProjectPath
    unityExe = $UnityExe
    expectedUnityVersion = $expectedUnityVersion
    projectVersion = $null
    approvedFormalPaths = $approvedFormalPaths
    knownSideEffectPaths = $knownSideEffectPaths
    approvedScene = [ordered]@{
        path = $expectedScenePath
        guid = $expectedSceneGuid
    }
    approvedAddressablesBuildOption = [ordered]@{
        settingsPath = $addressableSettingsPath
        optionName = $expectedAddressablesBuildOptionName
        serializedValue = $expectedAddressablesBuildOptionValue
    }
    outputDirectory = $outputDirectory
    playerPath = $playerPath
    artifactsDirectory = $artifactsDirectory
    log = $logPath
    cSharpBuildReport = $buildReportPath
    summary = $summaryPath
    timeoutMinutes = $TimeoutMinutes
    startedAtUtc = $scriptStartedAt.ToString("o")
    startedAtUnixMilliseconds = [long]$scriptStartedAt.ToUnixTimeMilliseconds()
    finishedAtUtc = $null
    finishedAtUnixMilliseconds = $null
    result = "Running"
    error = $null
    probes = $null
    preflight = [ordered]@{
        windowsStandaloneSupportPath = $null
        unityLockfileExists = $null
        existingUnityProcessIds = @()
        addressablesSerializedValue = $null
        gameEditorAsmdef = $null
    }
    git = [ordered]@{
        headBefore = $null
        headAfter = $null
        repositoryStateBefore = @()
        repositoryStateAfterUnity = @()
        repositoryStateFinal = @()
        repositoryStateFingerprintBefore = $null
        repositoryStateFingerprintAfterUnity = $null
        repositoryStateFingerprintFinal = $null
        changesDuringUnity = @()
        unexpectedChangesDuringUnity = @()
        finalChanges = @()
        formalFilesBefore = @()
        formalFilesAfter = @()
        formalFileChanges = @()
        generatedProjectFilesBefore = @()
        generatedProjectFilesAfterUnity = @()
        generatedProjectFilesFinal = @()
        generatedProjectFilesChangedDuringUnity = @()
        generatedProjectFilesChangedFinal = @()
    }
    sideEffects = [ordered]@{
        prebuildManifest = $null
        prebuild = @()
        postbuildManifest = $null
        postbuild = @()
        recovery = [ordered]@{
            status = "Skipped"
            reason = "NotReached"
            paths = @()
        }
    }
    unityProcess = [ordered]@{
        processId = $null
        processIdentity = $null
        startedAtUtc = $null
        startedAtUnixMilliseconds = $null
        finishedAtUtc = $null
        finishedAtUnixMilliseconds = $null
        exitCode = $null
        timedOut = $false
        wasTerminated = $false
        terminationIdentityVerified = $false
        terminationError = $null
        hasExited = $null
        discoveredChildProcesses = @()
        processTrackingErrors = @()
        shutdownQuietPeriod = $null
    }
    timeEvidence = $null
    artifactFreshness = $null
    build = $null
    unityLogDiagnostics = $null
    output = $null
    partialOutput = $null
}

$failureMessages = [Collections.Generic.List[string]]::new()
$repositoryStateBefore = $null
$formalFilesBefore = $null
$generatedProjectFilesBefore = $null
$sideEffectBackup = $null
$unityRun = $null
$summaryWriteEvidence = $null

try {
    $projectVersionPath = Join-Path $ProjectPath "ProjectSettings\ProjectVersion.txt"
    $packageManifestPath = Join-Path $ProjectPath "Packages\manifest.json"
    if (-not (Test-Path -LiteralPath $projectVersionPath -PathType Leaf) -or
        -not (Test-Path -LiteralPath $packageManifestPath -PathType Leaf)) {
        throw "Path is not a Unity project root: $ProjectPath"
    }
    if (-not (Test-Path -LiteralPath $UnityExe -PathType Leaf)) {
        throw "Unity executable does not exist: $UnityExe"
    }
    Assert-NoReparsePointInPath -Path $UnityExe -Label "Unity executable"

    $projectVersion = (Get-Content -LiteralPath $projectVersionPath -Raw).Trim()
    $summary.projectVersion = $projectVersion
    if ($projectVersion -notmatch "(?m)^m_EditorVersion:\s*2022\.3\.62f2c1\s*$") {
        throw "Expected Unity $expectedUnityVersion; ProjectVersion.txt contains: $projectVersion"
    }
    $summary.probes = Invoke-StaticProbes -RepositoryPath $ProjectPath
    if ([string]$summary.probes.result -cne "Passed" -or [int]$summary.probes.failedCount -ne 0) {
        throw "One or more offline probes failed; Unity was not started."
    }

    $windowsSupportPath = Join-Path (Split-Path -Parent $UnityExe) "Data\PlaybackEngines\windowsstandalonesupport"
    $summary.preflight.windowsStandaloneSupportPath = $windowsSupportPath
    Assert-NoReparsePointInPath -Path $windowsSupportPath -Label "Windows Standalone support"
    if (-not (Test-Path -LiteralPath $windowsSupportPath -PathType Container)) {
        throw "Windows Standalone Build Support is missing: $windowsSupportPath"
    }
    $win64Variations = @(Get-ChildItem -LiteralPath (Join-Path $windowsSupportPath "Variations") -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like "win64_player_*" })
    if ($win64Variations.Count -le 0) {
        throw "Windows Standalone support contains no Win64 Player variation."
    }

    $sceneAbsolutePath = Join-Path $ProjectPath $expectedScenePath
    $sceneMetaPath = "$sceneAbsolutePath.meta"
    if (-not (Test-Path -LiteralPath $sceneAbsolutePath -PathType Leaf) -or
        -not (Test-Path -LiteralPath $sceneMetaPath -PathType Leaf)) {
        throw "Approved Bootstrap scene or meta is missing: $expectedScenePath"
    }
    Assert-NoReparsePointInPath -Path $sceneAbsolutePath -Label "Bootstrap scene"
    Assert-NoReparsePointInPath -Path $sceneMetaPath -Label "Bootstrap scene meta"
    $sceneGuidMatch = Select-String -LiteralPath $sceneMetaPath -Pattern '^guid:\s*([0-9a-f]{32})$' |
        Select-Object -First 1
    $actualSceneGuid = if ($null -eq $sceneGuidMatch) {
        $null
    }
    else {
        $sceneGuidMatch.Matches[0].Groups[1].Value
    }
    if ($actualSceneGuid -cne $expectedSceneGuid) {
        throw "Bootstrap GUID mismatch. Expected=$expectedSceneGuid actual=$actualSceneGuid"
    }

    $addressableSettingsAbsolutePath = Join-Path $ProjectPath $addressableSettingsPath
    Assert-NoReparsePointInPath -Path $addressableSettingsAbsolutePath -Label "Addressables settings"
    $addressablesMatch = Select-String -LiteralPath $addressableSettingsAbsolutePath -Pattern '^\s*m_BuildAddressablesWithPlayerBuild:\s*([0-9]+)\s*$' |
        Select-Object -First 1
    if ($null -eq $addressablesMatch) {
        throw "Addressables Build With Player field is missing."
    }
    $addressablesValue = [int]$addressablesMatch.Matches[0].Groups[1].Value
    $summary.preflight.addressablesSerializedValue = $addressablesValue
    if ($addressablesValue -ne $expectedAddressablesBuildOptionValue) {
        throw "Addressables must explicitly use BuildWithPlayer value 1; actual=$addressablesValue"
    }

    $gameEditorAsmdefPath = Join-Path $ProjectPath "Assets\Scripts\Editor\Game.Editor.asmdef"
    $gameEditorAsmdef = Get-Content -LiteralPath $gameEditorAsmdefPath -Raw |
        ConvertFrom-Json -DateKind String
    $summary.preflight.gameEditorAsmdef = [ordered]@{
        path = $gameEditorAsmdefPath
        sha256 = Get-FileSha256 -Path $gameEditorAsmdefPath
        includePlatforms = @($gameEditorAsmdef.includePlatforms)
        excludePlatforms = @($gameEditorAsmdef.excludePlatforms)
        architectureDebt = "Existing unrestricted Editor asmdef; not modified."
    }

    $lockPath = Join-Path $ProjectPath "Temp\UnityLockfile"
    $lockExists = Test-Path -LiteralPath $lockPath -PathType Leaf
    $summary.preflight.unityLockfileExists = $lockExists
    if ($lockExists) {
        throw "Unity Lockfile exists; refusing to delete or start: $lockPath"
    }
    $existingUnity = @(Get-Process -Name Unity -ErrorAction SilentlyContinue)
    $summary.preflight.existingUnityProcessIds = @($existingUnity | ForEach-Object { $_.Id })
    if ($existingUnity.Count -gt 0) {
        throw "Existing Unity.exe detected: $(@($existingUnity.Id) -join ', ')"
    }

    $headBefore = (& git -C $ProjectPath rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw "git rev-parse HEAD failed." }
    if ($headBefore -cne $expectedHeadCommit) {
        throw "Unexpected HEAD. Expected=$expectedHeadCommit actual=$headBefore"
    }
    Assert-IndexEmpty -RepositoryPath $ProjectPath
    $summary.git.headBefore = $headBefore

    $repositoryStateBefore = @(Get-RepositoryStateSnapshot -RepositoryPath $ProjectPath)
    $formalFilesBefore = @(Get-FormalFileSnapshot -RepositoryPath $ProjectPath -RelativePaths $approvedFormalPaths)
    $generatedProjectFilesBefore = @(Get-GeneratedProjectFileSnapshot -RepositoryPath $ProjectPath)
    $summary.git.repositoryStateBefore = $repositoryStateBefore
    $summary.git.repositoryStateFingerprintBefore = Get-RepositoryStateFingerprint -Snapshot $repositoryStateBefore
    $summary.git.formalFilesBefore = $formalFilesBefore
    $summary.git.generatedProjectFilesBefore = $generatedProjectFilesBefore

    $sideEffectBackup = Initialize-SideEffectBackup -RepositoryPath $ProjectPath -EvidenceDirectory $artifactsDirectory -RelativePaths $knownSideEffectPaths
    $summary.sideEffects.prebuildManifest = $sideEffectBackup.manifestPath
    $summary.sideEffects.prebuild = @($sideEffectBackup.files)

    $stateBeforeLaunch = @(Get-RepositoryStateSnapshot -RepositoryPath $ProjectPath)
    $freezeDrift = @(Compare-RepositoryStateSnapshots -Before $repositoryStateBefore -After $stateBeforeLaunch)
    if ($freezeDrift.Count -gt 0) {
        throw "Repository changed after freeze: $(@($freezeDrift.path) -join ', ')"
    }
    $formalBeforeLaunch = @(Get-FormalFileSnapshot -RepositoryPath $ProjectPath -RelativePaths $approvedFormalPaths)
    $formalFreezeDrift = @(Compare-FileSnapshots -Before $formalFilesBefore -After $formalBeforeLaunch)
    if ($formalFreezeDrift.Count -gt 0) {
        throw "Formal files changed after freeze: $(@($formalFreezeDrift.path) -join ', ')"
    }
    Assert-IndexEmpty -RepositoryPath $ProjectPath
    if (@(Get-Process -Name Unity -ErrorAction SilentlyContinue).Count -gt 0) {
        throw "Unity.exe appeared after freeze."
    }
    if (Test-Path -LiteralPath $lockPath -PathType Leaf) {
        throw "Unity Lockfile appeared after freeze; refusing deletion."
    }
    if (Test-Path -LiteralPath $outputDirectory) {
        throw "Output directory appeared after freeze: $outputDirectory"
    }
    if ((Test-Path -LiteralPath $logPath) -or
        (Test-Path -LiteralPath $buildReportPath) -or
        (Test-Path -LiteralPath $summaryPath)) {
        throw "Evidence file collision appeared after freeze."
    }

    $arguments = @(
        "-batchmode",
        "-quit",
        "-projectPath", $ProjectPath,
        "-buildTarget", "StandaloneWindows64",
        "-executeMethod", $executeMethod,
        "-elementWarRunId", $runId,
        "-elementWarPlayerPath", $playerPath,
        "-elementWarBuildReportPath", $buildReportPath,
        "-logFile", $logPath
    )
    $unityRun = Invoke-UnityProcess -ExecutablePath $UnityExe -Arguments $arguments -MaximumMinutes $TimeoutMinutes
    foreach ($property in @(
        "processId",
        "processIdentity",
        "startedAtUtc",
        "startedAtUnixMilliseconds",
        "finishedAtUtc",
        "finishedAtUnixMilliseconds",
        "exitCode",
        "timedOut",
        "wasTerminated",
        "terminationIdentityVerified",
        "terminationError",
        "hasExited",
        "discoveredChildProcesses",
        "processTrackingErrors"
    )) {
        $summary.unityProcess[$property] = $unityRun.$property
    }
    if (-not $unityRun.hasExited) {
        $summary.sideEffects.recovery = [ordered]@{
            status = "Skipped"
            reason = "ExactUnityProcessDidNotExit"
            paths = @()
        }
        throw "Launched Unity PID $($unityRun.processId) did not exit; preserving side effects."
    }

    $shutdownQuietPeriod = Wait-UnityShutdownQuietPeriod `
        -MainProcessIdentity $unityRun.processIdentity `
        -TrackedChildIdentities @($unityRun.discoveredChildProcesses) `
        -LockPath $lockPath `
        -TimeoutMilliseconds $shutdownQuietTimeoutMilliseconds `
        -PollIntervalMilliseconds $shutdownQuietPollIntervalMilliseconds `
        -RequiredConsecutiveAbsent $shutdownQuietRequiredConsecutiveAbsent
    $summary.unityProcess.shutdownQuietPeriod = $shutdownQuietPeriod
    if (-not $shutdownQuietPeriod.succeeded) {
        $summary.sideEffects.recovery = [ordered]@{
            status = "Skipped"
            reason = "ShutdownQuietPeriodTimedOut"
            paths = @()
        }
        throw "Unity shutdown quiet period timed out after $shutdownQuietTimeoutMilliseconds ms; CAS recovery skipped."
    }

    $repositoryStateAfterUnity = @(Get-RepositoryStateSnapshot -RepositoryPath $ProjectPath)
    $generatedProjectFilesAfterUnity = @(Get-GeneratedProjectFileSnapshot -RepositoryPath $ProjectPath)
    $changesDuringUnity = @(Compare-RepositoryStateSnapshots -Before $repositoryStateBefore -After $repositoryStateAfterUnity)
    $unexpectedChanges = @($changesDuringUnity | Where-Object { $_.path -notin $knownSideEffectPaths })
    $postbuildSideEffects = @(Get-KnownSideEffectSnapshot -RepositoryPath $ProjectPath -RelativePaths $knownSideEffectPaths)
    $postbuildManifest = Write-PostbuildSideEffectManifest -Backup $sideEffectBackup -PostbuildStates $postbuildSideEffects

    $summary.git.repositoryStateAfterUnity = $repositoryStateAfterUnity
    $summary.git.repositoryStateFingerprintAfterUnity = Get-RepositoryStateFingerprint -Snapshot $repositoryStateAfterUnity
    $summary.git.changesDuringUnity = $changesDuringUnity
    $summary.git.unexpectedChangesDuringUnity = $unexpectedChanges
    $summary.git.generatedProjectFilesAfterUnity = $generatedProjectFilesAfterUnity
    $summary.git.generatedProjectFilesChangedDuringUnity = @(Compare-FileSnapshots -Before $generatedProjectFilesBefore -After $generatedProjectFilesAfterUnity)
    $summary.sideEffects.postbuildManifest = $postbuildManifest
    $summary.sideEffects.postbuild = $postbuildSideEffects

    if ($unexpectedChanges.Count -gt 0) {
        $summary.sideEffects.recovery = [ordered]@{
            status = "Skipped"
            reason = "UnexpectedRepositoryChanges"
            paths = @()
        }
        throw "Unexpected tracked/untracked changes; full postbuild state preserved: $(@($unexpectedChanges.path) -join ', ')"
    }

    $sideEffectRecovery = Restore-KnownSideEffectsWithCas -Backup $sideEffectBackup -PostbuildStates $postbuildSideEffects
    $summary.sideEffects.recovery = $sideEffectRecovery
    if ([string]$sideEffectRecovery.status -cne "Succeeded") {
        throw "Side-effect CAS recovery ended as '$($sideEffectRecovery.status)': $($sideEffectRecovery.error)"
    }
    $repositoryStateFinal = @(Get-RepositoryStateSnapshot -RepositoryPath $ProjectPath)
    $formalFilesAfter = @(Get-FormalFileSnapshot -RepositoryPath $ProjectPath -RelativePaths $approvedFormalPaths)
    $generatedProjectFilesFinal = @(Get-GeneratedProjectFileSnapshot -RepositoryPath $ProjectPath)
    $finalChanges = @(Compare-RepositoryStateSnapshots -Before $repositoryStateBefore -After $repositoryStateFinal)
    $formalFileChanges = @(Compare-FileSnapshots -Before $formalFilesBefore -After $formalFilesAfter)
    $summary.git.repositoryStateFinal = $repositoryStateFinal
    $summary.git.repositoryStateFingerprintFinal = Get-RepositoryStateFingerprint -Snapshot $repositoryStateFinal
    $summary.git.finalChanges = $finalChanges
    $summary.git.formalFilesAfter = $formalFilesAfter
    $summary.git.formalFileChanges = $formalFileChanges
    $summary.git.generatedProjectFilesFinal = $generatedProjectFilesFinal
    $summary.git.generatedProjectFilesChangedFinal = @(Compare-FileSnapshots -Before $generatedProjectFilesBefore -After $generatedProjectFilesFinal)
    if ($finalChanges.Count -gt 0) {
        throw "Final Git state differs from freeze: $(@($finalChanges.path) -join ', ')"
    }
    if ($formalFileChanges.Count -gt 0) {
        throw "Formal files changed during Unity: $(@($formalFileChanges.path) -join ', ')"
    }
    Assert-IndexEmpty -RepositoryPath $ProjectPath
    $headAfter = (& git -C $ProjectPath rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw "git rev-parse HEAD failed after Unity." }
    $summary.git.headAfter = $headAfter
    if ($headAfter -cne $headBefore) {
        throw "HEAD changed. Before=$headBefore after=$headAfter"
    }

    if ($unityRun.timedOut) {
        throw "Build exceeded $TimeoutMinutes minutes. Exact PID=$($unityRun.processId), terminated=$($unityRun.wasTerminated), terminationError=$($unityRun.terminationError)"
    }

    $logEvidence = Assert-FreshArtifactFileUnix -Path $logPath -Label "Windows64 Unity log" -WindowStartUnixMilliseconds $unityRun.startedAtUnixMilliseconds -WindowEndUnixMilliseconds $unityRun.finishedAtUnixMilliseconds -ToleranceMilliseconds $freshnessToleranceMilliseconds
    $reportEvidence = Assert-FreshArtifactFileUnix -Path $buildReportPath -Label "Windows64 C# report" -WindowStartUnixMilliseconds $unityRun.startedAtUnixMilliseconds -WindowEndUnixMilliseconds $unityRun.finishedAtUnixMilliseconds -ToleranceMilliseconds $freshnessToleranceMilliseconds
    $buildReport = Get-Content -LiteralPath $buildReportPath -Raw |
        ConvertFrom-Json -DateKind String
    $summary.build = $buildReport
    if ([int]$buildReport.schemaVersion -ne 2) {
        throw "C# report schemaVersion is '$($buildReport.schemaVersion)', expected 2."
    }
    if ([string]$buildReport.runId -cne $runId) {
        throw "C# report runId mismatch: $($buildReport.runId)"
    }
    if ([int]$buildReport.unityProcessId -ne [int]$unityRun.processId) {
        throw "C# report PID '$($buildReport.unityProcessId)' does not match '$($unityRun.processId)'."
    }
    if ([string]$buildReport.unityVersion -cne $expectedUnityVersion) {
        throw "C# report Unity version mismatch: $($buildReport.unityVersion)"
    }

    $entryStartUnix = ConvertTo-Int64Strict -Value $buildReport.entryStartedAtUnixMilliseconds -Label "C# entry start"
    $entryEndUnix = ConvertTo-Int64Strict -Value $buildReport.entryFinishedAtUnixMilliseconds -Label "C# entry end"
    $pipelineStartUnix = ConvertTo-Int64Strict -Value $buildReport.buildPipelineStartedAtUnixMilliseconds -Label "BuildPipeline start"
    $pipelineEndUnix = ConvertTo-Int64Strict -Value $buildReport.buildPipelineFinishedAtUnixMilliseconds -Label "BuildPipeline end"
    $isoCrossChecks = @(
        Assert-IsoMatchesUnixMilliseconds -IsoValue $unityRun.processIdentity.creationTimeUtc -UnixMilliseconds $unityRun.processIdentity.creationTimeUnixMilliseconds -Label "Unity process creationTime"
        Assert-IsoMatchesUnixMilliseconds -IsoValue $buildReport.entryStartedAtUtc -UnixMilliseconds $entryStartUnix -Label "C# entryStartedAt"
        Assert-IsoMatchesUnixMilliseconds -IsoValue $buildReport.entryFinishedAtUtc -UnixMilliseconds $entryEndUnix -Label "C# entryFinishedAt"
        Assert-IsoMatchesUnixMilliseconds -IsoValue $buildReport.buildPipelineStartedAtUtc -UnixMilliseconds $pipelineStartUnix -Label "C# BuildPipeline startedAt"
        Assert-IsoMatchesUnixMilliseconds -IsoValue $buildReport.buildPipelineFinishedAtUtc -UnixMilliseconds $pipelineEndUnix -Label "C# BuildPipeline finishedAt"
    )
    $orderedUnixEvidence = Assert-UnixMillisecondOrder -OrderedValues ([ordered]@{
        processLaunchRequested = $unityRun.startedAtUnixMilliseconds
        processCreated = $unityRun.processIdentity.creationTimeUnixMilliseconds
        entryStarted = $entryStartUnix
        buildPipelineStarted = $pipelineStartUnix
        buildPipelineFinished = $pipelineEndUnix
        entryFinished = $entryEndUnix
        processFinished = $unityRun.finishedAtUnixMilliseconds
    }) -Label "Authoritative process/build time"
    $summary.timeEvidence = [ordered]@{
        authority = "C# BuildPipeline invocation Unix milliseconds"
        orderedUnixMilliseconds = $orderedUnixEvidence
        isoCrossChecks = $isoCrossChecks
        buildSummaryDiagnosticOnly = [ordered]@{
            startedAtRaw = [string]$buildReport.buildSummaryStartedAtRaw
            startedAtKind = [string]$buildReport.buildSummaryStartedAtKind
            endedAtRaw = [string]$buildReport.buildSummaryEndedAtRaw
            endedAtKind = [string]$buildReport.buildSummaryEndedAtKind
            durationSeconds = $buildReport.buildSummaryDurationSeconds
        }
    }

    if ($unityRun.exitCode -ne 0) {
        throw "Unity returned non-zero exit code $($unityRun.exitCode)."
    }
    if ([string]$buildReport.status -cne "Succeeded") {
        throw "C# report status is '$($buildReport.status)': $($buildReport.errorMessage)"
    }
    if ([string]$buildReport.buildResult -cne "Succeeded") {
        throw "BuildReport result is '$($buildReport.buildResult)'."
    }
    if ([string]$buildReport.buildTarget -cne "StandaloneWindows64") {
        throw "BuildReport target is '$($buildReport.buildTarget)'."
    }
    if ([int]$buildReport.totalErrors -ne 0) {
        throw "BuildSummary recorded $($buildReport.totalErrors) errors."
    }
    if ([string]$buildReport.addressablesBuildWithPlayer.settingsPath -cne $addressableSettingsPath -or
        [string]$buildReport.addressablesBuildWithPlayer.effectiveOption -cne $expectedAddressablesBuildOptionName -or
        [int]$buildReport.addressablesBuildWithPlayer.serializedValue -ne $expectedAddressablesBuildOptionValue) {
        throw "C# report did not confirm effective Addressables BuildWithPlayer."
    }

    $reportedPlayerPath = Resolve-NormalizedPath -Path ([string]$buildReport.playerPath)
    $reportedOutputPath = Resolve-NormalizedPath -Path ([string]$buildReport.buildOutputPath)
    if (-not [string]::Equals($reportedPlayerPath, $playerPath, [StringComparison]::OrdinalIgnoreCase) -or
        -not [string]::Equals($reportedOutputPath, $playerPath, [StringComparison]::OrdinalIgnoreCase)) {
        throw "C# report output path does not match approved Player path."
    }
    $reportedScenes = @($buildReport.scenes)
    if ($reportedScenes.Count -ne 1 -or
        [string]$reportedScenes[0].path -cne $expectedScenePath -or
        [string]$reportedScenes[0].guid -cne $expectedSceneGuid) {
        throw "C# report scenes do not exactly match Bootstrap path/GUID."
    }

    $outputEvidence = Assert-Windows64Output -OutputDirectory $outputDirectory -PlayerPath $playerPath -ScriptingBackend ([string]$buildReport.scriptingBackend) -BuildFiles @($buildReport.files)
    $playerFreshness = Assert-FreshArtifactFileUnix -Path $playerPath -Label "Windows64 Player" -WindowStartUnixMilliseconds $pipelineStartUnix -WindowEndUnixMilliseconds $pipelineEndUnix -ToleranceMilliseconds $freshnessToleranceMilliseconds
    $summary.artifactFreshness = [ordered]@{
        hardGateUnit = "Int64 Unix milliseconds"
        toleranceMilliseconds = $freshnessToleranceMilliseconds
        log = $logEvidence
        cSharpBuildReport = $reportEvidence
        player = $playerFreshness
    }
    $summary.output = $outputEvidence

    $logDiagnostics = Get-UnityLogDiagnostics -LogPath $logPath
    $summary.unityLogDiagnostics = $logDiagnostics
    if ($logDiagnostics.compilerErrorOccurrenceCount -gt 0 -or
        $logDiagnostics.fatalErrorLineCount -gt 0) {
        throw "Unity log contains blocking errors. Compiler=$($logDiagnostics.compilerErrorOccurrenceCount), fatal=$($logDiagnostics.fatalErrorLineCount)"
    }

    $lastState = @(Get-RepositoryStateSnapshot -RepositoryPath $ProjectPath)
    $lateChanges = @(Compare-RepositoryStateSnapshots -Before $repositoryStateBefore -After $lastState)
    if ($lateChanges.Count -gt 0) {
        throw "Repository changed after output validation: $(@($lateChanges.path) -join ', ')"
    }
    Assert-IndexEmpty -RepositoryPath $ProjectPath
    $summary.git.repositoryStateFinal = $lastState
    $summary.git.repositoryStateFingerprintFinal = Get-RepositoryStateFingerprint -Snapshot $lastState
    $summary.result = "Passed"
}
catch {
    Add-FailureSafely -Summary $summary -FailureMessages $failureMessages -Message $_.Exception.Message
}
finally {
    try {
        try {
            $summary.partialOutput = Get-PartialOutputEvidence -OutputDirectory $outputDirectory
        }
        catch {
            $summary.partialOutput = [ordered]@{
                exists = $null
                path = $outputDirectory
                collectionError = $_.Exception.Message
            }
            Add-FailureSafely -Summary $summary -FailureMessages $failureMessages -Message "Partial output collection failed: $($_.Exception.Message)"
        }

        if ($null -eq $summary.unityLogDiagnostics -and
            (Test-Path -LiteralPath $logPath -PathType Leaf)) {
            try {
                $summary.unityLogDiagnostics = Get-UnityLogDiagnostics -LogPath $logPath
            }
            catch {
                Add-FailureSafely -Summary $summary -FailureMessages $failureMessages -Message "Unity log diagnostics failed: $($_.Exception.Message)"
            }
        }
        if ($null -eq $summary.build -and
            (Test-Path -LiteralPath $buildReportPath -PathType Leaf)) {
            try {
                $summary.build = Get-Content -LiteralPath $buildReportPath -Raw |
                    ConvertFrom-Json -DateKind String
            }
            catch {
                Add-FailureSafely -Summary $summary -FailureMessages $failureMessages -Message "Preserved C# report parse failed: $($_.Exception.Message)"
            }
        }

        try {
            $headFinally = (& git -C $ProjectPath rev-parse HEAD).Trim()
            if ($LASTEXITCODE -ne 0) { throw "git rev-parse HEAD failed." }
            $summary.git.headAfter = $headFinally
            if ($null -ne $summary.git.headBefore -and
                $headFinally -cne $summary.git.headBefore) {
                Add-FailureSafely -Summary $summary -FailureMessages $failureMessages -Message "HEAD changed during verification."
            }

            $finalState = @(Get-RepositoryStateSnapshot -RepositoryPath $ProjectPath)
            $summary.git.repositoryStateFinal = $finalState
            $summary.git.repositoryStateFingerprintFinal = Get-RepositoryStateFingerprint -Snapshot $finalState
            if ($null -ne $repositoryStateBefore) {
                $finalChangesFinally = @(Compare-RepositoryStateSnapshots -Before $repositoryStateBefore -After $finalState)
                $summary.git.finalChanges = $finalChangesFinally
                if ($finalChangesFinally.Count -gt 0) {
                    Add-FailureSafely -Summary $summary -FailureMessages $failureMessages -Message "Final tracked/untracked state differs from prebuild freeze: $(@($finalChangesFinally.path) -join ', ')"
                }
            }

            if ($null -ne $formalFilesBefore) {
                $formalAfterFinally = @(Get-FormalFileSnapshot -RepositoryPath $ProjectPath -RelativePaths $approvedFormalPaths)
                $formalChangesFinally = @(Compare-FileSnapshots -Before $formalFilesBefore -After $formalAfterFinally)
                $summary.git.formalFilesAfter = $formalAfterFinally
                $summary.git.formalFileChanges = $formalChangesFinally
                if ($formalChangesFinally.Count -gt 0) {
                    Add-FailureSafely -Summary $summary -FailureMessages $failureMessages -Message "Formal files changed during verification: $(@($formalChangesFinally.path) -join ', ')"
                }
            }

            if ($null -ne $generatedProjectFilesBefore) {
                $generatedFinal = @(Get-GeneratedProjectFileSnapshot -RepositoryPath $ProjectPath)
                $summary.git.generatedProjectFilesFinal = $generatedFinal
                $summary.git.generatedProjectFilesChangedFinal = @(Compare-FileSnapshots -Before $generatedProjectFilesBefore -After $generatedFinal)
            }
            Assert-IndexEmpty -RepositoryPath $ProjectPath
        }
        catch {
            Add-FailureSafely -Summary $summary -FailureMessages $failureMessages -Message "Final Git/formal boundary capture failed: $($_.Exception.Message)"
        }

        if ([string]$summary.sideEffects.recovery.status -ceq "InProgress") {
            $summary.sideEffects.recovery.status = "Failed"
            Add-FailureSafely -Summary $summary -FailureMessages $failureMessages -Message "CAS recovery did not reach a terminal status."
        }
        if ($failureMessages.Count -gt 0) {
            $summary.result = "Failed"
            $summary.error = @($failureMessages) -join " "
        }
        $finishedAt = [DateTimeOffset]::UtcNow
        $summary.finishedAtUtc = $finishedAt.ToString("o")
        $summary.finishedAtUnixMilliseconds = [long]$finishedAt.ToUnixTimeMilliseconds()
    }
    catch {
        Add-FailureSafely -Summary $summary -FailureMessages $failureMessages -Message "Finalization helper failed: $($_.Exception.Message)"
        try {
            $finishedAt = [DateTimeOffset]::UtcNow
            $summary.finishedAtUtc = $finishedAt.ToString("o")
            $summary.finishedAtUnixMilliseconds = [long]$finishedAt.ToUnixTimeMilliseconds()
        }
        catch { }
    }
    try {
        $summaryWriteEvidence = Write-FinalSummarySafely -Path $summaryPath -Summary $summary -FailureMessages $failureMessages
    }
    catch {
        Add-FailureSafely -Summary $summary -FailureMessages $failureMessages -Message "Top-level summary fail-safe unexpectedly threw: $($_.Exception.Message)"
        $summaryWriteEvidence = [pscustomobject]@{ written=$false; path=$summaryPath; usedFailSafe=$true; primaryError=$_.Exception.Message; failSafeError=$_.Exception.Message }
    }
}

if ($failureMessages.Count -eq 0 -and
    $summary.result -ceq "Passed" -and
    $null -ne $summaryWriteEvidence -and
    $summaryWriteEvidence.written) {
    Write-Host "ElementWar Windows64 verification passed. runId=$runId" -ForegroundColor Green
    Write-Host "Player: $playerPath"
    Write-Host "Summary: $($summaryWriteEvidence.path)"
    exit 0
}

Write-Host "ElementWar Windows64 verification failed. runId=$runId" -ForegroundColor Red
Write-Host "Error: $($summary.error)" -ForegroundColor Red
Write-Host "Summary: $($summaryWriteEvidence.path)"
exit 1

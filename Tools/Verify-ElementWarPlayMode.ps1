[CmdletBinding()]
param(
    [string]$ProjectPath = (Split-Path -Parent $PSScriptRoot),

    [string]$UnityExe = "E:\Unity\2022.3.62f2c1\Editor\Unity.exe",

    [string]$ArtifactsPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$expectedProjectAssemblyName = "Game.PlayModeTests.dll"
$expectedProjectFixtureClassName = "Game.Tests.PlayMode.Foundation.Runtime.RuntimeTickSchedulerPlayModeTests"
$expectedProjectTestMethods = @(
    "SubscribedHandlerRunsAfterFrameAdvancementAndStopsAfterUnsubscribe"
)
$freshnessTolerance = [TimeSpan]::FromSeconds(2)

if ($PSVersionTable.PSVersion.Major -lt 7) {
    throw "Verify-ElementWarPlayMode.ps1 requires PowerShell 7 or newer. Current version: $($PSVersionTable.PSVersion)"
}

function Resolve-NormalizedPath {
    param([Parameter(Mandatory)][string]$Path)

    return [System.IO.Path]::GetFullPath($Path)
}

function Clear-ExpectedArtifacts {
    param([Parameter(Mandatory)][string[]]$ArtifactPaths)

    $clearedArtifactNames = [System.Collections.Generic.List[string]]::new()
    foreach ($artifactPath in $ArtifactPaths) {
        if (Test-Path -LiteralPath $artifactPath -PathType Container) {
            throw "Expected artifact path is a directory; refusing to remove it: $artifactPath"
        }

        if (Test-Path -LiteralPath $artifactPath -PathType Leaf) {
            Remove-Item -LiteralPath $artifactPath -Force
            [void]$clearedArtifactNames.Add([System.IO.Path]::GetFileName($artifactPath))
        }

        if (Test-Path -LiteralPath $artifactPath) {
            throw "Expected artifact still exists after exact cleanup: $artifactPath"
        }
    }

    return $clearedArtifactNames.ToArray()
}

function ConvertTo-UtcTimestamp {
    param(
        [Parameter(Mandatory)][string]$Value,
        [Parameter(Mandatory)][string]$Label
    )

    $styles = [Globalization.DateTimeStyles]::AssumeUniversal -bor
        [Globalization.DateTimeStyles]::AdjustToUniversal
    try {
        return [DateTimeOffset]::Parse(
            $Value,
            [Globalization.CultureInfo]::InvariantCulture,
            $styles).ToUniversalTime()
    }
    catch {
        throw "Unable to parse $Label as a UTC timestamp: $Value"
    }
}

function Get-TestCounts {
    param([Parameter(Mandatory)][System.Xml.XmlElement]$Node)

    return [ordered]@{
        total = [int]$Node.GetAttribute("total")
        passed = [int]$Node.GetAttribute("passed")
        failed = [int]$Node.GetAttribute("failed")
        skipped = [int]$Node.GetAttribute("skipped")
        inconclusive = [int]$Node.GetAttribute("inconclusive")
    }
}

function Assert-FreshArtifactFile {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Label,
        [Parameter(Mandatory)][DateTimeOffset]$WindowStartUtc,
        [Parameter(Mandatory)][DateTimeOffset]$WindowEndUtc
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Label was not created: $Path"
    }

    $artifactFile = Get-Item -LiteralPath $Path
    if ($artifactFile.Length -le 0) {
        throw "$Label is empty: $Path"
    }

    $lastWriteTimeUtc = [DateTimeOffset]$artifactFile.LastWriteTimeUtc
    if ($lastWriteTimeUtc -lt $WindowStartUtc -or $lastWriteTimeUtc -gt $WindowEndUtc) {
        throw "$Label was not written during the current Unity process window: $Path"
    }

    return [ordered]@{
        path = $Path
        length = $artifactFile.Length
        lastWriteTimeUtc = $lastWriteTimeUtc.ToString("o")
    }
}

function Invoke-UnityProcess {
    param(
        [Parameter(Mandatory)][string]$ExecutablePath,
        [Parameter(Mandatory)][string[]]$Arguments
    )

    Write-Host "[PlayMode] $ExecutablePath $($Arguments -join ' ')" -ForegroundColor Cyan

    # ArgumentList preserves paths with spaces without relying on shell quoting rules.
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $ExecutablePath
    $startInfo.UseShellExecute = $false
    foreach ($argument in $Arguments) {
        [void]$startInfo.ArgumentList.Add($argument)
    }

    $startedAtUtc = [DateTimeOffset]::UtcNow
    $process = [System.Diagnostics.Process]::Start($startInfo)
    if ($null -eq $process) {
        throw "Failed to start Unity: $ExecutablePath"
    }

    $processId = $process.Id

    try {
        $process.WaitForExit()
        $finishedAtUtc = [DateTimeOffset]::UtcNow
        $exitCode = $process.ExitCode
        Write-Host "[PlayMode] Unity process id: $processId"
        Write-Host "[PlayMode] Unity exit code: $exitCode"

        return [pscustomobject]@{
            ProcessId = $processId
            StartedAtUtc = $startedAtUtc
            FinishedAtUtc = $finishedAtUtc
            ExitCode = $exitCode
        }
    }
    finally {
        $process.Dispose()
    }
}

function Assert-PlayModeResult {
    param(
        [Parameter(Mandatory)][string]$ResultPath,
        [Parameter(Mandatory)][string]$LogPath,
        [Parameter(Mandatory)][int]$ExitCode,
        [Parameter(Mandatory)][int]$UnityProcessId,
        [Parameter(Mandatory)][DateTimeOffset]$UnityStartedAtUtc,
        [Parameter(Mandatory)][DateTimeOffset]$UnityFinishedAtUtc,
        [Parameter(Mandatory)][string]$ExpectedAssemblyName,
        [Parameter(Mandatory)][string]$ExpectedFixtureClassName,
        [Parameter(Mandatory)][string[]]$ExpectedTestMethods,
        [Parameter(Mandatory)][TimeSpan]$FreshnessTolerance
    )

    $freshnessWindowStart = $UnityStartedAtUtc.Subtract($FreshnessTolerance)
    $freshnessWindowEnd = $UnityFinishedAtUtc.Add($FreshnessTolerance)
    $resultArtifact = Assert-FreshArtifactFile `
        -Path $ResultPath `
        -Label "PlayMode result XML" `
        -WindowStartUtc $freshnessWindowStart `
        -WindowEndUtc $freshnessWindowEnd
    $logArtifact = Assert-FreshArtifactFile `
        -Path $LogPath `
        -Label "PlayMode log" `
        -WindowStartUtc $freshnessWindowStart `
        -WindowEndUtc $freshnessWindowEnd

    [xml]$document = Get-Content -LiteralPath $ResultPath -Raw
    $run = $document.SelectSingleNode("/test-run")
    if ($null -eq $run) {
        throw "PlayMode result XML has no /test-run node: $ResultPath"
    }

    $xmlStartTimeUtc = ConvertTo-UtcTimestamp -Value $run.GetAttribute("start-time") -Label "XML start-time"
    $xmlEndTimeUtc = ConvertTo-UtcTimestamp -Value $run.GetAttribute("end-time") -Label "XML end-time"
    if ($xmlStartTimeUtc -lt $freshnessWindowStart -or
        $xmlEndTimeUtc -gt $freshnessWindowEnd -or
        $xmlEndTimeUtc -lt $xmlStartTimeUtc) {
        throw "PlayMode XML timestamps do not belong to the current Unity process window. XML: $ResultPath"
    }

    $allPlayModeTests = Get-TestCounts -Node $run

    Write-Host "[PlayMode] total=$($allPlayModeTests.total) passed=$($allPlayModeTests.passed) failed=$($allPlayModeTests.failed) skipped=$($allPlayModeTests.skipped) inconclusive=$($allPlayModeTests.inconclusive)"

    if ($allPlayModeTests.total -le 0) {
        throw "PlayMode ran zero tests. XML: $ResultPath"
    }

    $projectAssemblyNodes = @(
        $document.SelectNodes("//test-suite[@type='Assembly']") |
            Where-Object { $_.GetAttribute("name") -ceq $ExpectedAssemblyName }
    )
    if ($projectAssemblyNodes.Count -ne 1) {
        throw "Expected exactly one $ExpectedAssemblyName assembly in PlayMode XML, found $($projectAssemblyNodes.Count). XML: $ResultPath"
    }

    $projectAssembly = $projectAssemblyNodes[0]
    $projectTests = Get-TestCounts -Node $projectAssembly
    $projectTestCases = @($projectAssembly.SelectNodes(".//test-case"))
    if ($projectTestCases.Count -ne $projectTests.total) {
        throw "$ExpectedAssemblyName reports total=$($projectTests.total) but contains $($projectTestCases.Count) test-case nodes. XML: $ResultPath"
    }

    $pidProperties = @($projectAssembly.SelectNodes("./properties/property[@name='_PID']"))
    if ($pidProperties.Count -ne 1) {
        throw "$ExpectedAssemblyName does not contain exactly one _PID property. XML: $ResultPath"
    }

    $xmlProcessId = 0
    if (-not [int]::TryParse($pidProperties[0].GetAttribute("value"), [ref]$xmlProcessId)) {
        throw "$ExpectedAssemblyName contains a non-numeric _PID value. XML: $ResultPath"
    }

    if ($xmlProcessId -ne $UnityProcessId) {
        throw "PlayMode XML process id $xmlProcessId does not match current Unity process id $UnityProcessId. XML: $ResultPath"
    }

    $platformProperties = @($projectAssembly.SelectNodes("./properties/property[@name='platform']"))
    if ($platformProperties.Count -ne 1 -or $platformProperties[0].GetAttribute("value") -cne "PlayMode") {
        throw "$ExpectedAssemblyName does not contain exactly one platform=PlayMode property. XML: $ResultPath"
    }

    $fixtureNodes = @(
        $projectAssembly.SelectNodes(".//test-suite[@type='TestFixture']") |
            Where-Object {
                $_.GetAttribute("fullname") -ceq $ExpectedFixtureClassName -and
                $_.GetAttribute("classname") -ceq $ExpectedFixtureClassName
            }
    )
    if ($fixtureNodes.Count -ne 1) {
        throw "Expected exactly one $ExpectedFixtureClassName fixture in $ExpectedAssemblyName, found $($fixtureNodes.Count). XML: $ResultPath"
    }

    $fixtureTestCases = @($fixtureNodes[0].SelectNodes(".//test-case"))
    if ($fixtureTestCases.Count -ne $ExpectedTestMethods.Count) {
        throw "$ExpectedFixtureClassName contains $($fixtureTestCases.Count) tests; expected exactly $($ExpectedTestMethods.Count). XML: $ResultPath"
    }

    $expectedTestResults = @()
    foreach ($expectedMethod in $ExpectedTestMethods) {
        $expectedFullName = "$ExpectedFixtureClassName.$expectedMethod"
        $matchingTestCases = @(
            $fixtureTestCases |
                Where-Object {
                    $_.GetAttribute("fullname") -ceq $expectedFullName -and
                    $_.GetAttribute("classname") -ceq $ExpectedFixtureClassName -and
                    $_.GetAttribute("methodname") -ceq $expectedMethod
                }
        )

        if ($matchingTestCases.Count -ne 1) {
            throw "Expected exactly one project test $expectedFullName, found $($matchingTestCases.Count). XML: $ResultPath"
        }

        $testResult = $matchingTestCases[0].GetAttribute("result")
        if ($testResult -cne "Passed") {
            throw "Project test $expectedFullName did not pass; result=$testResult. XML: $ResultPath"
        }

        $expectedTestResults += [ordered]@{
            fullname = $expectedFullName
            methodName = $expectedMethod
            result = $testResult
        }
    }

    if ($run.GetAttribute("result") -cne "Passed" -or
        $allPlayModeTests.failed -gt 0 -or
        $ExitCode -ne 0) {
        throw "PlayMode failed. Unity exit code: $ExitCode. XML: $ResultPath. Log: $LogPath"
    }

    return [ordered]@{
        allPlayModeTests = $allPlayModeTests
        projectTests = [ordered]@{
            assemblyName = $ExpectedAssemblyName
            fixtureClassName = $ExpectedFixtureClassName
            total = $projectTests.total
            passed = $projectTests.passed
            failed = $projectTests.failed
            skipped = $projectTests.skipped
            inconclusive = $projectTests.inconclusive
            expectedTests = $expectedTestResults
        }
        freshness = [ordered]@{
            resultXml = $resultArtifact
            log = $logArtifact
            xmlStartTimeUtc = $xmlStartTimeUtc.ToString("o")
            xmlEndTimeUtc = $xmlEndTimeUtc.ToString("o")
            xmlProcessId = $xmlProcessId
            toleranceSeconds = $FreshnessTolerance.TotalSeconds
        }
    }
}

$ProjectPath = Resolve-NormalizedPath -Path $ProjectPath
$UnityExe = Resolve-NormalizedPath -Path $UnityExe

if (-not (Test-Path -LiteralPath $ProjectPath -PathType Container)) {
    throw "Project path does not exist: $ProjectPath"
}

$projectVersionPath = Join-Path $ProjectPath "ProjectSettings\ProjectVersion.txt"
$manifestPath = Join-Path $ProjectPath "Packages\manifest.json"
if (-not (Test-Path -LiteralPath $projectVersionPath -PathType Leaf) -or
    -not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "Path is not a Unity project root: $ProjectPath"
}

if (-not (Test-Path -LiteralPath $UnityExe -PathType Leaf)) {
    throw "Unity editor executable does not exist: $UnityExe"
}

$projectVersion = (Get-Content -LiteralPath $projectVersionPath -Raw).Trim()
if ($projectVersion -notmatch "(?m)^m_EditorVersion:\s*2022\.3\.62f2c1\s*$") {
    throw "Expected ElementWar Unity 2022.3.62f2c1, but ProjectVersion.txt contains: $projectVersion"
}

$lockPath = Join-Path $ProjectPath "Temp\UnityLockfile"
if (Test-Path -LiteralPath $lockPath) {
    throw "Unity appears to have this project open ($lockPath exists). Save and close the Editor before command-line verification."
}

if ([string]::IsNullOrWhiteSpace($ArtifactsPath)) {
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $ArtifactsPath = Join-Path $ProjectPath "Logs\Verification\$timestamp-playmode"
}

$ArtifactsPath = Resolve-NormalizedPath -Path $ArtifactsPath
New-Item -ItemType Directory -Path $ArtifactsPath -Force | Out-Null

$resultPath = Join-Path $ArtifactsPath "PlayMode-results.xml"
$logPath = Join-Path $ArtifactsPath "PlayMode.log"
$summaryPath = Join-Path $ArtifactsPath "PlayMode-verification-summary.json"
$expectedArtifactPaths = @($resultPath, $logPath, $summaryPath)
$clearedArtifacts = @(Clear-ExpectedArtifacts -ArtifactPaths $expectedArtifactPaths)
$verificationStartedAt = [DateTimeOffset]::Now
$summary = [ordered]@{
    mode = "PlayMode"
    projectPath = $ProjectPath
    unityExe = $UnityExe
    unityProjectVersion = $projectVersion
    startedAt = $verificationStartedAt.ToString("o")
    finishedAt = $null
    result = "Running"
    clearedArtifacts = $clearedArtifacts
    unityProcessId = $null
    unityStartedAtUtc = $null
    unityFinishedAtUtc = $null
    unityExitCode = $null
    allPlayModeTests = $null
    projectTests = $null
    freshness = $null
    resultXml = $resultPath
    log = $logPath
    error = $null
}

try {
    $arguments = @(
        "-batchmode",
        "-projectPath", $ProjectPath,
        "-runTests",
        "-testPlatform", "PlayMode",
        "-testResults", $resultPath,
        "-logFile", $logPath
    )

    $unityRun = Invoke-UnityProcess -ExecutablePath $UnityExe -Arguments $arguments
    $summary.unityProcessId = $unityRun.ProcessId
    $summary.unityStartedAtUtc = $unityRun.StartedAtUtc.ToString("o")
    $summary.unityFinishedAtUtc = $unityRun.FinishedAtUtc.ToString("o")
    $summary.unityExitCode = $unityRun.ExitCode

    $validation = Assert-PlayModeResult `
        -ResultPath $resultPath `
        -LogPath $logPath `
        -ExitCode $unityRun.ExitCode `
        -UnityProcessId $unityRun.ProcessId `
        -UnityStartedAtUtc $unityRun.StartedAtUtc `
        -UnityFinishedAtUtc $unityRun.FinishedAtUtc `
        -ExpectedAssemblyName $expectedProjectAssemblyName `
        -ExpectedFixtureClassName $expectedProjectFixtureClassName `
        -ExpectedTestMethods $expectedProjectTestMethods `
        -FreshnessTolerance $freshnessTolerance

    $summary.allPlayModeTests = $validation.allPlayModeTests
    $summary.projectTests = $validation.projectTests
    $summary.freshness = $validation.freshness
    $summary.result = "Passed"
    Write-Host "ElementWar PlayMode verification passed. Artifacts: $ArtifactsPath" -ForegroundColor Green
}
catch {
    $summary.result = "Failed"
    $summary.error = $_.Exception.Message
    Write-Host "ElementWar PlayMode verification failed: $($summary.error)" -ForegroundColor Red
    throw
}
finally {
    $summary.finishedAt = [DateTimeOffset]::Now.ToString("o")
    $summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $summaryPath -Encoding utf8
    Write-Host "Summary: $summaryPath"
}

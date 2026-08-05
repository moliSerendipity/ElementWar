[CmdletBinding()]
param(
    [string]$ProjectPath = (Split-Path -Parent $PSScriptRoot),

    [string]$UnityExe = "E:\Unity\2022.3.62f2c1\Editor\Unity.exe",

    [string]$ArtifactsPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$expectedProjectAssemblyName = "Game.EditModeTests.dll"
$expectedProjectFixtureClassName = "Game.Tests.EditMode.Foundation.Events.GameEventBusTests"
$expectedProjectTestMethods = @(
    "PublishInvokesMatchingHandlerSynchronouslyWithPayload",
    "PublishInvokesOnlyHandlersForMatchingEventType",
    "UnsubscribePreventsLaterDelivery",
    "PublishWithoutSubscribersDoesNotThrow"
)
$freshnessTolerance = [TimeSpan]::FromSeconds(2)

if ($PSVersionTable.PSVersion.Major -lt 7) {
    throw "Verify-ElementWarEditMode.ps1 requires PowerShell 7 or newer. Current version: $($PSVersionTable.PSVersion)"
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

function Invoke-UnityProcess {
    param(
        [Parameter(Mandatory)][string]$ExecutablePath,
        [Parameter(Mandatory)][string[]]$Arguments
    )

    Write-Host "[EditMode] $ExecutablePath $($Arguments -join ' ')" -ForegroundColor Cyan

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
        Write-Host "[EditMode] Unity process id: $processId"
        Write-Host "[EditMode] Unity exit code: $exitCode"

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

function Assert-EditModeResult {
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

    if (-not (Test-Path -LiteralPath $ResultPath -PathType Leaf)) {
        throw "EditMode did not create a test-result XML. Unity exit code: $ExitCode. Log: $LogPath"
    }

    $resultFile = Get-Item -LiteralPath $ResultPath
    $resultLastWriteTimeUtc = [DateTimeOffset]$resultFile.LastWriteTimeUtc
    $freshnessWindowStart = $UnityStartedAtUtc.Subtract($FreshnessTolerance)
    $freshnessWindowEnd = $UnityFinishedAtUtc.Add($FreshnessTolerance)
    if ($resultLastWriteTimeUtc -lt $freshnessWindowStart -or
        $resultLastWriteTimeUtc -gt $freshnessWindowEnd) {
        throw "EditMode result XML was not written during the current Unity process window. XML: $ResultPath"
    }

    [xml]$document = Get-Content -LiteralPath $ResultPath -Raw
    $run = $document.SelectSingleNode("/test-run")
    if ($null -eq $run) {
        throw "EditMode result XML has no /test-run node: $ResultPath"
    }

    $xmlStartTimeUtc = ConvertTo-UtcTimestamp -Value $run.GetAttribute("start-time") -Label "XML start-time"
    $xmlEndTimeUtc = ConvertTo-UtcTimestamp -Value $run.GetAttribute("end-time") -Label "XML end-time"
    if ($xmlStartTimeUtc -lt $freshnessWindowStart -or
        $xmlEndTimeUtc -gt $freshnessWindowEnd -or
        $xmlEndTimeUtc -lt $xmlStartTimeUtc) {
        throw "EditMode XML timestamps do not belong to the current Unity process window. XML: $ResultPath"
    }

    $allEditModeTests = Get-TestCounts -Node $run

    Write-Host "[EditMode] total=$($allEditModeTests.total) passed=$($allEditModeTests.passed) failed=$($allEditModeTests.failed) skipped=$($allEditModeTests.skipped) inconclusive=$($allEditModeTests.inconclusive)"

    if ($allEditModeTests.total -le 0) {
        throw "EditMode ran zero tests. XML: $ResultPath"
    }

    $projectAssemblyNodes = @(
        $document.SelectNodes("//test-suite[@type='Assembly']") |
            Where-Object { $_.GetAttribute("name") -ceq $ExpectedAssemblyName }
    )
    if ($projectAssemblyNodes.Count -ne 1) {
        throw "Expected exactly one $ExpectedAssemblyName assembly in EditMode XML, found $($projectAssemblyNodes.Count). XML: $ResultPath"
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
        throw "EditMode XML process id $xmlProcessId does not match current Unity process id $UnityProcessId. XML: $ResultPath"
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
        $allEditModeTests.failed -gt 0 -or
        $ExitCode -ne 0) {
        throw "EditMode failed. Unity exit code: $ExitCode. XML: $ResultPath. Log: $LogPath"
    }

    return [ordered]@{
        allEditModeTests = $allEditModeTests
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
            resultXmlLastWriteTimeUtc = $resultLastWriteTimeUtc.ToString("o")
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
    $ArtifactsPath = Join-Path $ProjectPath "Logs\Verification\$timestamp"
}

$ArtifactsPath = Resolve-NormalizedPath -Path $ArtifactsPath
New-Item -ItemType Directory -Path $ArtifactsPath -Force | Out-Null

$resultPath = Join-Path $ArtifactsPath "EditMode-results.xml"
$logPath = Join-Path $ArtifactsPath "EditMode.log"
$summaryPath = Join-Path $ArtifactsPath "verification-summary.json"
$expectedArtifactPaths = @($resultPath, $logPath, $summaryPath)
$clearedArtifacts = @(Clear-ExpectedArtifacts -ArtifactPaths $expectedArtifactPaths)
$verificationStartedAt = [DateTimeOffset]::Now
$summary = [ordered]@{
    mode = "EditMode"
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
    allEditModeTests = $null
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
        "-testPlatform", "EditMode",
        "-testResults", $resultPath,
        "-logFile", $logPath
    )

    $unityRun = Invoke-UnityProcess -ExecutablePath $UnityExe -Arguments $arguments
    $summary.unityProcessId = $unityRun.ProcessId
    $summary.unityStartedAtUtc = $unityRun.StartedAtUtc.ToString("o")
    $summary.unityFinishedAtUtc = $unityRun.FinishedAtUtc.ToString("o")
    $summary.unityExitCode = $unityRun.ExitCode

    $validation = Assert-EditModeResult `
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

    $summary.allEditModeTests = $validation.allEditModeTests
    $summary.projectTests = $validation.projectTests
    $summary.freshness = $validation.freshness
    $summary.result = "Passed"
    Write-Host "ElementWar EditMode verification passed. Artifacts: $ArtifactsPath" -ForegroundColor Green
}
catch {
    $summary.result = "Failed"
    $summary.error = $_.Exception.Message
    Write-Host "ElementWar EditMode verification failed: $($summary.error)" -ForegroundColor Red
    throw
}
finally {
    $summary.finishedAt = [DateTimeOffset]::Now.ToString("o")
    $summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $summaryPath -Encoding utf8
    Write-Host "Summary: $summaryPath"
}

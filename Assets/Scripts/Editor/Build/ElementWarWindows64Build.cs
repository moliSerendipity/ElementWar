#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Process = System.Diagnostics.Process;

namespace Game.Editor.Build
{
    /// <summary>
    /// 为独立 PowerShell 验证器提供受约束的 Bootstrap-only Windows64 构建入口。
    /// </summary>
    public static class ElementWarWindows64Build
    {
        private const string ApprovedScenePath = "Assets/Scenes/Bootstrap/Bootstrap.unity";
        private const string ApprovedSceneGuid = "d5ba7b6c1b4ae954b9bbab4fb20481a2";
        private const string BuildsRootRelativePath = "Builds/Windows64";
        private const string EvidenceRootRelativePath = "Logs/Verification";
        private const string AddressableSettingsPath =
            "Assets/AddressableAssetsData/AddressableAssetSettings.asset";
        private const string AddressablesBuildOptionPropertyName =
            "m_BuildAddressablesWithPlayerBuild";
        private const string RequiredAddressablesBuildOptionName = "BuildWithPlayer";
        private const int RequiredAddressablesBuildOptionValue = 1;
        private const string PlayerFileName = "ElementWar.exe";
        private const string ReportFileName = "Windows64-build-report.json";
        private const string RunIdArgument = "-elementWarRunId";
        private const string PlayerPathArgument = "-elementWarPlayerPath";
        private const string ReportPathArgument = "-elementWarBuildReportPath";

        private static readonly Regex runIdPattern = new Regex(
            @"^[0-9]{8}-[0-9]{9}Z-[0-9a-f]{8}$",
            RegexOptions.CultureInvariant);

        /// <summary>
        /// 校验命令行契约，构建批准的 Bootstrap 场景，并为成功或失败写出进程绑定报告。
        /// </summary>
        public static void Build()
        {
            DateTimeOffset entryStartedAt = DateTimeOffset.UtcNow;
            BuildEvidence evidence = new BuildEvidence
            {
                status = "Running",
                unityProcessId = Process.GetCurrentProcess().Id,
                unityVersion = Application.unityVersion,
                entryStartedAtUtc = entryStartedAt.ToString("o", CultureInfo.InvariantCulture),
                entryStartedAtUnixMilliseconds = entryStartedAt.ToUnixTimeMilliseconds()
            };

            string validatedReportPath = null;
            Exception failure = null;

            try
            {
                string projectRoot = ResolveProjectRoot();
                string[] commandLineArguments = Environment.GetCommandLineArgs();
                string runId = GetRequiredArgument(commandLineArguments, RunIdArgument);
                string requestedPlayerPath = GetRequiredArgument(commandLineArguments, PlayerPathArgument);
                string requestedReportPath = GetRequiredArgument(commandLineArguments, ReportPathArgument);

                ValidateRunId(runId);

                string buildsRoot = Path.GetFullPath(Path.Combine(projectRoot, BuildsRootRelativePath));
                string evidenceRoot = Path.GetFullPath(Path.Combine(projectRoot, EvidenceRootRelativePath));
                string expectedOutputDirectory = Path.GetFullPath(Path.Combine(buildsRoot, runId));
                string expectedPlayerPath = Path.GetFullPath(Path.Combine(expectedOutputDirectory, PlayerFileName));
                string expectedEvidenceDirectory = Path.GetFullPath(
                    Path.Combine(evidenceRoot, $"{runId}-windows64"));
                string expectedReportPath = Path.GetFullPath(
                    Path.Combine(expectedEvidenceDirectory, ReportFileName));

                string playerPath = NormalizeApprovedPath(
                    requestedPlayerPath,
                    buildsRoot,
                    expectedPlayerPath,
                    "Player output");
                validatedReportPath = NormalizeApprovedPath(
                    requestedReportPath,
                    evidenceRoot,
                    expectedReportPath,
                    "C# build report");

                evidence.runId = runId;
                evidence.projectRoot = projectRoot;
                evidence.playerPath = playerPath;
                evidence.reportPath = validatedReportPath;

                if (!Directory.Exists(expectedEvidenceDirectory))
                {
                    throw new BuildFailedException(
                        $"Approved evidence directory does not exist: {expectedEvidenceDirectory}");
                }

                AssertNoReparsePointInPath(
                    expectedEvidenceDirectory,
                    "Approved evidence directory");

                if (File.Exists(validatedReportPath) || Directory.Exists(validatedReportPath))
                {
                    throw new BuildFailedException(
                        $"C# build report path already exists; refusing to overwrite it: {validatedReportPath}");
                }

                if (Directory.Exists(expectedOutputDirectory) || File.Exists(expectedOutputDirectory))
                {
                    throw new BuildFailedException(
                        $"Windows64 output path already exists; refusing to overwrite it: {expectedOutputDirectory}");
                }

                SceneEvidence approvedScene = ValidateApprovedScene(projectRoot);
                evidence.scenes = new[] { approvedScene };

                if (!BuildPipeline.IsBuildTargetSupported(
                        BuildTargetGroup.Standalone,
                        BuildTarget.StandaloneWindows64))
                {
                    throw new BuildFailedException(
                        "StandaloneWindows64 is not supported by the current Unity installation.");
                }

                evidence.scriptingBackend = PlayerSettings
                    .GetScriptingBackend(NamedBuildTarget.Standalone)
                    .ToString();
                evidence.addressablesBuildWithPlayer = ValidateAddressablesBuildWithPlayer();

                Directory.CreateDirectory(expectedOutputDirectory);
                AssertNoReparsePointInPath(
                    expectedOutputDirectory,
                    "Windows64 output directory");

                BuildPlayerOptions options = new BuildPlayerOptions
                {
                    scenes = new[] { ApprovedScenePath },
                    locationPathName = playerPath,
                    targetGroup = BuildTargetGroup.Standalone,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None
                };

                BuildReport report;
                DateTimeOffset buildPipelineStartedAt = DateTimeOffset.UtcNow;
                evidence.buildPipelineStartedAtUtc = buildPipelineStartedAt.ToString(
                    "o",
                    CultureInfo.InvariantCulture);
                evidence.buildPipelineStartedAtUnixMilliseconds =
                    buildPipelineStartedAt.ToUnixTimeMilliseconds();

                try
                {
                    report = BuildPipeline.BuildPlayer(options);
                }
                finally
                {
                    DateTimeOffset buildPipelineFinishedAt = DateTimeOffset.UtcNow;
                    evidence.buildPipelineFinishedAtUtc = buildPipelineFinishedAt.ToString(
                        "o",
                        CultureInfo.InvariantCulture);
                    evidence.buildPipelineFinishedAtUnixMilliseconds =
                        buildPipelineFinishedAt.ToUnixTimeMilliseconds();
                }

                PopulateBuildReportEvidence(evidence, report);
                ValidateBuildSummary(evidence, playerPath);

                evidence.status = "Succeeded";
                Debug.Log(
                    $"ElementWar Windows64 build succeeded. runId={runId}, " +
                    $"backend={evidence.scriptingBackend}, warnings={evidence.totalWarnings}, " +
                    $"size={evidence.totalSizeBytes}, output='{playerPath}'.");
            }
            catch (Exception exception)
            {
                failure = exception;
                evidence.status = "Failed";
                evidence.errorType = exception.GetType().FullName;
                evidence.errorMessage = exception.Message;
            }

            DateTimeOffset entryFinishedAt = DateTimeOffset.UtcNow;
            evidence.entryFinishedAtUtc = entryFinishedAt.ToString(
                "o",
                CultureInfo.InvariantCulture);
            evidence.entryFinishedAtUnixMilliseconds = entryFinishedAt.ToUnixTimeMilliseconds();

            if (!string.IsNullOrWhiteSpace(validatedReportPath))
            {
                try
                {
                    WriteNewJsonFile(validatedReportPath, evidence);
                }
                catch (Exception reportException)
                {
                    string reportFailure =
                        $"Failed to write C# build report '{validatedReportPath}': {reportException.Message}";
                    if (failure == null)
                    {
                        failure = new BuildFailedException(reportFailure);
                        evidence.status = "Failed";
                        evidence.errorType = reportException.GetType().FullName;
                        evidence.errorMessage = reportFailure;
                    }
                    else
                    {
                        evidence.errorMessage = $"{evidence.errorMessage} Report error: {reportFailure}";
                    }
                }
            }

            if (failure != null)
            {
                Debug.LogError(
                    $"ElementWar Windows64 build failed. runId='{evidence.runId}', " +
                    $"error='{evidence.errorMessage}'.");
                throw new BuildFailedException(evidence.errorMessage);
            }
        }

        private static string ResolveProjectRoot()
        {
            DirectoryInfo projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new BuildFailedException("Could not resolve the Unity project root.");
            }

            return Path.GetFullPath(projectRoot.FullName);
        }

        private static string GetRequiredArgument(string[] _arguments, string _argumentName)
        {
            string value = null;
            int matches = 0;

            for (int index = 0; index < _arguments.Length; index++)
            {
                if (!string.Equals(_arguments[index], _argumentName, StringComparison.Ordinal))
                {
                    continue;
                }

                matches++;
                if (index + 1 >= _arguments.Length ||
                    string.IsNullOrWhiteSpace(_arguments[index + 1]))
                {
                    throw new BuildFailedException(
                        $"Command-line argument '{_argumentName}' has no value.");
                }

                value = _arguments[index + 1];
            }

            if (matches != 1)
            {
                throw new BuildFailedException(
                    $"Expected exactly one '{_argumentName}' argument, found {matches}.");
            }

            return value;
        }

        private static void ValidateRunId(string _runId)
        {
            if (!runIdPattern.IsMatch(_runId))
            {
                throw new BuildFailedException(
                    $"Run id does not match the approved format: '{_runId}'.");
            }
        }

        private static string NormalizeApprovedPath(
            string _requestedPath,
            string _approvedRoot,
            string _expectedPath,
            string _label)
        {
            if (!Path.IsPathFullyQualified(_requestedPath))
            {
                throw new BuildFailedException($"{_label} must be absolute: {_requestedPath}");
            }

            string normalizedPath = Path.GetFullPath(_requestedPath);
            string normalizedRoot = Path.GetFullPath(_approvedRoot);
            string rootPrefix = normalizedRoot.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            if (!normalizedPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new BuildFailedException(
                    $"{_label} is outside its approved root '{normalizedRoot}': {normalizedPath}");
            }

            if (!string.Equals(
                    normalizedPath,
                    Path.GetFullPath(_expectedPath),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new BuildFailedException(
                    $"{_label} does not match the approved run path. " +
                    $"Expected '{_expectedPath}', received '{normalizedPath}'.");
            }

            AssertNoReparsePointInPath(normalizedRoot, $"{_label} approved root");
            AssertNoReparsePointInPath(normalizedPath, _label);

            return normalizedPath;
        }

        private static void AssertNoReparsePointInPath(string _path, string _label)
        {
            string normalizedPath = Path.GetFullPath(_path);
            string currentPath = File.Exists(normalizedPath) || Directory.Exists(normalizedPath)
                ? normalizedPath
                : Path.GetDirectoryName(normalizedPath);

            while (!string.IsNullOrWhiteSpace(currentPath))
            {
                if (File.Exists(currentPath) || Directory.Exists(currentPath))
                {
                    FileAttributes attributes = File.GetAttributes(currentPath);
                    if ((attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        throw new BuildFailedException(
                            $"{_label} contains a reparse point in its path: {currentPath}");
                    }
                }

                string parentPath = Path.GetDirectoryName(currentPath);
                if (string.IsNullOrWhiteSpace(parentPath) ||
                    string.Equals(parentPath, currentPath, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                currentPath = parentPath;
            }
        }

        private static AddressablesBuildEvidence ValidateAddressablesBuildWithPlayer()
        {
            UnityEngine.Object settingsAsset = AssetDatabase.LoadMainAssetAtPath(
                AddressableSettingsPath);
            if (settingsAsset == null)
            {
                throw new BuildFailedException(
                    $"Addressables settings asset is missing: {AddressableSettingsPath}");
            }

            SerializedObject serializedSettings = new SerializedObject(settingsAsset);
            serializedSettings.UpdateIfRequiredOrScript();
            SerializedProperty buildOption = serializedSettings.FindProperty(
                AddressablesBuildOptionPropertyName);
            if (buildOption == null || buildOption.propertyType != SerializedPropertyType.Enum)
            {
                throw new BuildFailedException(
                    $"Addressables build option property is missing or is not an enum: " +
                    $"{AddressablesBuildOptionPropertyName}");
            }

            int enumIndex = buildOption.enumValueIndex;
            string[] enumNames = buildOption.enumNames ?? Array.Empty<string>();
            string enumName = enumIndex >= 0 && enumIndex < enumNames.Length
                ? enumNames[enumIndex]
                : null;
            int serializedValue = buildOption.intValue;

            AddressablesBuildEvidence result = new AddressablesBuildEvidence
            {
                settingsPath = AddressableSettingsPath,
                settingsAssetType = settingsAsset.GetType().FullName,
                propertyName = AddressablesBuildOptionPropertyName,
                serializedValue = serializedValue,
                enumValueIndex = enumIndex,
                enumName = enumName,
                effectiveOption = enumName
            };

            if (serializedValue != RequiredAddressablesBuildOptionValue ||
                !string.Equals(
                    enumName,
                    RequiredAddressablesBuildOptionName,
                    StringComparison.Ordinal))
            {
                throw new BuildFailedException(
                    "Addressables must explicitly use BuildWithPlayer. " +
                    $"Actual serializedValue={serializedValue}, enumName='{enumName}'.");
            }

            return result;
        }

        private static SceneEvidence ValidateApprovedScene(string _projectRoot)
        {
            string sceneAbsolutePath = Path.GetFullPath(Path.Combine(
                _projectRoot,
                ApprovedScenePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!File.Exists(sceneAbsolutePath))
            {
                throw new BuildFailedException(
                    $"Approved Bootstrap scene does not exist: {sceneAbsolutePath}");
            }

            string actualGuid = AssetDatabase.AssetPathToGUID(ApprovedScenePath);
            if (!string.Equals(actualGuid, ApprovedSceneGuid, StringComparison.OrdinalIgnoreCase))
            {
                throw new BuildFailedException(
                    $"Approved Bootstrap GUID mismatch. Expected '{ApprovedSceneGuid}', " +
                    $"actual '{actualGuid}'.");
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ApprovedScenePath);
            if (sceneAsset == null)
            {
                throw new BuildFailedException(
                    $"Unity could not load the approved Bootstrap scene asset: {ApprovedScenePath}");
            }

            return new SceneEvidence
            {
                path = ApprovedScenePath,
                guid = actualGuid
            };
        }

        private static void PopulateBuildReportEvidence(
            BuildEvidence _evidence,
            BuildReport _report)
        {
            if (_report == null)
            {
                throw new BuildFailedException("BuildPipeline.BuildPlayer returned no BuildReport.");
            }

            BuildSummary summary = _report.summary;
            _evidence.buildResult = summary.result.ToString();
            _evidence.buildTarget = summary.platform.ToString();
            _evidence.buildTargetGroup = summary.platformGroup.ToString();
            _evidence.buildOptions = summary.options.ToString();
            _evidence.buildSummaryStartedAtRaw = summary.buildStartedAt.ToString(
                "o",
                CultureInfo.InvariantCulture);
            _evidence.buildSummaryStartedAtKind = summary.buildStartedAt.Kind.ToString();
            _evidence.buildSummaryEndedAtRaw = summary.buildEndedAt.ToString(
                "o",
                CultureInfo.InvariantCulture);
            _evidence.buildSummaryEndedAtKind = summary.buildEndedAt.Kind.ToString();
            _evidence.buildSummaryDurationSeconds = summary.totalTime.TotalSeconds;
            _evidence.buildOutputPath = string.IsNullOrWhiteSpace(summary.outputPath)
                ? summary.outputPath
                : Path.GetFullPath(summary.outputPath);
            _evidence.totalWarnings = summary.totalWarnings;
            _evidence.totalErrors = summary.totalErrors;
            _evidence.totalSizeBytes = checked((long)summary.totalSize);

            BuildFile[] buildFiles = _report.GetFiles() ?? Array.Empty<BuildFile>();
            _evidence.files = buildFiles
                .Select(file => new BuildFileEvidence
                {
                    path = string.IsNullOrWhiteSpace(file.path)
                        ? file.path
                        : Path.GetFullPath(file.path),
                    role = file.role,
                    sizeBytes = checked((long)file.size)
                })
                .OrderBy(file => file.path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static void ValidateBuildSummary(BuildEvidence _evidence, string _playerPath)
        {
            if (!string.Equals(_evidence.buildResult, BuildResult.Succeeded.ToString(), StringComparison.Ordinal))
            {
                throw new BuildFailedException(
                    $"Windows64 BuildReport result is '{_evidence.buildResult}', " +
                    $"errors={_evidence.totalErrors}, warnings={_evidence.totalWarnings}.");
            }

            if (!string.Equals(
                    _evidence.buildTarget,
                    BuildTarget.StandaloneWindows64.ToString(),
                    StringComparison.Ordinal))
            {
                throw new BuildFailedException(
                    $"BuildReport platform is '{_evidence.buildTarget}', expected StandaloneWindows64.");
            }

            if (!string.Equals(
                    Path.GetFullPath(_evidence.buildOutputPath),
                    Path.GetFullPath(_playerPath),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new BuildFailedException(
                    $"BuildReport output path '{_evidence.buildOutputPath}' does not match '{_playerPath}'.");
            }

            if (_evidence.totalErrors != 0)
            {
                throw new BuildFailedException(
                    $"BuildReport recorded {_evidence.totalErrors} errors.");
            }

            if (_evidence.totalSizeBytes <= 0 || _evidence.files.Length == 0)
            {
                throw new BuildFailedException(
                    "BuildReport succeeded but did not report a non-empty output.");
            }
        }

        private static void WriteNewJsonFile(string _path, BuildEvidence _evidence)
        {
            if (File.Exists(_path) || Directory.Exists(_path))
            {
                throw new IOException($"Refusing to overwrite build report path: {_path}");
            }

            string directory = Path.GetDirectoryName(_path);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException(
                    $"Build report directory does not exist: {directory}");
            }

            string temporaryPath = $"{_path}.tmp-{_evidence.unityProcessId}";
            if (File.Exists(temporaryPath) || Directory.Exists(temporaryPath))
            {
                throw new IOException(
                    $"Refusing to overwrite temporary build report path: {temporaryPath}");
            }

            string json = JsonUtility.ToJson(_evidence, true);
            File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));
            File.Move(temporaryPath, _path);
        }

        [Serializable]
        private sealed class BuildEvidence
        {
            [SerializeField] internal int schemaVersion = 2;
            [SerializeField] internal string status;
            [SerializeField] internal string runId;
            [SerializeField] internal string projectRoot;
            [SerializeField] internal int unityProcessId;
            [SerializeField] internal string unityVersion;
            [SerializeField] internal string scriptingBackend;
            [SerializeField] internal AddressablesBuildEvidence addressablesBuildWithPlayer;
            [SerializeField] internal string entryStartedAtUtc;
            [SerializeField] internal long entryStartedAtUnixMilliseconds;
            [SerializeField] internal string entryFinishedAtUtc;
            [SerializeField] internal long entryFinishedAtUnixMilliseconds;
            [SerializeField] internal string buildPipelineStartedAtUtc;
            [SerializeField] internal long buildPipelineStartedAtUnixMilliseconds;
            [SerializeField] internal string buildPipelineFinishedAtUtc;
            [SerializeField] internal long buildPipelineFinishedAtUnixMilliseconds;
            [SerializeField] internal string playerPath;
            [SerializeField] internal string reportPath;
            [SerializeField] internal SceneEvidence[] scenes = Array.Empty<SceneEvidence>();
            [SerializeField] internal string buildResult;
            [SerializeField] internal string buildTarget;
            [SerializeField] internal string buildTargetGroup;
            [SerializeField] internal string buildOptions;
            [SerializeField] internal string buildSummaryStartedAtRaw;
            [SerializeField] internal string buildSummaryStartedAtKind;
            [SerializeField] internal string buildSummaryEndedAtRaw;
            [SerializeField] internal string buildSummaryEndedAtKind;
            [SerializeField] internal double buildSummaryDurationSeconds;
            [SerializeField] internal string buildOutputPath;
            [SerializeField] internal int totalWarnings;
            [SerializeField] internal int totalErrors;
            [SerializeField] internal long totalSizeBytes;
            [SerializeField] internal BuildFileEvidence[] files = Array.Empty<BuildFileEvidence>();
            [SerializeField] internal string errorType;
            [SerializeField] internal string errorMessage;
        }

        [Serializable]
        private sealed class AddressablesBuildEvidence
        {
            [SerializeField] internal string settingsPath;
            [SerializeField] internal string settingsAssetType;
            [SerializeField] internal string propertyName;
            [SerializeField] internal int serializedValue;
            [SerializeField] internal int enumValueIndex;
            [SerializeField] internal string enumName;
            [SerializeField] internal string effectiveOption;
        }

        [Serializable]
        private sealed class SceneEvidence
        {
            [SerializeField] internal string path;
            [SerializeField] internal string guid;
        }

        [Serializable]
        private sealed class BuildFileEvidence
        {
            [SerializeField] internal string path;
            [SerializeField] internal string role;
            [SerializeField] internal long sizeBytes;
        }
    }
}
#endif

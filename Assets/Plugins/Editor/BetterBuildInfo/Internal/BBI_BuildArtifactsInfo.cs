using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Better.BuildInfo.Internal
{
    internal class BuildArtifactsInfo
    {
        public struct SizePair
        {
            public long compressed;
            public long uncompressed;

            public SizePair(long compressed, long uncompressed)
                : this()
            {
                this.compressed = compressed;
                this.uncompressed = uncompressed;
            }

            public static implicit operator SizePair(long value)
            {
                return new SizePair()
                {
                    compressed = 0,
                    uncompressed = value
                };
            }
        }

        private static readonly HashSet<string> s_unityResourcesNames = new HashSet<string>(new[]
        {
            "unity default resources",
            "Resources/unity default resources",
            "Resources/unity_builtin_extra"
        });

        public List<SizePair> sceneSizes = new List<SizePair>();
        public Dictionary<string, SizePair> managedModules = new Dictionary<string, SizePair>();
        public SizePair totalSize;
        public SizePair runtimeSize;
        public long streamingAssetsSize;

        public Dictionary<string, SizePair> unityResources = new Dictionary<string, SizePair>();
        public Dictionary<string, SizePair> otherAssets = new Dictionary<string, SizePair>();

        //private BuildArtifactsInfo(SizePair totalSize, long streamingAssetsSize, List<SizePair> scenes, Dictionary<string, SizePair> modules)
        //{

        //}

        public static BuildArtifactsInfo Create(BuildTarget buildTarget, string buildPath, string standaloneWinDataDirectoryOverride = null)
        {
            switch (buildTarget)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return CreateForStandaloneWin(buildPath, standaloneWinDataDirectoryOverride);

                case BuildTarget.Android:
                    return CreateForAndroid(buildPath, PlayerSettings.Android.useAPKExpansionFiles);

                case UnityVersionAgnostic.iOSBuildTarget:
                    return CreateForIOS(buildPath);

#if !UNITY_4_7
                case BuildTarget.WebGL:
                    return CreateForWebGL(buildPath);
#endif

                default:
                    if (UnityVersionAgnostic.IsOSXBuildTarget(buildTarget))
                        return CreateForStandaloneMac(buildPath);

                    throw new NotSupportedException();
            }
        }

#if !UNITY_4_7
        private static BuildArtifactsInfo CreateForWebGL(string buildPath)
        {
            var compressedSize = GetDirectorySizeNoThrow(buildPath);
            var totalSize = compressedSize;
            var streamingAssetsSize = GetDirectorySizeNoThrow(ReliablePath.Combine(buildPath, "StreamingAssets"));

            var latestReport = UnityVersionAgnostic.GetLatestBuildReport();
            if (latestReport == null)
            {
                throw new System.InvalidOperationException("Unable to retreive native Unity report");
            }

            var prop = new SerializedObject(latestReport).FindPropertyOrThrow("m_Files");

            var scenes = new List<SizePair>();
            var modules = new Dictionary<string, SizePair>();

            for (int propIdx = 0; propIdx < prop.arraySize; ++propIdx)
            {
                var elem = prop.GetArrayElementAtIndex(propIdx);
                var role = elem.FindPropertyRelativeOrThrow("role").stringValue;

                if (role == "Scene")
                {
                    var path = elem.FindPropertyRelativeOrThrow("path").stringValue;
                    var prefix = "level";
                    var lastIndex = path.LastIndexOf(prefix);
                    if (lastIndex < 0)
                    {
                        Log.Warning("Unexpected level path: " + path);
                        continue;
                    }

                    var levelNumberStr = path.Substring(lastIndex + prefix.Length);
                    var levelNumber = int.Parse(levelNumberStr);

                    // pad with zeros
                    for (int i = scenes.Count; i <= levelNumber; ++i)
                    {
                        scenes.Add(0);
                    }

                    var s = elem.FindPropertyRelative("totalSize").longValue;
                    scenes[levelNumber] = new SizePair(s, s);
                }
                else if (role == "DependentManagedLibrary" || role == "ManagedLibrary")
                {
                    var path = elem.FindPropertyRelativeOrThrow("path").stringValue;
                    var prefix = "/Managed/";
                    var lastIndex = path.LastIndexOf(prefix);
                    if (lastIndex < 0)
                    {
                        Log.Warning("Unexpected module path: " + path);
                        continue;
                    }

                    var moduleName = path.Substring(lastIndex + prefix.Length);
                    var s = elem.FindPropertyRelative("totalSize").longValue;
                    modules.Add(moduleName, new SizePair(0, s));
                }
            }

            // try to run 7z to get actual data size
            var releaseDir = ReliablePath.Combine(buildPath, "Release");
            if (Directory.Exists(releaseDir))
            {
                var buildName = buildPath.Split(new[] { "/", "\\" }, StringSplitOptions.RemoveEmptyEntries).Last();
                var zipPath = ReliablePath.Combine(releaseDir, buildName + ".datagz");
                var uncompressedSize = Get7ZipArchiveUncompressedSize(zipPath);
                if (uncompressedSize >= 0)
                {
                    totalSize += uncompressedSize;
                    totalSize -= GetFileSizeNoThrow(zipPath);
                }
            }
            else
            {
                var buildDir = ReliablePath.Combine(buildPath, "Build");
                if (Directory.Exists(buildDir))
                {
                    foreach (var compressedFile in Directory.GetFiles(buildDir, "*.unityweb"))
                    {
                        var uncompressedSize = Get7ZipArchiveUncompressedSize(compressedFile);
                        if (uncompressedSize >= 0)
                        {
                            totalSize += uncompressedSize;
                            totalSize -= GetFileSizeNoThrow(compressedFile);
                        }
                    }
                }
            }

            return new BuildArtifactsInfo()
            {
                totalSize = new SizePair(compressedSize, totalSize),
                streamingAssetsSize = streamingAssetsSize,
                sceneSizes = scenes,
                managedModules = modules,
            };
        }
#endif

        private static BuildArtifactsInfo CreateForIOS(string buildPath)
        {
            var dataDirectory = ReliablePath.Combine(buildPath, "Data");
            return CreateFromFileSystem(dataDirectory, dataDirectory, "Raw");
        }

        private static BuildArtifactsInfo CreateForStandaloneWin(string buildPath, string dataDirectoryOverride)
        {
            var dataDirectory = DropExtension(buildPath) + "_Data";
            if (!string.IsNullOrEmpty(dataDirectoryOverride))
            {
                dataDirectory = dataDirectoryOverride;
            }

            var result = CreateFromFileSystem(dataDirectory, dataDirectory, "StreamingAssets");
            result.runtimeSize = result.runtimeSize.uncompressed + GetDirectorySizeNoThrow(ReliablePath.Combine(dataDirectory, "Mono"));

            // get the exe
            var additionalRuntimeSize = GetFileSizeNoThrow(buildPath);

            if (UnityVersionAgnostic.HasStuffInRootForStandaloneBuild)
            {
                var directory = ReliablePath.GetDirectoryName(buildPath);
                additionalRuntimeSize += GetFileSizeNoThrow(ReliablePath.Combine(directory, "UnityPlayer.dll"), logError: false);
                additionalRuntimeSize += GetFileSizeNoThrow(ReliablePath.Combine(directory, "UnityCrashHandler64.exe"), logError: false);
                additionalRuntimeSize += GetDirectorySizeNoThrow(ReliablePath.Combine(directory, "Mono"));
            }

            result.totalSize.uncompressed += additionalRuntimeSize;
            result.runtimeSize.uncompressed += additionalRuntimeSize;

            return result;
        }

        private static BuildArtifactsInfo CreateForStandaloneMac(string buildPath)
        {
            buildPath = buildPath.TrimEnd('/', '\\');
            var dataDirectory = buildPath + "/Contents/Resources/Data";

            return CreateFromFileSystem(buildPath, dataDirectory, "StreamingAssets");
        }


        private static BuildArtifactsInfo CreateFromFileSystem(string totalSizeDir, string dataDirectory, string streamingAssetsName)
        {
            var modulesDirectory = ReliablePath.Combine(dataDirectory, "Managed");
            var streamingAssetsDirectory = ReliablePath.Combine(dataDirectory, streamingAssetsName);

            Dictionary<string, SizePair> modules = new Dictionary<string, SizePair>();
            long runtimeSize = 0;

            if (Directory.Exists(modulesDirectory))
            {
                // dlls are included as assets, so don't count them as runtime size
                modules = Directory.GetFiles(modulesDirectory, "*.dll", SearchOption.TopDirectoryOnly)
                    .ToDictionary(x => ReliablePath.GetFileName(x), x => (SizePair)GetFileSizeNoThrow(x));

                runtimeSize = GetDirectorySizeNoThrow(modulesDirectory) - Enumerable.Sum(modules, x => x.Value.uncompressed);
            }

            var unityResources = s_unityResourcesNames
                .Select(x => new { Relative = x, Actual = ReliablePath.Combine(dataDirectory, x) })
                .Where(x => !Directory.Exists(x.Actual))
                .Select(x => new { x.Relative, File = new FileInfo(x.Actual) })
                .Where(x => x.File.Exists)
                .ToDictionary(x => x.Relative, x => (SizePair)x.File.Length);

            return new BuildArtifactsInfo()
            {
                totalSize = GetDirectorySizeNoThrow(totalSizeDir),
                streamingAssetsSize = GetDirectorySizeNoThrow(streamingAssetsDirectory),
                runtimeSize = runtimeSize,
                managedModules = modules,
                unityResources = unityResources,
                sceneSizes = CalculateScenesSizes(x =>
                {
                    var fileInfo = new FileInfo(ReliablePath.Combine(dataDirectory, x));
                    if (!fileInfo.Exists)
                        return null;
                    return fileInfo.Length;
                })
            };
        }

        private static BuildArtifactsInfo CreateForAndroid(string buildPath, bool hasObb)
        {
            Dictionary<string, SizePair> partialResults = new Dictionary<string, SizePair>();
            Dictionary<string, SizePair> managedModules = new Dictionary<string, SizePair>();
            Dictionary<string, SizePair> otherAssets = new Dictionary<string, SizePair>();

            const string DataDirectory = "assets/bin/Data/";

            long compressedSize = 0;
            long uncompressedSize = 0;
            long streamingAssetsSize = 0;

            SizePair runtimeSize = new SizePair();
            var unityResources = new Dictionary<string, SizePair>();

            //var sources = Enumerable.Repeat(new { Source = "Apk", Files = GetFilesFromZipArchive(buildPath) }, 1);

            var sources = new List<KeyValuePair<string, string>>();
            sources.Add(new KeyValuePair<string, string>("Apk", buildPath));

            if (hasObb)
            {
                var obbPath = DropExtension(buildPath) + ".main.obb";
                sources.Add(new KeyValuePair<string, string>("Obb", obbPath));
            }

            foreach (var source in sources)
            {
                var path = source.Value;

                compressedSize += GetFileSizeNoThrow(path);

                //files = files.Concat(GetFilesFromZipArchive(obbPath));
                var files = GetFilesFromZipArchive(path);
                foreach (var entry in files)
                {
                    uncompressedSize += entry.size.uncompressed;

                    if (entry.path.StartsWith(DataDirectory))
                    {
                        var fileName = entry.path.Substring(DataDirectory.Length);
                        if (fileName.StartsWith("level") || fileName.StartsWith("mainData"))
                        {
                            partialResults.Add(fileName, entry.size);
                        }
                        else if (fileName.StartsWith("Managed/"))
                        {
                            // dlls are included as assets, so don't count them as a part of runtime
                            if (fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                            {
                                var actualFileName = ReliablePath.GetFileName(fileName);
                                managedModules.Add(actualFileName, entry.size);
                            }
                            else
                            {
                                runtimeSize.compressed += entry.size.compressed;
                                runtimeSize.uncompressed += entry.size.uncompressed;
                            }
                        }
                        else if (s_unityResourcesNames.Contains(fileName))
                        {
                            unityResources.Add(fileName, entry.size);
                        }
                        else
                        {
                            // is it a guid?
                            var justFileName = Path.GetFileNameWithoutExtension(fileName);
                            if (justFileName.Length == 32 && justFileName.All(x => char.IsDigit(x) || x >= 'a' && x <= 'f' || x >= 'A' && x <= 'F'))
                            {
                                SizePair existingEntry;
                                if (otherAssets.TryGetValue(justFileName, out existingEntry))
                                {
                                    otherAssets[justFileName] = new SizePair(existingEntry.compressed + entry.size.compressed, existingEntry.uncompressed + entry.size.uncompressed);
                                }
                                else
                                {
                                    otherAssets.Add(justFileName, entry.size);
                                }
                            }
                        }
                    }
                    else if (entry.path.StartsWith("assets/"))
                    {
                        streamingAssetsSize += entry.size.uncompressed;
                    }
                    else if (entry.path.StartsWith("lib/"))
                    {
                        runtimeSize.compressed += entry.size.compressed;
                        runtimeSize.uncompressed += entry.size.uncompressed;
                    }
                }
            }

            var scenes = CalculateScenesSizes(x =>
            {
                SizePair result;
                if (partialResults.TryGetValue(x, out result))
                    return result;
                return null;
            });

            return new BuildArtifactsInfo()
            {
                managedModules = managedModules,
                sceneSizes = scenes,
                streamingAssetsSize = streamingAssetsSize,
                totalSize = new SizePair(compressedSize, uncompressedSize),
                runtimeSize = runtimeSize,
                unityResources = unityResources,
                otherAssets = otherAssets,
            };
        }

        private static long Get7ZipArchiveUncompressedSize(string path)
        {
            var process = new System.Diagnostics.Process();
            var startInfo = process.StartInfo;
            startInfo.FileName = EditorApplication.applicationContentsPath + "/Tools/7z";
            startInfo.UseShellExecute = false;
            startInfo.Arguments = string.Format("t \"{0}\"", path);
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = true;

            process.Start();

            var stdoutData = process.StandardOutput.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                Log.Debug("Invalid 7Zip return code: {0} for {2}, details:\n{1}", process.ExitCode, stdoutData, path);
                return -1;
            }

            var regex = new Regex(@"^Size:\s*(\d+)$");

            using (var reader = new StringReader(stdoutData))
            {
                for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
                {
                    var match = regex.Match(line);
                    if (match.Success)
                    {
                        return long.Parse(match.Groups[1].Value);
                    }
                }
            }

            return -1;
        }

        private struct ZipFileEntry
        {
            public string path;
            public SizePair size;
        }

        private static IEnumerable<ZipFileEntry> GetFilesFromZipArchive(string zipPath)
        {
            var process = new System.Diagnostics.Process();
            var startInfo = process.StartInfo;
            startInfo.FileName = BuildInfoSettings.Instance.ZipinfoPath;
            startInfo.UseShellExecute = false;
            startInfo.Arguments = string.Format("-l \"{0}\"", zipPath);
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = true;

            process.Start();

            var stdoutData = process.StandardOutput.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new System.InvalidOperationException(string.Format("Invalid return code: {0}, details:\n{1}", process.ExitCode, stdoutData));
            }

            using (var reader = new StringReader(stdoutData))
            {
                // ignore first line
                var line = reader.ReadLine();

                if (line != null)
                    line = reader.ReadLine();

                if (line != null)
                {
                    // now it seems something changed since 2.41 and now header is printed by default; let's skip it
                    if (line.StartsWith("Zip file size:", StringComparison.OrdinalIgnoreCase))
                    {
                        line = reader.ReadLine();
                    }
                }

                if (line == null)
                {
                    throw new System.InvalidOperationException("Unexpected output format:\n" + stdoutData);
                }

                // scan each line except the last one
                for (string nextLine = reader.ReadLine(); nextLine != null; line = nextLine, nextLine = reader.ReadLine())
                {
                    string filePath;
                    long fileSize;
                    long compressedSize;

                    try
                    {
                        var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 10)
                        {
                            filePath = parts[9];
                        }
                        else
                        {
                            filePath = string.Join(" ", parts.Skip(9).ToArray());
                        }

                        fileSize = long.Parse(parts[3]);
                        compressedSize = long.Parse(parts[5]);

                    }
                    catch (System.Exception ex)
                    {
                        throw new System.InvalidOperationException("Error parsing line: " + line, ex);
                    }

                    yield return new ZipFileEntry()
                    {
                        path = filePath,
                        size = new SizePair(compressedSize, fileSize)
                    };
                }

                {
                    var regex = new Regex(@"(\d+) files, (\d+) bytes uncompressed, (\d+) bytes compressed");
                    var match = regex.Match(line);
                    if (!match.Success)
                    {
                        throw new System.InvalidOperationException("Unexpected footer format: " + line);
                    }
                }
            }
        }

        private static List<SizePair> CalculateScenesSizes(Func<string, SizePair?> getSize)
        {
            List<SizePair> result = new List<SizePair>();

            for (int i = 0, levelIndex = 0; ; ++i, ++levelIndex)
            {
                SizePair totalSize = 0;
                string levelPath = "level" + levelIndex;

                if (i == 0)
                {
                    // on new unity it's gone... don't know which version exactly made that change
                    var mainDataPath = "mainData";
                    if (getSize(mainDataPath).HasValue)
                    {
                        levelPath = mainDataPath;
                        --levelIndex;
                    }
                }

                bool hasEntryForLevel = false;

                var size = getSize(levelPath);
                if (size.HasValue)
                {
                    totalSize.compressed += size.Value.compressed;
                    totalSize.uncompressed += size.Value.uncompressed;
                    hasEntryForLevel = true;
                }

                for (int splitIndex = 0; ; ++splitIndex)
                {
                    var splitPath = levelPath + ".split" + splitIndex;
                    size = getSize(splitPath);
                    if (size.HasValue)
                    {
                        totalSize.compressed += size.Value.compressed;
                        totalSize.uncompressed += size.Value.uncompressed;
                        hasEntryForLevel = true;
                    }
                    else
                    {
                        break;
                    }
                }

                if (hasEntryForLevel)
                {
                    result.Add(totalSize);
                }
                else
                {
                    break;
                }
            }

            return result;
        }

        private static long GetFileSizeNoThrow(string path, bool logError = true)
        {
            FileInfo fi;
            try
            {
                fi = new FileInfo(path);
            }
            catch (System.Exception ex)
            {
                if (logError)
                {
                    Log.Error("Unable to get file {0} size: {1}", path, ex);
                }
                return 0;
            }
            return GetFileSizeNoThrow(fi, logError);
        }


        private static long GetFileSizeNoThrow(FileInfo fileInfo, bool logError = true)
        {
            if (!fileInfo.Exists)
            {
                if (logError)
                    Log.Error("File {0} doesn't exist", fileInfo.FullName);

                return 0;
            }

            try
            {
                return fileInfo.Length;
            }
            catch (System.Exception ex)
            {
                if (logError)
                    Log.Error("Unable to get file {0} size: {1}", fileInfo.FullName, ex);

                return 0;
            }
        }

        private static long GetDirectorySizeNoThrow(string path)
        {
            DirectoryInfo di;
            try
            {
                di = new DirectoryInfo(path);
                if (!di.Exists)
                {
                    return 0;
                }
            }
            catch (System.Exception ex)
            {
                Log.Error("Unable to get file {0} size: {1}", path, ex);
                return 0;
            }
            return GetDirectorySizeNoThrow(di);
        }

        private static long GetDirectorySizeNoThrow(DirectoryInfo directory)
        {
            if (!directory.Exists)
            {
                Log.Error("Directory {0} doesn't exist", directory.FullName);
                return 0;
            }

            long size = 0;

            var files = directory.GetFiles();
            foreach (var file in files)
            {
                size += GetFileSizeNoThrow(file);
            }

            var directories = directory.GetDirectories();
            foreach (var child in directories)
            {
                size += GetDirectorySizeNoThrow(child);
            }
            return size;
        }

        private static string DropExtension(string path)
        {
            return ReliablePath.Combine(ReliablePath.GetDirectoryName(path), ReliablePath.GetFileNameWithoutExtension(path));
        }
    }
}

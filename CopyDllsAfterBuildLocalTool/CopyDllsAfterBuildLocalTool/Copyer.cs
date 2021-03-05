using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CopyDllsAfterBuildLocalTool
{
    public class Copyer
    {
        // `$` means end of a file name exclude the extension. if file name is end with this marker, will do exact match.
        private const string ExactMatchMarker = "$";

        private static readonly string[] unityPluginExtensions = new[] { "dll", "pdb", "xml" };
        private static readonly ILogger logger = Logger.Instance;
        // destination depth will be only top directory. ignore for child folder in destination path.
        private static readonly SearchOption searchOption = SearchOption.TopDirectoryOnly;

        private readonly string _projectDir;
        private readonly Statistic _statistic;

        public Copyer(string projectDir)
        {
            _projectDir = Path.GetFullPath(projectDir);
            _statistic = new Statistic();
        }

        /// <summary>
        /// Get Settings from file.
        /// </summary>
        /// <param name="settingsFile"></param>
        /// <returns></returns>
        public CopySettings GetSettings(string settingsFile)
        {
            var settingsPath = Path.Combine(_projectDir, settingsFile);
            if (!File.Exists(settingsPath))
                throw new FileNotFoundException($"Settings file path not found. {settingsPath}.");

            logger.LogDebug($"Setting file found, loading file. {settingsFile}");
            var json = File.ReadAllText(settingsPath, Encoding.UTF8);
            return CopySettings.LoadJson(json);
        }

        /// <summary>
        /// Obtain exclude file names from excludes and folders.
        /// </summary>
        /// <param name="excludes"></param>
        /// <param name="excludeFolders"></param>
        /// <returns></returns>
        public string[] GetExcludes(string[] excludes, string[] excludeFolders)
        {
            var excludesFromFolder = new List<string>();
            foreach (var excludeFolder in excludeFolders)
            {
                var excludePath = Path.Combine(_projectDir, excludeFolder);
                if (Directory.Exists(excludePath))
                {
                    logger.LogDebug($"Enumerate exclude files from exclude_folders. folder: {excludePath}");

                    // top directory only
                    foreach (var excludeFile in Directory.EnumerateFiles(excludePath))
                    {
                        // exlude unity .meta extension file
                        if (Path.GetExtension(excludeFile) != ".meta")
                        {
                            logger.LogTrace($"Exclude file found from exclude_folders. file: {excludeFile}");

                            // mark for exact match instead of prefix match.
                            var fileName = Path.GetFileNameWithoutExtension(excludeFile) + ExactMatchMarker;
                            excludesFromFolder.Add(fileName);
                        }
                    }
                }
            }
            if (excludeFolders.Any())
            {
                logger.LogTrace($"Concat excludes and exclude_folders file names.");
                var distinct = excludesFromFolder.Distinct();
                return excludes.Concat(distinct).Distinct().ToArray();
            }
            else
            {
                logger.LogTrace($"exclude_folders not contains target, just excludes will be use.");
                return excludes;
            }
        }

        /// <summary>
        /// Sync dlls from source folder to destination folder.
        /// Source dlls will search with pattern.
        /// When filename is includes in excludes, it won't copy.
        /// </summary>
        /// <param name="source">copy source folder path</param>
        /// <param name="destination">copy destination folder path</param>
        /// <param name="pattern">source file name pattern to search.</param>
        /// <param name="excludes">excludes file names from copy.</param>
        public void Sync(string source, string destination, string pattern, string[] excludes)
        {
            logger.LogInformation("Copy DLLs");

            if (!Directory.Exists(destination))
            {
                logger.LogDebug($"Creating destination directory {destination}");
                Directory.CreateDirectory(destination);
            }

            var sourceFiles = new List<string>();
            foreach (var ext in unityPluginExtensions)
            {
                logger.LogDebug($"Begin Copy dlls. extensions {ext}, source {source}");

                // source candidates
                var candicates = Directory.EnumerateFiles(source, $"{pattern}.{ext}", searchOption);
                if (!candicates.Any())
                {
                    // origin not found, go next.
                    logger.LogTrace($"skipping copy, go next extention. extension: {ext}, reason: Source files not found.");
                    continue;
                }

                // determine source files
                var sources = SkipExcludes(candicates, excludes, ext);
                sourceFiles.AddRange(sources);
            }

            // delete all except for Synced target.
            var destinationFiles = Directory.EnumerateFiles(destination, $"*", searchOption);
            var deleteFiles = destinationFiles.Except(sourceFiles.Select(x => Path.Combine(destination, Path.GetFileName(x)))).ToArray();
            Delete(deleteFiles);

            // copy!
            CopyCore(sourceFiles, destination);

            logger.LogDebug($"Copy completed, total {_statistic.TotalCount}");
        }

        /// <summary>
        /// Get Excluded source files
        /// </summary>
        /// <param name="sourceFiles"></param>
        /// <param name="exactMatchExcludeFiles"></param>
        /// <param name="prefixMatchExcludeFiles"></param>
        /// <returns></returns>
        private string[] SkipExcludes(IEnumerable<string> sourceFiles, string[] excludes, string extention)
        {
            // classify excludes with ExactMatch and PrefixMatch
            var length = excludes.Length;
            var exactMatchLength = excludes.Where(x => x.EndsWith(ExactMatchMarker)).Count();
            var exactMatchExcludeFiles = new string[exactMatchLength];
            var prefixMatchLength = excludes.Length - exactMatchLength;
            var prefixMatchExcludeFiles = new string[prefixMatchLength];
            var (prefixIndex, exactIndex) = (0, 0);
            for (var i = 0; i < length; i++)
            {
                if (excludes[i].EndsWith(ExactMatchMarker))
                {
                    // exact match. remove $ marker.
                    exactMatchExcludeFiles[exactIndex] = excludes[i].Substring(0, excludes[i].Length - 1) + "." + extention;
                    exactIndex++;
                }
                else
                {
                    // prefix match
                    prefixMatchExcludeFiles[prefixIndex] = excludes[i];
                    prefixIndex++;
                }
            }

            // determine source files
            var outputs = new List<string>();
            foreach (var sourceFile in sourceFiles)
            {
                var fileName = Path.GetFileName(sourceFile);
                if (PrefixMatch(prefixMatchExcludeFiles, fileName) || ExactMatch(exactMatchExcludeFiles, fileName))
                {
                    // skipped
                    logger.LogTrace($"Skipping copy. filename: {fileName}, reason: match to exclude.");
                    _statistic.IncrementSkip();
                    continue;
                }
                outputs.Add(sourceFile);
            }
            return outputs.ToArray();
        }

        /// <summary>
        /// Copy files from source to destination for current file extension.
        /// </summary>
        /// <param name="sources"></param>
        /// <param name="destination"></param>
        private void CopyCore(IEnumerable<string> sources, string destination)
        {
            // copy
            foreach (var copyFrom in sources)
            {
                var fileName = Path.GetFileName(copyFrom);
                var copyTo = Path.Combine(destination, fileName);

                // Write source file to destination path
                var sourceBinary = File.ReadAllBytes(copyFrom);
                var result = FileWriter.Write(sourceBinary, copyTo, WriteCheckOption.BinaryEquality);
                if (result)
                {
                    // skipped
                    logger.LogTrace($"Skipping copy. filename: {fileName}, reason: binary not changed.");
                    _statistic.IncrementSkip();
                }
                else
                {
                    // copied
                    logger.LogTrace($"Copied. filename: {fileName}");
                    _statistic.IncrementCopy();
                }
            }
        }

        /// <summary>
        /// Delete files
        /// </summary>
        /// <param name="files"></param>
        private void Delete(string[] files)
        {
            foreach (var file in files)
            {
                if (File.Exists(file))
                {
                    logger.LogTrace($"Delete unneccesary file. filename: {Path.GetFileName(file)}, reason: Not a sync target.");
                    File.Delete(file);
                    _statistic.IncrementDelete();
                }
            }
        }

        private static bool ExactMatch(string[] source, string input) => source.Contains(input);
        private static bool PrefixMatch(string[] source, string input) => source.Any(x => input.StartsWith(x));

        public class Statistic
        {
            public int TotalCount => _copyCount + _skipCount + _deleteCount;
            public int CopyCount => _copyCount;
            private int _copyCount = 0;
            public int SkipCount => _skipCount;
            private int _skipCount = 0;
            public int DeleteCount => _deleteCount;
            private int _deleteCount = 0;

            public Statistic() { }
            public Statistic(int copyCount, int skipCount, int deleteCount) 
                => (_copyCount, _skipCount, _deleteCount) = (copyCount, skipCount, deleteCount);

            public string GetProgressMessage() => $"copied {_copyCount}, skipped {_skipCount}, deleted {_deleteCount}";
            public void Reset() => (_copyCount, _skipCount, _deleteCount) = (0, 0, 0);
            public void IncrementCopy() => _copyCount++;
            public void IncrementSkip() => _skipCount++;
            public void IncrementDelete() => _deleteCount++;
        }
    }
}

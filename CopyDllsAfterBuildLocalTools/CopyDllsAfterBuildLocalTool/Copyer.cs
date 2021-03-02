using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CopyDllsAfterBuild
{
    public class Copyer
    {
        // `$` means end of a file name exclude the extension. if file name is end with this marker, will do exact match.
        private const string ExactMatchMarker = "$";

        private static readonly string[] unityPluginExtensions = new[] { "dll", "pdb", "xml" };
        private static readonly ILogger logger = Logger.Instance;

        private readonly string _projectDir;
        private int _totalCount;

        public Copyer(string projectDir) => _projectDir = projectDir;

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
        /// Copy dlls from source to destination. source dlls will search with pattern. excluded when filename is includes in excludes.
        /// </summary>
        /// <param name="source">source of copy</param>
        /// <param name="destination">destination of copy</param>
        /// <param name="pattern">exclude pattern when exclude is prefix match.</param>
        /// <param name="excludes">excludes files from copy.</param>
        public void CopyDlls(string source, string destination, string pattern, string[] excludes)
        {
            logger.LogInformation("Copy DLLs");

            if (!Directory.Exists(destination))
            {
                logger.LogDebug($"Creating destination directory {destination}");
                Directory.CreateDirectory(destination);
            }

            foreach (var ext in unityPluginExtensions)
            {
                logger.LogDebug($"Begin Copy dlls. extensions {ext}, source {source}");

                var sourceFiles = Directory.EnumerateFiles(source, $"{pattern}.{ext}", SearchOption.TopDirectoryOnly);
                if (!sourceFiles.Any())
                {
                    // origin not found, go next.
                    logger.LogTrace($"skipping copy. extension: {ext}, reason: Source files not found, skip to next.");
                    continue;
                }

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
                        exactMatchExcludeFiles[exactIndex] = excludes[i].Substring(0, excludes[i].Length - 1) + "." + ext;
                        exactIndex++;
                    }
                    else
                    {
                        // prefix match
                        prefixMatchExcludeFiles[prefixIndex] = excludes[i];
                        prefixIndex++;
                    }
                }

                // delete existing before copy. File lock will fail this operation.
                DeleteExistingFiles(destination, ext);

                // do copy!
                CopyCore(destination, sourceFiles, exactMatchExcludeFiles, prefixMatchExcludeFiles);
            }
            logger.LogDebug($"Copy completed. Total Copied {_totalCount}.");
        }

        private void CopyCore(string destination, IEnumerable<string> sourceFiles, string[] exactMatchExcludeFiles, string[] prefixMatchExcludeFiles)
        {
            var count = 0;
            var skipCount = 0;
            foreach (var sourceFile in sourceFiles)
            {
                var fileName = Path.GetFileName(sourceFile);
                if (PrefixMatch(prefixMatchExcludeFiles, fileName) || ExactMatch(exactMatchExcludeFiles, fileName))
                {
                    logger.LogTrace($"Skipping copy. filename: {fileName}, reason: match to exclude.");
                    skipCount++;
                    continue;
                }
                var destinationPath = Path.Combine(destination, fileName);
                logger.LogTrace($"Copying from {sourceFile} to {destinationPath}");
                File.Copy(sourceFile, destinationPath, true);
                count++;
            }
            logger.LogDebug($"Copy progress. copied {count}, skipped {skipCount}.");
            _totalCount += count;
        }

        private static void DeleteExistingFiles(string destination, string ext)
        {
            var destinationFiles = Directory.EnumerateFiles(destination, $"*.{ext}", SearchOption.TopDirectoryOnly);
            foreach (var destinationFile in destinationFiles)
            {
                if (File.Exists(destinationFile))
                {
                    logger.LogTrace($"Deleting existing {destinationFile}");
                    File.Delete(destinationFile);
                }
            }
        }

        private static bool ExactMatch(string[] source, string input) => source.Contains(input);
        private static bool PrefixMatch(string[] source, string input) => source.Any(x => input.StartsWith(x));
    }
}

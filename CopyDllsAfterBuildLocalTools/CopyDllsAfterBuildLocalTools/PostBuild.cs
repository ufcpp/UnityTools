using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CopyDllsAfterBuild
{
    class PostBuild
    {
        private static readonly string[] unityPluginExtensions = new[] { "dll", "pdb", "xml" };
        private static readonly ILogger logger = Logger.Instance;

        private readonly string _projectDir;

        public PostBuild(string projectDir)
        {
            _projectDir = projectDir;
        }

        public CopySettings GetSettings(string settingsFile)
        {
            var settingsPath = Path.Combine(_projectDir, settingsFile);
            if (File.Exists(settingsPath))
            {
                logger.LogDebug($"Setting file {settingsFile} found.");
                return CopySettings.LoadJsonFile(settingsPath);
            }
            else
            {
                logger.LogDebug("Setting file {settingsFile} not found. Use default settings.");
                return new CopySettings();
            }
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
                if (Directory.Exists(excludeFolder))
                {
                    logger.LogDebug($"Enumerate exclude files from exclude_folders. folder: {excludeFolder}");

                    // top directory only
                    foreach (var excludeFile in Directory.EnumerateFiles(excludeFolder))
                    {
                        // exlude unity .meta extension file
                        if (Path.GetExtension(excludeFile) != ".meta")
                        {
                            logger.LogTrace($"Exclude file found from exclude_folders. file: {excludeFile}");

                            // $ to use exact match instead of prefix match.
                            var fileName = Path.GetFileNameWithoutExtension(excludeFile) + "$";
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
                var completeMatchLength = excludes.Where(x => x.EndsWith("$")).Count();
                var completeMatchExcludeFiles = new string[completeMatchLength];
                var prefixMatchLength = excludes.Length - completeMatchLength;
                var prefixMatchExcludeFiles = new string[prefixMatchLength];
                var (prefixIndex, perfectIndex) = (0, 0);
                for (var i = 0; i < length; i++)
                {
                    // # `$` means end of a file name exclude the extension.
                    if (excludes[i].EndsWith("$"))
                    {
                        // complete match. remove $ marker.
                        completeMatchExcludeFiles[perfectIndex] = excludes[i].Substring(0, excludes[i].Length - 1) + "." + ext;
                        perfectIndex++;
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
                CopyCore(destination, sourceFiles, completeMatchExcludeFiles, prefixMatchExcludeFiles);
            }
        }

        private void CopyCore(string destination, IEnumerable<string> sourceFiles, string[] completeMatchExcludeFiles, string[] prefixMatchExcludeFiles)
        {
            foreach (var sourceFile in sourceFiles)
            {
                var fileName = Path.GetFileName(sourceFile);
                if (PrefixMatch(prefixMatchExcludeFiles, fileName) || PerfectMatch(completeMatchExcludeFiles, fileName))
                {
                    logger.LogTrace($"Skipping copy. filename: {fileName}, reason: match to exclude.");
                    continue;
                }
                var destinationPath = Path.Combine(destination, fileName);
                logger.LogTrace($"Copying from {sourceFile} to {destinationPath}");
                File.Copy(sourceFile, destinationPath, true);
            }
        }

        private static void DeleteExistingFiles(string destination, string ext)
        {
            logger.LogDebug($"Delete copy destination files. extensions {ext}, destination {destination}");
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

        private bool PerfectMatch(string[] source, string fileName) => source.Contains(fileName);
        private bool PrefixMatch(string[] source, string fileName) => source.Any(x => fileName.StartsWith(x));
    }
}

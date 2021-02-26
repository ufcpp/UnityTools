using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CopyDllsAfterBuild
{
    class PostBuild
    {
        private static readonly string[] unityPluginExtensions = new[] { "dll", "pdb", "xml" };

        private readonly string _projectDir;

        public PostBuild(string projectDir)
        {
            _projectDir = projectDir;
        }

        public CopySettings GetSettings(string settingsFile)
        {
            var settingsPath = Path.Combine(_projectDir, settingsFile);
            return File.Exists(settingsPath)
                ? CopySettings.LoadJsonFile(settingsPath)
                : new CopySettings();
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
                    // top directory only
                    foreach (var excludeFile in Directory.EnumerateFiles(excludeFolder))
                    {
                        // exlude unity .meta extension file
                        if (Path.GetExtension(excludeFile) != ".meta")
                        {
                            // $ to use exact match instead of prefix match.
                            var fileName = Path.GetFileNameWithoutExtension(excludeFile) + "$";
                            excludesFromFolder.Add(fileName);
                        }
                    }
                }
            }
            if (excludeFolders.Any())
            {
                var distinct = excludesFromFolder.Distinct();
                return excludes.Concat(distinct).Distinct().ToArray();
            }
            else
            {
                return excludes;
            }
        }

        public void CopyDlls(string source, string destination, string pattern, string[] excludes)
        {
            Console.WriteLine("Copy DLLs");

            if (!Directory.Exists(destination))
                Directory.CreateDirectory(destination);


            foreach (var ext in unityPluginExtensions)
            {
                var destinationFile = Path.Combine(destination, ext);
                var sourceFiles = Directory.EnumerateFiles(source, $"{pattern}.{ext}", SearchOption.TopDirectoryOnly);
                if (!sourceFiles.Any())
                {
                    // origin not found, go next.
                    continue;
                }

                var length = excludes.Length;
                var excludeFiles = new string[length];
                for (var i = 0; i < length; i++)
                {
                    // # `$` means end of a file name excluding the extension.
                    if (excludes[i].EndsWith("$"))
                    {
                        // remove $ marker
                        excludeFiles[i] = excludes[i].Substring(0, excludes[i].Length - 1) + "." + ext;
                    }
                    else
                    {
                        excludeFiles[i] = excludes[i] + "." + ext;
                    }
                }

                // delete existing before copy. File lock will fail this operation.
                if (File.Exists(destinationFile))
                    File.Delete(destinationFile);

                // do copy!
                foreach (var sourceFile in sourceFiles)
                {
                    File.Copy(sourceFile, destination, true);
                }                
            }
        }
    }
}

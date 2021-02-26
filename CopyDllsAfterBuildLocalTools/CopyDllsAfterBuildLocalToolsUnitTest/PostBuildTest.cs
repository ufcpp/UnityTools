﻿using CopyDllsAfterBuild;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace CopyDllsAfterBuildLocalToolsUnitTest
{
    public class PostBuildTest : IDisposable
    {
        private readonly string _destination = "../Dlls";
        private readonly string _pattern = "*";
        private readonly string[] _excludes = new[] { "UnityEngine", "UnityEditor" };
        private readonly (string folder, string[] files)[] _excludeFolders = new[]
        {
            ("ExcludeDlls", new[] { "System.Buffers.dll", "System.Memory.dll" }),
            ("ExcludeMeta", new[] { "foo.dll", "bar.dll" })
        };

        private readonly string _projectDir;
        private readonly string _targetDir;
        private readonly string _settingsFile;
        private readonly string _settingsFilePath;

        // setup
        public PostBuildTest()
        {
            _projectDir = Path.Combine(Path.GetTempPath(), "CopyDllsTest", Guid.NewGuid().ToString());
            _targetDir = Path.Combine(Path.GetTempPath(), "CopyDllsTest", Guid.NewGuid().ToString(), "bin", "Debug", "net5.0");
            _settingsFile = "CopySettings.json";
            _settingsFilePath = Path.Combine(_projectDir, _settingsFile);

            DeleteTempPath();
            CreateTempPath();
        }

        // teardown
        public void Dispose()
        {
            DeleteTempPath();
        }

        [Fact]
        public void GetSettings_Should_Deserialize_Test()
        {
            var json = $@"
{{
""destination"": ""{_destination}"",
""pattern"": ""{_pattern}"",
""excludes"": [
""{_excludes[0]}"",
""{_excludes[1]}""
],
""exclude_folders"": [
""{_excludeFolders[0].folder}"",
""{_excludeFolders[1].folder}""
]
}}";
            File.WriteAllText(_settingsFilePath, json);

            var build = new PostBuild(_projectDir);
            var settings = build.GetSettings(_settingsFile);
            Assert.Equal(_destination, settings.Destination);
            Assert.Equal(_pattern, settings.Pattern);
            Assert.Equal(_excludes, settings.Excludes);
            Assert.Equal(_excludeFolders.Select(x => x.folder).ToArray(), settings.ExcludeFolders);
        }

        [Fact]
        public void GetSettings_Should_UseDefault_Test()
        {
            // missing CopySettings.json path should use default settings
            var expected = new CopySettings();
            var build = new PostBuild(_projectDir);
            var settings = build.GetSettings(_settingsFile);
            Assert.Equal(expected.Destination, settings.Destination);
            Assert.Equal(expected.Pattern, settings.Pattern);
            Assert.Equal(expected.Excludes, settings.Excludes);
            Assert.Equal(expected.ExcludeFolders, settings.ExcludeFolders);
        }

        [Fact]
        public void GetExclude_Should_Concat_Test()
        {
            var expected = _excludes.Concat(_excludeFolders.SelectMany(xs => xs.files.Select(x => Path.GetFileNameWithoutExtension(x) + "$")))
                .Distinct()
                .OrderBy(x => x)
                .ToArray();

            CreateExcludes(_excludes);
            foreach (var exclude in _excludeFolders)
            {
                CreateExcludeFolders(exclude.folder, exclude.files);
            }

            var json = $@"
{{
""destination"": ""{_destination}"",
""pattern"": ""{_pattern}"",
""excludes"": [
""{_excludes[0]}"",
""{_excludes[1]}""
],
""exclude_folders"": [
""{_excludeFolders[0].folder}"",
""{_excludeFolders[1].folder}""
]
}}";
            File.WriteAllText(_settingsFilePath, json);

            var build = new PostBuild(_projectDir);
            var settings = build.GetSettings(_settingsFile);
            var excludes = build.GetExcludes(settings.Excludes, settings.ExcludeFolders);
            Assert.Equal(expected, excludes.OrderBy(x => x).ToArray());
            Assert.Equal(6, excludes.Count()); // exclude(2) + exclude_folders(4)
        }

        [Fact]
        public void GetExclude_Should_Ignore_metafile_Test()
        {
            (string folder, string[] files)[] excludeFolders = new[]
            {
                ("ExcludeDlls", new[] { "UnityEngine", "UnityEditor" }),
                ("ExcludeMeta", new[] { "foo.meta", "foo.meta" })
            };
            var expected = _excludes.Concat(excludeFolders.SelectMany(xs => xs.files
                    .Where(x => Path.GetExtension(x) != ".meta")
                    .Select(x => Path.GetFileNameWithoutExtension(x) + "$")))
                .Distinct()
                .OrderBy(x => x)
                .ToArray();

            CreateExcludes(_excludes);
            foreach (var exclude in excludeFolders)
            {
                CreateExcludeFolders(exclude.folder, exclude.files);
            }

            var json = $@"
{{
""destination"": ""{_destination}"",
""pattern"": ""{_pattern}"",
""excludes"": [
""{_excludes[0]}"",
""{_excludes[1]}""
],
""exclude_folders"": [
""{excludeFolders[0].folder}"",
""{excludeFolders[1].folder}""
]
}}";
            File.WriteAllText(_settingsFilePath, json);

            var build = new PostBuild(_projectDir);
            var settings = build.GetSettings(_settingsFile);
            var excludes = build.GetExcludes(settings.Excludes, settings.ExcludeFolders);
            Assert.Equal(expected, excludes.OrderBy(x => x).ToArray());
            Assert.Equal(4, excludes.Count()); // exclude(2) + exclude_folders(4) - .meta excludes(2)
        }

        [Fact]
        public void GetExclude_Should_Distinct_Test()
        {
            var excludesStrict = new[] { "UnityEngine$", "UnityEditor$" };
            (string folder, string[] files)[] excludeFolders = new[]
            {
                ("ExcludeDlls", new[] { "System.Buffers.dll", "System.Memory.dll" }),
                ("ExcludeMeta", new[] { "UnityEngine.dll", "UnityEditor.dll" }) // distinct with excludes
            };
            var expected = excludesStrict.Concat(excludeFolders.SelectMany(xs => xs.files.Select(x => Path.GetFileNameWithoutExtension(x) + "$")))
                .Distinct()
                .OrderBy(x => x)
                .ToArray();

            CreateExcludes(excludesStrict);
            foreach (var exclude in excludeFolders)
            {
                CreateExcludeFolders(exclude.folder, exclude.files);
            }

            var json = $@"
{{
""destination"": ""{_destination}"",
""pattern"": ""{_pattern}"",
""excludes"": [
""{excludesStrict[0]}"",
""{excludesStrict[1]}""
],
""exclude_folders"": [
""{excludeFolders[0].folder}"",
""{excludeFolders[1].folder}""
]
}}";
            File.WriteAllText(_settingsFilePath, json);

            var build = new PostBuild(_projectDir);
            var settings = build.GetSettings(_settingsFile);
            var excludes = build.GetExcludes(settings.Excludes, settings.ExcludeFolders);
            Assert.Equal(expected, excludes.OrderBy(x => x).ToArray());
            Assert.Equal(4, excludes.Count()); // exclude(2) + exclude_folders(4) - distinct(2)
        }

        [Fact]
        public void GetExclude_Should_Distinct_Test2()
        {
            var excludesStrict = new[] { "UnityEngine$", "UnityEditor$" };
            (string folder, string[] files)[] excludeFolders = new[]
            {
                ("ExcludeDlls", new[] { "UnityEngine.dll", "UnityEngine.dll" }), // distinct with excludeStrict
                ("ExcludeMeta", new[] { "UnityEngine.xml", "UnityEditor.xml" }) // distinct with ExcludeDlls
            };
            var expected = excludesStrict.Concat(excludeFolders.SelectMany(xs => xs.files.Select(x => Path.GetFileNameWithoutExtension(x) + "$")))
                .Distinct()
                .OrderBy(x => x)
                .ToArray();

            CreateExcludes(excludesStrict);
            foreach (var exclude in excludeFolders)
            {
                CreateExcludeFolders(exclude.folder, exclude.files);
            }

            var json = $@"
{{
""destination"": ""{_destination}"",
""pattern"": ""{_pattern}"",
""excludes"": [
""{excludesStrict[0]}"",
""{excludesStrict[1]}""
],
""exclude_folders"": [
""{excludeFolders[0].folder}"",
""{excludeFolders[1].folder}""
]
}}";
            File.WriteAllText(_settingsFilePath, json);

            var build = new PostBuild(_projectDir);
            var settings = build.GetSettings(_settingsFile);
            var excludes = build.GetExcludes(settings.Excludes, settings.ExcludeFolders);
            Assert.Equal(expected, excludes.OrderBy(x => x).ToArray());
            Assert.Equal(2, excludes.Count()); // exclude(2) + exclude_folders(4) - distinct(4)
        }

        [Fact]
        public void CopyDllsTest()
        {
            var buildOutputs = new[] {
                "Class1.dll",
                "Class1.pdb",
                "Cocona.dll",
                "ConsoleApp.dll",
                "ConsoleApp.pdb",
                "Microsoft.Extensions.DependencyInjection.dll",
                "Microsoft.Extensions.Hosting.dll",
                "Microsoft.Extensions.Logging.dll",
                "System.Text.Json.dll",
                "UnityEngine.dll",
                "System.Buffers.dll",
                "System.Memory.dll",
            };
            var expected = new[]
            {
                "Class1.dll",
                "Class1.pdb",
                "Cocona.dll",
                "ConsoleApp.dll",
                "ConsoleApp.pdb",
                "Microsoft.Extensions.DependencyInjection.dll",
                "Microsoft.Extensions.Hosting.dll",
                "Microsoft.Extensions.Logging.dll",
                "System.Text.Json.dll",
            };

            CreateTargetDir(buildOutputs);
            CreateExcludes(_excludes);
            foreach (var exclude in _excludeFolders)
            {
                CreateExcludeFolders(exclude.folder, exclude.files);
            }
            CreateTargetDir(buildOutputs);

            var json = $@"
{{
""destination"": ""{_destination}"",
""pattern"": ""{_pattern}"",
""excludes"": [
""{_excludes[0]}"",
""{_excludes[1]}""
],
""exclude_folders"": [
""{_excludeFolders[0].folder}"",
""{_excludeFolders[1].folder}""
]
}}";
            File.WriteAllText(_settingsFilePath, json);

            var build = new PostBuild(_projectDir);
            var settings = build.GetSettings(_settingsFile);
            var excludes = build.GetExcludes(settings.Excludes, settings.ExcludeFolders);
            build.CopyDlls(_targetDir, settings.Destination, settings.Pattern, excludes);
            var actual = Directory.GetFiles(settings.Destination).Select(x => Path.GetFileName(x)).ToArray();
            Assert.Equal(expected, actual);
        }


        private void CreateTempPath()
        {
            if (!Directory.Exists(_projectDir))
                Directory.CreateDirectory(_projectDir);
        }
        private void DeleteTempPath()
        {
            if (Directory.Exists(_projectDir))
                Directory.Delete(_projectDir, true);
        }
        private void CreateTargetDir(string[] fileNames)
        {
            if (!Directory.Exists(_targetDir))
                Directory.CreateDirectory(_targetDir);

            foreach (var file in fileNames)
            {
                var filePath = Path.Combine(_targetDir, file);
                if (File.Exists(filePath))
                    File.Delete(filePath);

                File.WriteAllText(filePath, "");
            }
        }
        private void CreateExcludes(string[] fileNames)
        {
            foreach (var file in fileNames)
            {
                var filePath = Path.Combine(_projectDir, file);
                if (File.Exists(filePath))
                    File.Delete(filePath);

                File.WriteAllText(filePath, "");
            }
        }

        private void CreateExcludeFolders(string directoryName, string[] fileNames)
        {
            var path = Path.Combine(_projectDir, directoryName);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            foreach (var file in fileNames)
            {
                var filePath = Path.Combine(path, file);
                if (File.Exists(filePath))
                    File.Delete(filePath);

                File.WriteAllText(filePath, "");
            }
        }
    }
}

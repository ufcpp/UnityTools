using CopyDllsAfterBuildLocalTool;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CopyDllsAfterBuildLocalToolUnitTest
{
    public class CopyerTest : IDisposable
    {
        private readonly string _pattern = "*";
        private readonly string[] _excludes = new[] { "UnityEngine", "UnityEditor" };
        private readonly (string folder, string[] files)[] _excludeFolders = new[]
        {
            ("ExcludeDlls", new[] { "System.Buffers.dll", "System.Memory.dll" }),
            ("ExcludeMeta", new[] { "foo.dll", "bar.dll" })
        };

        private readonly string[] _buildOutputs = new[]
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
            "UnityEngine.dll",
            "System.Buffers.dll",
            "System.Memory.dll",
        };
        private readonly string[] _expected = new[]
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

        private readonly string _projectDir;
        private readonly string _targetDir;
        private readonly string _settingsFile;
        private readonly string _settingsFilePath;
        private readonly string _destinationDir;

        // setup
        public CopyerTest()
        {
            var random = Guid.NewGuid().ToString();
            _projectDir = Path.Combine(Path.GetTempPath(), "CopyDllsTest", random);
            _targetDir = Path.Combine(Path.GetTempPath(), "CopyDllsTest", random, "bin", "Debug", "net5.0");
            _destinationDir = Path.Combine(Path.GetTempPath(), "CopyDllsTest", random, "destinations", "Dlls");
            _settingsFile = "CopySettings.json";
            _settingsFilePath = Path.Combine(_projectDir, _settingsFile);

            if (!Directory.Exists(_projectDir))
                Directory.CreateDirectory(_projectDir);
        }

        // teardown
        public void Dispose()
        {
            if (Directory.Exists(_projectDir))
                Directory.Delete(_projectDir, true);
        }

        [Fact]
        public void GetSettings_Should_Deserialize()
        {
            var json = $@"
{{
""destination"": ""{_destinationDir}"",
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

            var copyer = new Copyer(_projectDir);
            var settings = copyer.GetSettings(_settingsFile);
            Assert.Equal(CopySettings.SafeJsonStringReplace(_destinationDir), settings.Destination);
            Assert.Equal(_pattern, settings.Pattern);
            Assert.Equal(_excludes, settings.Excludes);
            Assert.Equal(_excludeFolders.Select(x => x.folder).ToArray(), settings.ExcludeFolders);
        }

        [Fact]
        public void GetSettings_Missing_Should_Fail()
        {
            // missing CopySettings.json path should use default settings
            var copyer = new Copyer(_projectDir);
            Assert.Throws<FileNotFoundException>(() => copyer.GetSettings(_settingsFile));
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
""destination"": ""{_destinationDir}"",
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

            var copyer = new Copyer(_projectDir);
            var settings = copyer.GetSettings(_settingsFile);
            var excludes = copyer.GetExcludes(settings.Excludes, settings.ExcludeFolders);
            Assert.Equal(expected, excludes.OrderBy(x => x).ToArray());
            Assert.Equal(6, excludes.Count()); // exclude(2) + exclude_folders(4)
        }

        [Fact]
        public void GetExclude_Should_Ignore_metafile()
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
""destination"": ""{_destinationDir}"",
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

            var copyer = new Copyer(_projectDir);
            var settings = copyer.GetSettings(_settingsFile);
            var excludes = copyer.GetExcludes(settings.Excludes, settings.ExcludeFolders);
            Assert.Equal(expected, excludes.OrderBy(x => x).ToArray());
            Assert.Equal(4, excludes.Count()); // exclude(2) + exclude_folders(4) - .meta excludes(2)
        }

        [Fact]
        public void GetExclude_Should_Distinct()
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
""destination"": ""{_destinationDir}"",
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

            var copyer = new Copyer(_projectDir);
            var settings = copyer.GetSettings(_settingsFile);
            var excludes = copyer.GetExcludes(settings.Excludes, settings.ExcludeFolders);
            Assert.Equal(expected, excludes.OrderBy(x => x).ToArray());
            Assert.Equal(4, excludes.Count()); // exclude(2) + exclude_folders(4) - distinct(2)
        }

        [Fact]
        public void GetExclude_Should_Distinct2()
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
""destination"": ""{_destinationDir}"",
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

            var copyer = new Copyer(_projectDir);
            var settings = copyer.GetSettings(_settingsFile);
            var excludes = copyer.GetExcludes(settings.Excludes, settings.ExcludeFolders);
            Assert.Equal(expected, excludes.OrderBy(x => x).ToArray());
            Assert.Equal(2, excludes.Count()); // exclude(2) + exclude_folders(4) - distinct(4)
        }

        [Fact]
        public void CopyDlls_To_Empty_Destination_Should_Success()
        {
            CreateTargetDir(_buildOutputs);
            CreateExcludes(_excludes);
            foreach (var exclude in _excludeFolders)
            {
                CreateExcludeFolders(exclude.folder, exclude.files);
            }

            Assert.False(Directory.Exists(_destinationDir));

            var json = $@"
{{
""destination"": ""{_destinationDir}"",
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

            var copyer = new Copyer(_projectDir);
            var settings = copyer.GetSettings(_settingsFile);
            var excludes = copyer.GetExcludes(settings.Excludes, settings.ExcludeFolders);
            copyer.CopyDlls(_targetDir, settings.Destination, settings.Pattern, excludes);
            var actual = Directory.GetFiles(settings.Destination).Select(x => Path.GetFileName(x)).OrderBy(x => x).ToArray();
            Assert.Equal(_expected, actual);
        }

        [Fact]
        public async Task CopyDlls_Should_Skip_Copy_When_BinaryMatch()
        {
            var destinations = _expected;

            CreateDestinationFolders(_destinationDir, destinations);
            // map of Dictionary<string, bytes[]>(fileName, bytes)
            var expectedFiles = Directory.GetFiles(_destinationDir).ToDictionary(kv => Path.GetFileName(kv), kv => (File.ReadAllBytes(kv), File.GetLastWriteTime(kv)));
            // Linux FileSystem suspect to not update timestamp for too short update, wait bit.
            await Task.Delay(TimeSpan.FromMilliseconds(1));

            CreateTargetDir(_buildOutputs);
            CreateExcludes(_excludes);
            foreach (var exclude in _excludeFolders)
            {
                CreateExcludeFolders(exclude.folder, exclude.files);
            }

            Assert.True(Directory.Exists(_destinationDir));

            var json = $@"
{{
""destination"": ""{_destinationDir}"",
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

            var copyer = new Copyer(_projectDir);
            var settings = copyer.GetSettings(_settingsFile);
            var excludes = copyer.GetExcludes(settings.Excludes, settings.ExcludeFolders);
            copyer.CopyDlls(_targetDir, settings.Destination, settings.Pattern, excludes);
            var actual = Directory.GetFiles(settings.Destination).ToDictionary(kv => Path.GetFileName(kv), kv => (File.ReadAllBytes(kv), File.GetLastWriteTime(kv)));
            Assert.Equal(_expected, actual.Keys.OrderBy(x => x).ToArray());
            foreach (var item in actual)
            {
                // binary match
                Assert.Equal(expectedFiles[item.Key].Item1, item.Value.Item1);
                // date not updated
                Assert.Equal(expectedFiles[item.Key].Item2, item.Value.Item2);
            }
        }

        [Fact]
        public async Task CopyDlls_Should_Copy_When_BinaryMissmatch()
        {
            var destinations = _expected;

            CreateDestinationFolders(_destinationDir, destinations, Guid.NewGuid().ToString());
            // map of Dictionary<string, bytes[]>(fileName, bytes)
            var expectedFiles = Directory.GetFiles(_destinationDir).ToDictionary(kv => Path.GetFileName(kv), kv => (File.ReadAllBytes(kv), File.GetLastWriteTime(kv)));
            // Linux FileSystem suspect to not update timestamp for too short update, wait bit.
            await Task.Delay(TimeSpan.FromMilliseconds(1));

            CreateTargetDir(_buildOutputs);
            CreateExcludes(_excludes);
            foreach (var exclude in _excludeFolders)
            {
                CreateExcludeFolders(exclude.folder, exclude.files);
            }

            Assert.True(Directory.Exists(_destinationDir));

            var json = $@"
{{
""destination"": ""{_destinationDir}"",
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

            var copyer = new Copyer(_projectDir);
            var settings = copyer.GetSettings(_settingsFile);
            var excludes = copyer.GetExcludes(settings.Excludes, settings.ExcludeFolders);
            copyer.CopyDlls(_targetDir, settings.Destination, settings.Pattern, excludes);
            var actual = Directory.GetFiles(settings.Destination).ToDictionary(kv => Path.GetFileName(kv), kv => (File.ReadAllBytes(kv), File.GetLastWriteTime(kv)));
            Assert.Equal(_expected, actual.Keys.OrderBy(x => x).ToArray());
            foreach (var item in actual)
            {
                // binary updated
                Assert.NotEqual(expectedFiles[item.Key].Item1, item.Value.Item1);
                // date updated
                Assert.True(expectedFiles[item.Key].Item2 < item.Value.Item2);
            }
        }

        [Fact]
        public void CopyDlls_Should_Delete_GarbageFile_On_Destination()
        {
            var garbages = new[]
            {
                "Test.txt",
                "Test.meta",
                "Gomi",
                "Gomi.meta",
                "Foo.dll",
                "Bar.dll",
            };
            var destinations = garbages.Concat(_expected).ToArray();

            CreateDestinationFolders(_destinationDir, destinations);
            // map of Dictionary<string, bytes[]>(fileName, bytes)
            var expectedFiles = Directory.GetFiles(_destinationDir)
                .Where(x => !garbages.Contains(Path.GetFileName(x)))
                .ToDictionary(kv => Path.GetFileName(kv), kv => (File.ReadAllBytes(kv), File.GetLastWriteTime(kv)));

            CreateTargetDir(_buildOutputs);
            CreateExcludes(_excludes);
            foreach (var exclude in _excludeFolders)
            {
                CreateExcludeFolders(exclude.folder, exclude.files);
            }

            Assert.True(Directory.Exists(_destinationDir));

            var json = $@"
{{
""destination"": ""{_destinationDir}"",
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

            var copyer = new Copyer(_projectDir);
            var settings = copyer.GetSettings(_settingsFile);
            var excludes = copyer.GetExcludes(settings.Excludes, settings.ExcludeFolders);
            copyer.CopyDlls(_targetDir, settings.Destination, settings.Pattern, excludes);
            var actual = Directory.GetFiles(settings.Destination).ToDictionary(kv => Path.GetFileName(kv), kv => (File.ReadAllBytes(kv), File.GetLastWriteTime(kv)));
            Assert.Equal(_expected, actual.Keys.OrderBy(x => x).ToArray());
            foreach (var item in actual)
            {
                // binary match
                Assert.Equal(expectedFiles[item.Key].Item1, item.Value.Item1);
                // date not updated
                Assert.Equal(expectedFiles[item.Key].Item2, item.Value.Item2);
            }
        }

        private void CreateTargetDir(IEnumerable<string> fileNames, string content = "")
        {
            if (!Directory.Exists(_targetDir))
                Directory.CreateDirectory(_targetDir);

            foreach (var file in fileNames)
            {
                var filePath = Path.Combine(_targetDir, file);
                if (File.Exists(filePath))
                    File.Delete(filePath);

                File.WriteAllText(filePath, content);
            }
        }
        private void CreateExcludes(IEnumerable<string> fileNames)
        {
            foreach (var file in fileNames)
            {
                var filePath = Path.Combine(_projectDir, file);
                if (File.Exists(filePath))
                    File.Delete(filePath);

                File.WriteAllText(filePath, "");
            }
        }
        private void CreateExcludeFolders(string directoryName, IEnumerable<string> fileNames)
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
        private void CreateDestinationFolders(string path, IEnumerable<string> fileNames, string content = "")
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            foreach (var file in fileNames)
            {
                var filePath = Path.Combine(path, file);
                if (File.Exists(filePath))
                    File.Delete(filePath);

                File.WriteAllText(filePath, content);
            }

        }
    }
}

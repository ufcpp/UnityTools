using CopyDllsAfterBuildLocalTool;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace CopyDllsAfterBuildLocalToolUnitTest
{
    public class CommandTest
    {
        private readonly string _basePath;
        private readonly string _projectDir;
        private readonly string _targetDir;
        private readonly string _settings;

        // setup
        public CommandTest()
        {
            var random = Guid.NewGuid().ToString();
            _basePath = Path.Combine(Path.GetTempPath(), "CommandTest", random);
            _projectDir = Path.Combine(_basePath, "sln", "projectDir");
            _targetDir = Path.Combine(_basePath, "sln", "projectDir", "bin", "Debug", "net5.0");
            _settings = Path.Combine(_projectDir, "CopySettings.json");

            if (!Directory.Exists(_projectDir))
                Directory.CreateDirectory(_projectDir);
        }

        // teardown
        public void Dispose()
        {
            if (Directory.Exists(_basePath))
                Directory.Delete(_basePath, true);
        }

        [Fact]
        public void InitCommandTest()
        {
            var expected = CopySettings.LoadJson(CopySettings.GetTemplateJson());

            var program = new Program();
            program.Init(_projectDir);
            Assert.True(File.Exists(_settings));

            var generated = File.ReadAllText(_settings, Encoding.UTF8);
            var actual = CopySettings.LoadJson(generated);
            Assert.Equal(expected.Destination, actual.Destination);
            Assert.Equal(expected.Pattern, actual.Pattern);
            Assert.Equal(expected.Excludes, actual.Excludes);
            Assert.Equal(expected.ExcludeFolders, actual.ExcludeFolders);
        }

        [Fact]
        public void ValidateCommandTest()
        {
            var program = new Program();
            program.Init(_projectDir);
            Assert.Equal(0, program.Validate(_projectDir));
        }

        [Fact]
        public void RunCommandTest()
        {
            static Dictionary<string, (byte[] bytes, DateTime date)> getActual(string destination)
            {
                var actuals = Directory.GetFiles(destination).ToDictionary(kv => Path.GetFileName(kv), kv => (File.ReadAllBytes(kv), File.GetLastWriteTime(kv)));
                return actuals;
            }

            var program = new Program();
            program.Init(_projectDir);
            var settings = CopySettings.LoadJson(File.ReadAllText(_settings, Encoding.UTF8));
            var destinationPath = Path.Combine(_projectDir, settings.Destination);
            var excludes = settings.Excludes;
            var buildOutputs = new[]
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
                "System.Buffers.dll",
                "System.Memory.dll",
            }.OrderBy(x => x).ToArray();
            CreateTargetDir(buildOutputs);
            CreateExcludes(excludes);

            // 1st run. copy to empty destination
            program.Run(_projectDir, _targetDir);
            var actual = getActual(destinationPath);
            var i = 0;
            foreach (var item in actual)
            {
                // File successfully copy
                Assert.Equal(expected[i++], item.Key);
            }
            var expected2 = getActual(destinationPath);

            // 2nd run. without change
            {
                program.Run(_projectDir, _targetDir);
                var actual2nd = getActual(destinationPath);
                Assert.Equal(actual.Keys, actual2nd.Keys);
                foreach (var actualItem in actual2nd)
                {
                    // binary match
                    Assert.Equal(expected2[actualItem.Key].bytes, actualItem.Value.bytes);
                    // date not updated
                    Assert.Equal(expected2[actualItem.Key].date, actualItem.Value.date);
                }
            }

            // 3rd run. add garbage file to destination
            {
                File.WriteAllText(Path.Combine(destinationPath, "a"), "abcde");
                var before3rd = getActual(destinationPath);
                program.Run(_projectDir, _targetDir);
                var actual3rd = getActual(destinationPath);
                Assert.Equal(actual.Keys, actual3rd.Keys); // deleted!
                foreach (var actualItem in actual3rd)
                {
                    // binary match
                    Assert.Equal(expected2[actualItem.Key].bytes, actualItem.Value.bytes);
                    // date not updated
                    Assert.Equal(expected2[actualItem.Key].date, actualItem.Value.date);
                }
            }

            // 4th run. change source dll.
            {
                var changed = buildOutputs.Take(3);
                CreateTargetDir(changed, Guid.NewGuid().ToString());
                program.Run(_projectDir, _targetDir);
                var actual4th = getActual(destinationPath);
                Assert.Equal(actual.Keys, actual4th.Keys);
                foreach (var actualItem in actual4th)
                {
                    if (changed.Contains(actualItem.Key))
                    {
                        // binary missmatch
                        Assert.NotEqual(expected2[actualItem.Key].bytes, actualItem.Value.bytes);
                        // date updated
                        Assert.True(expected2[actualItem.Key].date < actualItem.Value.date);
                    }
                    else
                    {
                        // binary match
                        Assert.Equal(expected2[actualItem.Key].bytes, actualItem.Value.bytes);
                        // date not updated
                        Assert.Equal(expected2[actualItem.Key].date, actualItem.Value.date);
                    }
                }
            }
            var expected3 = getActual(destinationPath);

            // 5th run. add file to source.
            {
                var adds = new[] { "Ping.dll", "Pong.dll" };
                CreateTargetDir(adds, Guid.NewGuid().ToString());
                program.Run(_projectDir, _targetDir);
                var actual5th = getActual(destinationPath);
                Assert.Equal(actual.Keys.Concat(adds).OrderBy(x => x).ToArray(), actual5th.Keys.ToArray());
                foreach (var actualItem in actual5th)
                {
                    if (adds.Contains(actualItem.Key))
                    {
                        // binary is not 0
                        Assert.True(actualItem.Value.bytes.Length > 0);
                    }
                    else
                    {
                        // binary match
                        Assert.Equal(expected3[actualItem.Key].bytes, actualItem.Value.bytes);
                        // date not updated
                        Assert.Equal(expected3[actualItem.Key].date, actualItem.Value.date);
                    }
                }
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
    }
}

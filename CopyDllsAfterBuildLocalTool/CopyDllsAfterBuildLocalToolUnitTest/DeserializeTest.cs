using CopyDllsAfterBuildLocalTool;
using System;
using System.IO;
using Xunit;

namespace CopyDllsAfterBuildLocalToolsUnitTest
{
    public class DeserializeTest
    {
        private readonly string _destination = "../Dlls";
        private readonly string _fullPathDestination = Path.GetFullPath("../Dlls");
        private readonly string _pattern = "*";
        private readonly string[] _excludes = new[] { "UnityEngine", "UnityEditor" };
        private readonly string[] _excludeFolders = new[] { "ExcludeDlls", "ExcludeDlls2" };

        [Fact]
        public void FullJson()
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
    ""{_excludeFolders[0]}"",
    ""{_excludeFolders[1]}""
  ]
}}";
            var settings = CopySettings.LoadJson(json);
            Assert.Equal(_destination, settings.Destination);
            Assert.Equal(_pattern, settings.Pattern);
            Assert.Equal(_excludes, settings.Excludes);
            Assert.Equal(_excludeFolders, settings.ExcludeFolders);
        }

        [Fact]
        public void FullPath_Destination_Allow()
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
    ""{_excludeFolders[0]}"",
    ""{_excludeFolders[1]}""
  ]
}}";
            var settings = CopySettings.LoadJson(json);
            Assert.Equal(CopySettings.SafeJsonStringReplace(_destination), settings.Destination);
            Assert.Equal("*", settings.Pattern);
            Assert.Equal(_excludes, settings.Excludes);
            Assert.Equal(_excludeFolders, settings.ExcludeFolders);
        }

        [Fact]
        public void Null_Pattern_Allowed()
        {
            var json = $@"
{{
  ""destination"": ""{_destination}"",
  ""excludes"": [
    ""{_excludes[0]}"",
    ""{_excludes[1]}""
  ],
  ""exclude_folders"": [
    ""{_excludeFolders[0]}"",
    ""{_excludeFolders[1]}""
  ]
}}";
            var settings = CopySettings.LoadJson(json);
            Assert.Equal(_destination, settings.Destination);
            Assert.Equal("*", settings.Pattern);
            Assert.Equal(_excludes, settings.Excludes);
            Assert.Equal(_excludeFolders, settings.ExcludeFolders);
        }

        [Fact]
        public void Null_Excludes_Allowed()
        {
            var json = $@"
{{
  ""destination"": ""{_destination}"",
  ""pattern"": ""{_pattern}"",
  ""exclude_folders"": [
    ""{_excludeFolders[0]}"",
    ""{_excludeFolders[1]}""
  ]
}}";
            var settings = CopySettings.LoadJson(json);
            Assert.Equal(_destination, settings.Destination);
            Assert.Equal(_pattern, settings.Pattern);
            Assert.Equal(Array.Empty<string>(), settings.Excludes);
            Assert.Equal(_excludeFolders, settings.ExcludeFolders);
        }
        [Fact]
        public void Null_ExcludeFolders_Allowed()
        {
            var json = $@"
{{
  ""destination"": ""{_destination}"",
  ""pattern"": ""{_pattern}"",
  ""excludes"": [
    ""{_excludes[0]}"",
    ""{_excludes[1]}""
  ]
}}";
            var settings = CopySettings.LoadJson(json);
            Assert.Equal(_destination, settings.Destination);
            Assert.Equal(_pattern, settings.Pattern);
            Assert.Equal(_excludes, settings.Excludes);
            Assert.Equal(Array.Empty<string>(), settings.ExcludeFolders);
        }

        // special json format
        [Fact]
        public void JsonFormat_TrailingCommaJson_Allowed()
        {
            var json = $@"
{{
  ""destination"": ""{_destination}"",
  ""pattern"": ""{_pattern}"",
  ""excludes"": [
    ""{_excludes[0]}"",
    ""{_excludes[1]}"",
  ],
  ""exclude_folders"": [
    ""{_excludeFolders[0]}"",
    ""{_excludeFolders[1]}"",
  ]
}}";
            var settings = CopySettings.LoadJson(json);
            Assert.Equal(_destination, settings.Destination);
            Assert.Equal(_pattern, settings.Pattern);
            Assert.Equal(_excludes, settings.Excludes);
            Assert.Equal(_excludeFolders, settings.ExcludeFolders);
        }

        [Fact]
        public void JsonFormat_CommentJson_Allowed()
        {
            var json = $@"
{{
  // this is destination
  ""destination"": ""{_destination}"",
  // this is pattern
  ""pattern"": ""{_pattern}"",
  // this is excludes
  ""excludes"": [
    ""{_excludes[0]}"",
    ""{_excludes[1]}"",
  ],
  // this is exclude_folders
  ""exclude_folders"": [
    ""{_excludeFolders[0]}"",
    ""{_excludeFolders[1]}"",
  ]
}}";
            var settings = CopySettings.LoadJson(json);
            Assert.Equal(_destination, settings.Destination);
            Assert.Equal(_pattern, settings.Pattern);
            Assert.Equal(_excludes, settings.Excludes);
            Assert.Equal(_excludeFolders, settings.ExcludeFolders);
        }

        // fail tests
        [Fact]
        public void Empty_Json_Fail()
        {
            var json = $@"
{{
}}";
            Assert.Throws<ArgumentNullException>(() => CopySettings.LoadJson(json));
        }
        [Fact]
        public void Null_Destination_Fail()
        {
            var json = $@"
{{
  ""pattern"": ""{_pattern}"",
  ""excludes"": [
    ""{_excludes[0]}"",
    ""{_excludes[1]}""
  ],
  ""exclude_folders"": [
    ""{_excludeFolders[0]}"",
    ""{_excludeFolders[1]}""
  ]
}}";
            Assert.Throws<ArgumentNullException>(() => CopySettings.LoadJson(json));
        }

        [Fact]
        public void Null_Excludes_ExcludeFolders_Fail()
        {
            var json = $@"
{{
  ""destination"": ""{_destination}"",
  ""pattern"": ""{_pattern}""
}}";
            Assert.Throws<ArgumentNullException>(() => CopySettings.LoadJson(json));
        }
    }
}

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CopyDllsAfterBuild
{
    public class CopySettings
    {
        private static readonly ILogger logger = Logger.Instance;
        private static readonly JsonSerializerOptions serializeOptions = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            IgnoreNullValues = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };
        /// <summary>
        /// Destination of Path dlls will be copy to.
        /// </summary>
        [JsonPropertyName("destination")]
        public string Destination { get; set; } = "../../../Project/Assets/Dlls";
        /// <summary>
        /// Acceptable pattern is [wildcard + ext] style. This is same as searcPatterns <see cref="System.IO.Directory.EnumerateFiles"/> offers.
        /// </summary>
        [JsonPropertyName("pattern")]
        public string Pattern { get; set; } = "*";
        /// <summary>
        /// Exclude file names you don't want to copy from. Complete file name match when name is end with $, others will be prefix match.
        /// </summary>
        [JsonPropertyName("excludes")]
        public string[] Excludes { get; set; } = new[] { "UnityEngine", "UnityEditor" };
        /// <summary>
        /// Exclude folder names you don't want to copy from. File names in these folders will be added to excludes.
        /// </summary>
        [JsonPropertyName("exclude_folders")]
        public string[] ExcludeFolders { get; set; } = Array.Empty<string>();

        [JsonIgnore]
        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// Load Settings from Json
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <returns></returns>
        public static CopySettings LoadJsonFile(string sourcePath)
        {
            logger.LogDebug($"Loading JSON file from {sourcePath}");
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException($"Settings file not found from path specified. {sourcePath}.");

            var source = File.ReadAllText(sourcePath, Encoding.UTF8);
            logger.LogTrace($"Trying to deserialize settings file\n{source}");

            var serialized = JsonSerializer.Deserialize<CopySettings>(source, serializeOptions);
            if (serialized == null)
                throw new NullReferenceException($"Deserialized {sourcePath}, but result was empty. May be source json is empty.");

            return serialized;
        }
    }
}

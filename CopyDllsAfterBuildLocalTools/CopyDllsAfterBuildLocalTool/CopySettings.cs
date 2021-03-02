using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CopyDllsAfterBuild
{
    /// <summary>
    /// Spec of filename for copy.
    /// [exclude]
    /// if excludes element is end with $, do exact match.
    /// if excludes element is not end with $, do prefix match.
    /// [exclude_folders]
    /// if exclude_folders is found, do exact match with items.
    /// </summary>
    public class CopySettings
    {
        private static readonly ILogger logger = Logger.Instance;
        private static readonly JsonSerializerOptions serializeOptions = new JsonSerializerOptions
        {
            IgnoreNullValues = true, // exclude_folders is often not defined.
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // follow to normal JSON style.
            AllowTrailingCommas = true, // allow loose JSON format
            ReadCommentHandling = JsonCommentHandling.Skip, // allow JSON with Comments.
        };

        /// <summary>
        /// Destination Path which dlls will be copy to.
        /// </summary>
        [JsonPropertyName("destination")]
        public string? Destination { get; set; }
        /// <summary>
        /// Acceptable pattern is [wildcard + extentions] style. This is same as searcPatterns <see cref="System.IO.Directory.EnumerateFiles"/> offers.
        /// </summary>
        [JsonPropertyName("pattern")]
        public string Pattern { get; set; } = "*";
        /// <summary>
        /// Exclude file names you don't want to copy from. Exact file name match when name is end with $, others will be prefix match.
        /// </summary>
        [JsonPropertyName("excludes")]
        public string[]? Excludes { get; set; }
        /// <summary>
        /// Exclude folder names you don't want to copy from. File names in these folders will be added to excludes.
        /// </summary>
        [JsonPropertyName("exclude_folders")]
        public string[]? ExcludeFolders { get; set; }

        [JsonIgnore]
        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// Load Settings from Json
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <returns></returns>
        public static CopySettings LoadJson(string json)
        {
            logger.LogTrace($"Trying to deserialize settings file\n{json}");
            var serialized = JsonSerializer.Deserialize<CopySettings>(json, serializeOptions);

            if (serialized == null)
                throw new NullReferenceException($"Deserialized json but result was empty. May be source json is empty.");
            if (serialized.Destination == null)
                throw new ArgumentNullException($"You can not empty destination property.");
            if (serialized.Excludes == null && serialized.ExcludeFolders == null)
                throw new ArgumentNullException($"You can not empty both exclude and exclude_folders property.");

            // sset empty array when null. can not use property init, because array is fixed size...
            serialized.Excludes ??= Array.Empty<string>();
            serialized.ExcludeFolders ??= Array.Empty<string>();

            return serialized;
        }
    }
}

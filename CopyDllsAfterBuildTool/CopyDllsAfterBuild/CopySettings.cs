﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CopyDllsAfterBuild
{
    public class CopySettings
    {
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
        /// Exclude file names you don't want to copy from.
        /// </summary>
        [JsonPropertyName("excludes")]
        public string[] Excludes { get; set; } = new[] { "UnityEngine", "UnityEditor" };
        /// <summary>
        /// Exclude folder names you don't want to copy from. Files in these folders will be added to excludes.
        /// </summary>
        [JsonPropertyName("exclude_folders")]
        public string[] ExcludeFolders { get; set; } = Array.Empty<string>();

        public static CopySettings LoadJsonFile(string source)
        {
            var serialized = JsonSerializer.Deserialize<CopySettings>(source, new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                IgnoreNullValues = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReadCommentHandling = JsonCommentHandling.Skip,
            });
            if (serialized == null)
                throw new NullReferenceException($"Deserialize {source}, but result was null. May be source json is empty.");
            return serialized;
        }
    }
}

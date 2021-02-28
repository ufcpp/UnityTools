using Cocona;
using System;
using System.IO;

namespace CopyDllsAfterBuild
{
    partial class Program
    {
        private static readonly ILogger logger = Logger.Instance;
        static void Main(string[] args) => CoconaLiteApp.Run<Program>(args);

        /// <summary>
        /// Run CopyDlls operation.
        /// </summary>
        /// <param name="projectDir">Project Root directory. Should be $(ProjectDir)</param>
        /// <param name="targetDir">Project Build output directory. Should be $(TargetDir)</param>
        /// <param name="settingFile">JSON file to specify settings. Make sure file is UTF8.</param>
        public void Run(string projectDir, string targetDir, string settingFile = "CopySettings.json")
        {
            try
            {
                var trimedProjectDir = projectDir.TrimStart('"').TrimEnd('"');
                var trimedTargetdir = targetDir.TrimStart('"').TrimEnd('"');

                var build = new PostBuild(trimedProjectDir);
                var settings = build.GetSettings(settingFile);
                var excludes = build.GetExcludes(settings.Excludes!, settings.ExcludeFolders!);
                var dllPath = Path.Combine(trimedProjectDir, settings.Destination!);

                build.CopyDlls(trimedTargetdir, dllPath, settings.Pattern, excludes);
            }
            catch (Exception ex)
            {
                logger.LogCritical($"{ex.Message} {ex.GetType().FullName} {ex.StackTrace}");
                logger.LogInformation("Set COPYDLLS_LOGLEVEL=Debug or Trace to see more detail logs.");
                throw;
            }
        }
    }
}

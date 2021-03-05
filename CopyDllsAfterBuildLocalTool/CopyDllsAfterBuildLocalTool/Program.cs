using Cocona;
using System;
using System.IO;
using System.Text;

namespace CopyDllsAfterBuildLocalTool
{
    partial class Program
    {
        private static readonly ILogger logger = Logger.Instance;
        static void Main(string[] args) => CoconaLiteApp.Run<Program>(args);

        /// <summary>
        /// Run CopyDlls operation.
        /// </summary>
        /// <param name="projectDir">--project-dir. Project Root directory. Should be $(ProjectDir)</param>
        /// <param name="targetDir">--target-dir. Project Build output directory. Should be $(TargetDir)</param>
        /// <param name="settingFile">--setting-file. JSON file to specify settings. Make sure file is UTF8.</param>
        public void Run(string projectDir, string targetDir, string settingFile = "CopySettings.json")
        {
            try
            {
                var trimedProjectDir = TrimInput(projectDir);
                var trimedTargetdir = TrimInput(targetDir);

                var copyer = new Copyer(trimedProjectDir);
                var settings = copyer.GetSettings(settingFile);
                var excludes = copyer.GetExcludes(settings.Excludes!, settings.ExcludeFolders!);
                var destination = Path.Combine(trimedProjectDir, settings.Destination!);

                copyer.Sync(trimedTargetdir, destination, settings.Pattern, excludes);
            }
            catch (Exception ex)
            {
                logger.LogCritical($"{ex.Message} {ex.GetType().FullName} {ex.StackTrace}");
                logger.LogInformation("NOTE: Set Environment variable COPYDLLS_LOGLEVEL=Debug or Trace to see more detail logs.");
                throw;
            }
        }

        /// <summary>
        /// Initialize environment. Output initial settings JSON file to path specified.
        /// </summary>
        /// <param name="projectDir">--project-dir. Project Root directory. Should be $(ProjectDir)</param>
        /// <param name="settingFile">--setting-file. JSON file settings to output.</param>
        public void init(string projectDir = ".", string settingFile = "CopySettings.json")
        {
            var trimedProjectDir = TrimInput(projectDir);

            var template = CopySettings.GetTemplateJson();
            var path = Path.Combine(trimedProjectDir, settingFile);
            logger.LogInformation($"Output template Settings. Output {settingFile}");
            File.WriteAllText(path, template, Encoding.UTF8);
        }

        /// <summary>
        /// Confirm setting JON file is valid or not.
        /// </summary>
        /// <param name="projectDir"></param>
        /// <param name="settingFile"></param>
        public void Validate(string projectDir = ".", string settingFile = "CopySettings.json")
        {
            var trimedProjectDir = TrimInput(projectDir);
            var copyer = new Copyer(trimedProjectDir);
            copyer.GetSettings(settingFile);
        }

        private string TrimInput(string input) => input.TrimStart('"').TrimEnd('"');
    }
}

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
        /// <param name="settingFile">--setting-file. JSON setting file to load. Make sure file is UTF8.</param>
        [Command(Description = "Run CopyDlls operation.")]
        public int Run(string projectDir, string targetDir, string settingFile = "CopySettings.json")
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
                return 0;
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed run Copy DLLs. {ex.Message} ({ex.GetType().FullName})");
                logger.LogCritical($"{ex.StackTrace}"); // output detail every time.
                logger.LogInformationIfNotDebug("NOTE: Set Environment variable COPYDLLS_LOGLEVEL=Debug or Trace to see more detail logs.");
                return 1;
            }
        }

        /// <summary>
        /// Initialize environment. Output initial settings JSON file to path specified.
        /// </summary>
        /// <param name="projectDir">--project-dir. Project Root directory. Should be $(ProjectDir)</param>
        /// <param name="settingFile">--setting-file. JSON file settings to output.</param>
        [Command(Description = "Initialize environment. Output initial settings JSON file to path specified.")]
        public void Init(string projectDir = ".", string settingFile = "CopySettings.json")
        {
            var trimedProjectDir = TrimInput(projectDir);

            var template = CopySettings.GetTemplateJson();
            var path = Path.Combine(trimedProjectDir, settingFile);
            logger.LogInformation($"Output template Settings. path: {Path.GetFullPath(path)}");
            File.WriteAllText(path, template, Encoding.UTF8);
        }

        /// <summary>
        /// Confirm setting JSON file is valid or not.
        /// </summary>
        /// <param name="projectDir">--project-dir. Project Root directory. Should be $(ProjectDir)</param>
        /// <param name="settingFile">--setting-file. JSON setting file to load. Make sure file is UTF8.</param>
        [Command(Description = "Confirm setting JSON file is valid or not.")]
        public int Validate(string projectDir = ".", string settingFile = "CopySettings.json")
        {
            try
            {
                var trimedProjectDir = TrimInput(projectDir);
                var copyer = new Copyer(trimedProjectDir);
                var settings = copyer.GetSettings(settingFile);
                if (settings == null)
                    throw new NullReferenceException("Tried load setting, but was null.");

                logger.LogInformation($"Successfully load settings.");
                logger.LogInformation(settings.ToString());
                return 0;
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed load settings. {ex.Message} ({ex.GetType().FullName})");
                logger.LogDebug($"{ex.StackTrace}");
                logger.LogInformationIfNotDebug("NOTE: Set Environment variable COPYDLLS_LOGLEVEL=Debug or Trace to see more detail logs.");
                return 1;
            }
        }

        private string TrimInput(string input) => input.TrimStart('"').TrimEnd('"');
    }
}

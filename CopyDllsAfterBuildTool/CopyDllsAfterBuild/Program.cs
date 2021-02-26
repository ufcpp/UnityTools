using Cocona;
using System;
using System.IO;

namespace CopyDllsAfterBuild
{
    class Program
    {
        static void Main(string[] args)
        {
            CoconaLiteApp.Run<Program>(args);
        }

        public void Run(string projectDir, string targetDir, string settingFile = "CopySettings.json")
        {
            var trimedProjectDir = projectDir.TrimStart('"').TrimEnd('"');
            var trimedTargetdir = targetDir.TrimStart('"').TrimEnd('"');

            var build = new PostBuild(trimedProjectDir);
            var settings = build.GetSettings(settingFile);
            var excludes = build.GetExcludes(settings.Excludes, settings.ExcludeFolders);
            var dllPath = Path.Combine(trimedProjectDir, settings.Destination);

            build.CopyDlls(trimedTargetdir, dllPath, settings.Pattern, excludes);
        }
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ProjectModels
{
    /// <summary>
    /// Manipulate a *.sln file.
    /// </summary>
    public class Solution : Item
    {
        public Solution(string slnPath)
            : base(slnPath)
        {
        }

        public IEnumerable<Csproj> CsharpProjcts => _csprojs ?? (_csprojs = GetCsProject().ToArray());
        private IEnumerable<Csproj> _csprojs;

        private static readonly Regex regProject = new Regex(@" = "".*?"", ""(?<csproj>.*?\.csproj)""");

        /// <summary>
        /// Enumerate all projects in a solution.
        /// </summary>
        /// <param name="slnPath"></param>
        /// <returns></returns>
        private IEnumerable<Csproj> GetCsProject()
        {
            var slnLines = File.ReadAllLines(Path);

            foreach (var line in slnLines)
            {
                var m = regProject.Match(line);
                if (!m.Success) continue;

                var relative = m.Groups["csproj"].Value;
                var csprojPath = System.IO.Path.Combine(Folder, relative);
                if (!File.Exists(csprojPath)) continue;

                yield return new Csproj(Folder, relative);
            }
        }

        public void MigrateToProjectJson()
        {
            foreach (var csproj in CsharpProjcts.Where(p => p.HasPackagesConfig))
            {
                if (csproj.MigrateToProjectJson())
                    csproj.Save();
            }
        }

        private string WrapFolder => System.IO.Path.Combine(Folder, "wrap");

        public void GenerateWrapJson()
        {
            var wrapFolder = WrapFolder;
            Directory.CreateDirectory(wrapFolder);

            foreach (var csproj in CsharpProjcts.Where(p => p.OutputType == CsprojOutputType.Library))
            {
                csproj.GenerateWrapJson(wrapFolder);
            }
        }
    }
}

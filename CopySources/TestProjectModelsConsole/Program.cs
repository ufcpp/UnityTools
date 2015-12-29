using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectModels;
using System.IO;

namespace TestProjectModelsConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var sln = new Solution(@"your solution.sln");
            ShowInfo(sln);
        }

        private static void ShowInfo(Solution sln)
        {
            foreach (var proj in sln.CsharpProjcts)
            {
                Console.WriteLine("path");

                Console.WriteLine("    " + proj.Path);
                Console.WriteLine("    " + proj.RelativePath);

                Console.WriteLine("packages in project.json");

                if (proj.HasProjectJson)
                    foreach (var pkg in proj.ProjectJson.Packages)
                    {
                        Console.WriteLine("    " + pkg.Id);
                    }

                Console.WriteLine("packages in packages.config");

                if (proj.HasPackagesConfig)
                    foreach (var pkg in proj.PackagesConfig.Packages)
                    {
                        Console.WriteLine("    " + pkg.Id);
                    }

                Console.WriteLine("C# files");

                foreach (var item in proj.CsFiles)
                {
                    Console.WriteLine("    " + item.RelativePath);
                }

                if (proj.CsFiles.Any(cs => !System.IO.File.Exists(cs.Path)))
                    Console.WriteLine("error: file not found");

                Console.Read();
            }
        }
    }
}

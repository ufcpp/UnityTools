using ProjectModels;
using System;
using System.IO;
using System.Linq;

namespace CopySources
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length < 2)
            {
                Console.WriteLine(@"CopySource [slnPath] [dllFolder]

slnPath  : ソリューションのファイルのパス
dllFolder: DLLを置いているフォルダーのパス。
");
                return;
            }

            var slnPath = args[0];
            var dllFolder = args[1];

            Console.WriteLine($@"ソースコードをコピーします
sln : {slnPath}
dlls: {dllFolder}
");

            var sln = new Solution(slnPath);
            var allDllNames = Directory.GetFiles(dllFolder, "*.dll").Select(f => Path.GetFileNameWithoutExtension(f)).ToArray();
            var projs = sln.CsharpProjcts.Where(p => allDllNames.Contains(p.NameWithoutExtension));
            var dllsNames = allDllNames.Intersect(projs.Select(p => p.NameWithoutExtension));

            Console.WriteLine("DLL 削除");

            foreach (var n in dllsNames)
            {
                Console.WriteLine(n);
                foreach (var path in Directory.GetFiles(dllFolder, n + ".*"))
                {
                    File.Delete(path);
                }
            }

            Console.WriteLine("コピー開始");

            foreach (var p in projs)
            {
                Console.WriteLine(p.NameWithoutExtension);
                p.CopySources(dllFolder, true);
            }
        }
    }
}

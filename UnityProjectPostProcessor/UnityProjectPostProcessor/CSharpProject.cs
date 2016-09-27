using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace UnityProjectPostProcessor
{
    /// <summary>
    /// Visual Studio の C# プロジェクト。
    /// </summary>
    public class CSharpProject : Project
    {
        private string Directory => System.IO.Path.GetDirectoryName(Path);

        /// <summary>
        /// このプロジェクトが参照している別 C# プロジェクトの一覧。
        /// </summary>
        /// <remarks>
        /// 同一ソリューションの別 csproj への参照のみ。
        /// </remarks>
        public IEnumerable<CSharpProject> CSharpProjectReferences => GetCSharpProjectReferences(Directory, Content);

        /// <summary>
        /// 自分 + 参照している .csproj プロジェクトを再帰的にすべて返す。
        /// </summary>
        /// <returns></returns>
        public IEnumerable<CSharpProject> GetAllCSharpProjectReferences() => GetRecursively(p => p.CSharpProjectReferences);

        private IEnumerable<CSharpProject> GetRecursively(Func<CSharpProject, IEnumerable<CSharpProject>> getChildren)
            => GetRecursively(this, getChildren, new HashSet<CSharpProject>());

        private static IEnumerable<CSharpProject> GetRecursively(CSharpProject project, Func<CSharpProject, IEnumerable<CSharpProject>> getChildren, HashSet<CSharpProject> set)
        {
            yield return project;

            foreach (var x in getChildren(project))
            {
                if (set.Contains(x)) continue;

                set.Add(x);
                foreach (var child in GetRecursively(x, getChildren, set))
                    yield return child;
            }
        }

        /// <summary>
        /// このプロジェクトが参照している共有プロジェクトの一覧。
        /// </summary>
        public IEnumerable<SharedProject> SharedProjectReferences => GetSharedProjectReferences(Directory, Content);

        /// <summary>
        /// このプロジェクトが参照している共有プロジェクトの一覧。
        /// 再帰探索版。
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SharedProject> GetAllSharedProjectReferences()
            => GetAllCSharpProjectReferences()
            .SelectMany(p => p.SharedProjectReferences)
            .Distinct();

        /// <summary>
        /// このプロジェクトが参照しているアナライザーの一覧。
        /// </summary>
        public IEnumerable<string> AnalyzerReferences => GetAnalyzerReferences(Directory, Content);

        /// <summary>
        /// このプロジェクトが参照している DLL の一覧。
        /// </summary>
        public IEnumerable<string> References => GetAssemblyReferences(Content);

        /// <summary>
        /// <see cref="Project.TypeGuid"/>
        /// </summary>
        public override Guid TypeGuid => new Guid("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC");
        
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="path">プロジェクトのパス。</param>
        public CSharpProject(string path) : this(path, File.ReadAllText(path)) { }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="path">プロジェクトのパス。</param>
        /// <param name="content">プロジェクトの内容（*.csproj の XML テキスト）。</param>
        public CSharpProject(string path, string content) : base(path, content)
        {
            Name = GetAssemblyName(Content);
        }

        private static readonly Regex regProjectReferenceClose = new Regex(@"\</ProjectReference\>[\s]*\</ItemGroup\>", RegexOptions.Compiled);
        private static readonly Regex regAssemblyName = new Regex(@"\<AssemblyName\>(?<name>.*?)\</AssemblyName\>", RegexOptions.Compiled);
        private static readonly Regex regReference = new Regex(@"\<Reference Include=""(?<asm>.*?)"" /\>", RegexOptions.Compiled);
        private static readonly Regex regReferenceHintPath = new Regex(@"\<Reference Include=""(?<asm>.*?)""\>[\s]*\<HintPath\>.*\</HintPath\>[\s]*\</Reference\>", RegexOptions.Compiled);
        private static readonly Regex regProjectReference = new Regex(@"\<ProjectReference Include=""(?<path>.*?)\.csproj""\>", RegexOptions.Compiled);
        private static readonly Regex regImport = new Regex(@"\<Import Project=""(?<path>.*?)\.projitems"" Label=""Shared"".*/\>", RegexOptions.Compiled);
        private static readonly Regex regAnalyzer = new Regex(@"\s*\<Analyzer Include=""(?<path>.*?)"" /\>", RegexOptions.Compiled);
        private static readonly Regex regProjectClose = new Regex(@"\</Project\>", RegexOptions.Compiled);
        private static readonly Regex regLangVersion = new Regex(@"\<LangVersion.*?\</LangVersion\>", RegexOptions.Compiled);
        private static readonly IEnumerable<Regex> regReferenceList = new[]
        {
            regReference,
            regReferenceHintPath,
        };

        /// <summary>
        /// バージョン指定。
        /// </summary>
        /// <param name="version">バージョン。nullだったら langversion = default。</param>
        public void SpecifyLangVersion(int? version)
        {
            var versionString = version == null ? "default" : version.Value.ToString();
            var tags = regLangVersion.Matches(Content).Cast<Match>().Select(x => x.Value).Distinct().ToArray();

            if (tags.Any())
            {
                var versionTag = $"<LangVersion>{versionString}</LangVersion>";
                foreach (var t in tags)
                {
                    Content = Content.Replace(t, versionTag);
                }
            }
            else
            {
                // LangVersion タグを挿入したい場所の直前のタグ
                const string leadingTag = "<AllowUnsafeBlocks>false</AllowUnsafeBlocks>";

                var versionTag = leadingTag + $@"
    <LangVersion>{versionString}</LangVersion>";

                Content = Content.Replace(leadingTag, versionTag);
            }
        }

        /// <summary>
        /// TODO: プロジェクトに参照を追加する。
        /// </summary>
        /// <param name="name"></param>
        //public void AddReference(string name)
        //{
        //    throw new NotImplementedException();
        //}

        /// <summary>
        /// プロジェクトから参照を削除する。
        /// </summary>
        /// <param name="name"></param>
        public void RemoveReference(string name)
        {
            var match = regReferenceList
                .SelectMany(x => x.Matches(Content).Cast<Match>())
                .FirstOrDefault(x => x.Success && x.Groups["asm"].Value == name);
            if (match != null)
            {
                Content = Content.Replace(match.Value, "");
            }
        }

        /// <summary>
        /// プロジェクトに別プロジェクトの参照を追加する。
        /// </summary>
        /// <param name="project">プロジェクト。</param>
        public void AddProjectReference(CSharpProject project)
        {
            var xml = $@"    <ProjectReference Include=""{project.Path}"">
      <Project>{{{project.Guid.ToString().ToUpper()}}}</Project>
      <Name>{project.Name}</Name>
    </ProjectReference>
";

            var match = regProjectReferenceClose.Match(Content);

            if (match.Success)
            {
                var index = match.Index + match.Value.Length - "</ItemGroup>".Length;
                Content = Content.Insert(index, xml);
            }
            else
            {
                var index = Content.LastIndexOf("</Project>"); // ProjectReference 用の ItemGroup が見つからないので末尾に追加する。
                xml = $"  <ItemGroup>\n{xml}  </ItemGroup>\n";
                Content = Content.Insert(index, xml);
            }
        }

        /// <summary>
        /// プロジェクトに別プロジェクトのアナライザー参照を追加する。
        /// </summary>
        /// <param name="project">プロジェクト。</param>
        public void AddAnalyzer(string analyzerPath)
        {
            var xml = $@"    <Analyzer Include=""{analyzerPath}"" />
";

            var match = regAnalyzer.Match(Content);

            if (match.Success)
            {
                var index = match.Index;
                Content = Content.Insert(index, xml);
            }
            else
            {
                var index = Content.LastIndexOf("</Project>"); // ProjectReference 用の ItemGroup が見つからないので末尾に追加する。
                xml = $"  <ItemGroup>\n{xml}  </ItemGroup>\n";
                Content = Content.Insert(index, xml);
            }
        }

        /// <summary>
        /// TODO: プロジェクトから別プロジェクトの参照を削除する。
        /// </summary>
        /// <param name="project">プロジェクト。</param>
        //public void RemoveProjectReference(Project project)
        //{
        //    throw new NotImplementedException();
        //}

        private static string GetAssemblyName(string proj)
        {
            var name = regAssemblyName.Match(proj).Groups["name"].Value;
            return name;
        }

        private IEnumerable<string> GetAssemblyReferences(string proj)
        {
            return regReferenceList.SelectMany(x => x.Matches(proj).Cast<Match>())
                .Where(x => x.Groups["asm"].Success)
                .Select(x => x.Groups["asm"].Value);
        }

        private static IEnumerable<CSharpProject> GetCSharpProjectReferences(string basePath, string proj)
        {
            return regProjectReference.Matches(proj).Cast<Match>()
                .Where(x => x.Groups["path"].Success)
                .Select(x =>
                {
                    var path = GetFullPath(basePath, x.Groups["path"].Value + ".csproj");
                    return GetProject(path, _ => new CSharpProject(path));
                });
        }

        private static IEnumerable<SharedProject> GetSharedProjectReferences(string basePath, string proj)
        {
            return regImport.Matches(proj).Cast<Match>()
                .Where(x => x.Groups["path"].Success)
                .Select(x =>
                {
                    //↓ .projitems と同じフォルダに .shproj がある想定。
                    var path = GetFullPath(basePath, x.Groups["path"].Value + ".shproj");
                    return GetProject(path, _ => new SharedProject(path));
                });
        }

        private static IEnumerable<string> GetAnalyzerReferences(string basePath, string proj)
        {
            return regAnalyzer.Matches(proj).Cast<Match>()
                .Where(x => x.Groups["path"].Success)
                .Select(x => GetFullPath(basePath, x.Groups["path"].Value));
        }

        private static string GetFullPath(string basePath, string relativePath)
            => System.IO.Path.GetFullPath(System.IO.Path.Combine(basePath, relativePath));
    }
}

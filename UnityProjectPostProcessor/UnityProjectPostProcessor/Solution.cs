using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace UnityProjectPostProcessor
{
    /// <summary>
    /// Visual Studio ソリューション。
    /// </summary>
    public class Solution
    {
        /// <summary>
        /// ソリューションのパス。（常に絶対パス）
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// ソリューションファイルの内容。
        /// </summary>
        public string Content { get; private set; }

        /// <summary>
        /// ソリューションに含まれている C# プロジェクトの一覧。
        /// </summary>
        public IEnumerable<CSharpProject> CSharpProjects { get { return GetCSharpProjects(); } }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="path">ソリューションのパス。</param>
        public Solution(string path) : this(path, File.ReadAllText(path)) { }
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="path">ソリューションのパス。</param>
        /// <param name="content">ソリューションの内容（*.sln ファイルのテキスト）。</param>
        public Solution(string path, string content)
        {
            Path = path;
            Content = content;
        }

        private IEnumerable<CSharpProject> GetCSharpProjects()
        {
            var regex = new Regex("Project\\(\"\\{.*\\}\"\\) = \".*\",.*\"(?<path>.*?)\\.csproj\",.*\"\\{.*\\}");
            return regex.Matches(Content).Cast<Match>()
                .Select(x => x.Groups["path"])
                .Where(x => x.Success).Select(x => Project.GetProject<CSharpProject>(x.Value + ".csproj", path => new CSharpProject(path)));
        }

        /// <summary>
        /// ソリューションにプロジェクトを追加する。
        /// 既に追加されている場合は何もしない。
        /// </summary>
        public void AddProject(Project project)
        {
            if (!HasProject(project.Guid))
            {
                var typeGuid = project.TypeGuid;
                var endProject = "EndProject";
                var i = Content.LastIndexOf(endProject) + endProject.Length;
                Content = Content.Insert(i, string.Format(
        @"
Project(""{{{0}}}"") = ""{1}"", ""{2}"", ""{{{3}}}""
EndProject"
                , typeGuid, project.Name, project.Path, project.Guid.ToString().ToUpper()));
            }
        }

        /// <summary>
        /// ソリューションに指定したプロジェクトが追加されているかどうか。
        /// </summary>
        /// <param name="guid">プロジェクトGUID</param>
        /// <returns>ソリューションがプロジェクトを含んでいれば true。</returns>
        private bool HasProject(Guid guid)
        {
            var pattern = string.Format("Project\\(\"\\{{.*\\}}\"\\) = .*,.*,.*\"\\{{{0}\\}}\"", guid.ToString().ToUpper());
            var regex = new Regex(pattern);
            return regex.Match(Content).Success;
        }
    }
}

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
        public string Path { get; }

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
            var regex = new Regex("Project\\(\"\\{.*\\}\"\\) = \".*\",.*\"(?<path>.*?)\\.csproj\",.*\"\\{(?<guid>.*)\\}");
            return
                from Match m in regex.Matches(Content).Cast<Match>()
                where m.Success
                let path = m.Groups["path"].Value + ".csproj"
                select Project.GetProject(path, x => new CSharpProject(x));
        }

        /// <summary>
        /// ソリューションにプロジェクトを追加する。
        /// 既に追加されている場合は何もしない。
        /// </summary>
        public void AddProject(Project project)
        {
            if (!Content.Contains(project.Path))
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
    }
}

using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace UnityProjectPostProcessor
{
    /// <summary>
    /// C# プロジェクトを含むソリューションに Game プロジェクトを自動追加する。
    /// </summary>
    /// <remarks>
    /// Visual Studio Tools for Unity 必須。
    /// </remarks>
    public class ProjectPostprocessor
    {
        /// <summary>
        /// プロジェクトのパス。
        /// </summary>
        private readonly string ProjectRoot = Directory.GetParent(Application.dataPath).FullName;

        /// <summary>
        /// Game.csproj
        /// </summary>
        private CSharpProject GameProject { get; }

        public void Generate()
        {
            // Visual Studio Tools for Unity 入ってない環境でもエラー出さないようにするため動的に

            var path = Path.Combine(Application.dataPath, @"UnityVS\Editor\SyntaxTree.VisualStudio.Unity.Bridge.dll");

            if (!File.Exists(path))
            {
                Debug.Log($"ProjectPostprocessor: path '{path}' does not exist");
                return;
            }

            var asm = Assembly.LoadFile(path);
            var projectFilesGeneratorType = asm.GetType("SyntaxTree.VisualStudio.Unity.Bridge.ProjectFilesGenerator");

            if (projectFilesGeneratorType != null)
            {
                var typeOfThis = typeof(ProjectPostprocessor);
                var fileGenerationHandlerType = asm.GetType("SyntaxTree.VisualStudio.Unity.Bridge.FileGenerationHandler");

                var solutionFileGenerationField = projectFilesGeneratorType.GetField("SolutionFileGeneration", BindingFlags.Static | BindingFlags.Public);
                var onGeneratedSolutionFile = typeOfThis.GetMethod("OnGeneratedSolutionFile", BindingFlags.Instance | BindingFlags.NonPublic);
                solutionFileGenerationField.SetValue(null, Delegate.CreateDelegate(fileGenerationHandlerType, this, onGeneratedSolutionFile));

                var projectFileGenerationField = projectFilesGeneratorType.GetField("ProjectFileGeneration", BindingFlags.Static | BindingFlags.Public);
                var onGeneratedProjectFile = typeOfThis.GetMethod("OnGeneratedProjectFile", BindingFlags.Instance | BindingFlags.NonPublic);
                projectFileGenerationField.SetValue(null, Delegate.CreateDelegate(fileGenerationHandlerType, this, onGeneratedProjectFile));

                // フックしていると Unity のプロジェクト自動更新が効かなくなる。
                // ロード時に一度だけ明示的にプロジェクト生成することにした。
                var generateProjectMethod = projectFilesGeneratorType.GetMethod("GenerateProject", BindingFlags.Static | BindingFlags.Public);
                generateProjectMethod.Invoke(null, new object[0]);

                solutionFileGenerationField.SetValue(null, null);
                projectFileGenerationField.SetValue(null, null);
            }
            else
            {
                Debug.Log("ProjectPostprocessor: type 'SyntaxTree.VisualStudio.Unity.Bridge.ProjectFilesGenerator' does not exist");

            }
        }

        private string OnGeneratedSolutionFile(string fileName, string fileContent)
        {
            var solution = new Solution(Path.Combine(ProjectRoot, fileName), fileContent);
            AddGameProject(solution);
            return solution.Content;
        }

        private string OnGeneratedProjectFile(string fileName, string fileContent)
        {
            // Editor, Plugins プロジェクトは無視する。
            if (fileName.Contains(".Editor.")) { return fileContent; }

            var csproj = new CSharpProject(Path.Combine(ProjectRoot, fileName), fileContent);
            UpdateReferences(csproj);
            SpecifyLangVersion(csproj);

            return csproj.Content;
        }

        private static void SpecifyLangVersion(CSharpProject csproj)
        {
            var unityVersion = Application.unityVersion;
            var csharpVersion = unityVersion.CompareTo("5.5") >= 0 ? default(int?) : 4;
            csproj.SpecifyLangVersion(csharpVersion);
        }

        private void UpdateReferences(CSharpProject csproj)
        {
            var log = new System.Text.StringBuilder();

            // Boo.Lang は無条件で消す。
            csproj.RemoveReference("Boo.Lang");

            csproj.RemoveReference(GameProject.Name);
            csproj.AddProjectReference(GameProject);

            foreach (var refProj in GameProject.GetAllCSharpProjectReferences())
            {
                log.AppendLine($"updare ref {refProj.Name}");
                // Game プロジェクト関連のアセンブリ参照削除。
                csproj.RemoveReference(refProj.Name);
                // Game プロジェクト関連の参照を追加。
                csproj.AddProjectReference(refProj);
            }

            foreach (var analyser in GameProject.AnalyzerReferences)
            {
                log.AppendLine($"add analyzer {analyser}");
                // Game プロジェクト関連のアナライザーを追加。
                csproj.AddAnalyzer(analyser);
            }

            Debug.Log(@"ProjectPostprocessor: UpdateReferences
" + log.ToString());
        }

        /// <summary>
        /// ソリューションに Game プロジェクトを追加する。
        /// </summary>
        /// <remarks>
        /// GameProject が参照している別 C# プロジェクトも合わせて追加する。
        /// </remarks>
        /// <param name="solution"></param>
        /// <returns></returns>
        private void AddGameProject(Solution solution)
        {
            var log = new System.Text.StringBuilder();

            solution.AddProject(GameProject);
            foreach (var p in GameProject.GetAllCSharpProjectReferences())
            {
                log.AppendLine($"add project {p.Name}");
                solution.AddProject(p);
            }

            foreach (var p in GameProject.GetAllSharedProjectReferences())
            {
                log.AppendLine($"add project {p.Name}");
                solution.AddProject(p);
            }

            Debug.Log(@"ProjectPostprocessor: AddProjects
" + log.ToString());
        }

        /// <summary>Record Constructor</summary>
        /// <param name="gameProject"><see cref="GameProject"/></param>
        public ProjectPostprocessor(CSharpProject gameProject = default(CSharpProject))
        {
            GameProject = gameProject;
        }
    }

}

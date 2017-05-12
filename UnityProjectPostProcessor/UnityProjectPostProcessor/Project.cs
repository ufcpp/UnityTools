using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace UnityProjectPostProcessor
{
    public abstract class Project : IEquatable<Project>
    {
        /// <summary>
        /// プロジェクトのパス。（常に絶対パス）
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// プロジェクトの種類。
        /// </summary>
        public abstract Guid TypeGuid { get; }

        /// <summary>
        /// プロジェクトのGUID。
        /// </summary>
        public Guid Guid { get; }

        /// <summary>
        /// プロジェクト名。
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// プロジェクトファイルの内容。
        /// </summary>
        public string Content { get; protected set; }

        public Project(string path) : this(path, File.ReadAllText(path)) { }
        public Project(string path, string content)
        {
            Path = System.IO.Path.GetFullPath(path);
            Content = content;

            try
            {
                // GUID が入ってないことがあるので、その場合は例外を無視。
                Guid = GetProjectGuid(Content);
            }
            catch
            {
                Guid = Guid.NewGuid();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="proj"></param>
        /// <returns></returns>
        private static Guid GetProjectGuid(string proj)
        {
            var regex = new Regex("\\<ProjectGuid\\>\\{?(?<guid>.*?)\\}?\\</ProjectGuid\\>");
            var guid = regex.Match(proj).Groups["guid"].Value;
            return new Guid(guid);
        }

        /// <summary>
        /// パスに対応するプロジェクトファイルの内容を取得する。
        /// </summary>
        /// <remarks>
        /// 一度読み込まれたパスの内容はキャッシュされ、以降はキャッシュを返す。
        /// </remarks>
        /// <param name="path">プロジェクトのパス。</param>
        /// <param name="creator">キャッシュされていなかったときにインスタンスを生成する。</param>
        /// <returns>プロジェクト。</returns>
        internal static T GetProject<T>(string path, Func<string, T> creator) where T : Project
        {
            if (!_projectChace.TryGetValue(path, out var project))
            {
                project = creator(path);
                _projectChace.Add(path, project);
            }

            return (T)project;
        }

        public bool Equals(Project other) => Guid == other.Guid;

        private static Dictionary<string, Project> _projectChace = new Dictionary<string, Project>();
    }
}

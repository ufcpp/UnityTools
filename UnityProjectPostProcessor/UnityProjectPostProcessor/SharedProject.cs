using System;
using System.IO;

namespace UnityProjectPostProcessor
{
    /// <summary>
    /// Shared プロジェクト。
    /// </summary>
    public class SharedProject : Project, IEquatable<SharedProject>
    {
        /// <summary>
        /// <see cref="Project.TypeGuid"/>
        /// </summary>
        public override Guid TypeGuid => new Guid("D954291E-2A0B-460D-934E-DC6B0785DB48");

        public SharedProject(string path) : this(path, File.ReadAllText(path)) { }
        public SharedProject(string path, string content) : base(path, content)
        {
            Name = System.IO.Path.GetFileNameWithoutExtension(path);
        }

        public bool Equals(SharedProject other) => Guid == other.Guid;
    }
}

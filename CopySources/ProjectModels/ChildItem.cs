namespace ProjectModels
{
    /// <summary>
    /// Child item
    /// e.g.
    /// | parent   | child    |
    /// | *.sln    | *.csproj |
    /// | *.csproj | *.cs     |
    /// </summary>
    public class ChildItem : Item
    {
        /// <summary>
        /// relative path from parent to this item.
        /// </summary>
        public string RelativePath { get; }

        public ChildItem(string basePath, string relativePath)
            : base(System.IO.Path.Combine(basePath, relativePath))
        {
            RelativePath = relativePath.Replace("\\", "/");
        }

        public ChildItem(Item parent, string relativePath)
            : this(parent.Folder, relativePath)
        {
        }
    }
}

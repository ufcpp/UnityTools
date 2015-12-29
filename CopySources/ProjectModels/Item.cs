namespace ProjectModels
{
    public class Item
    {
        /// <summary>
        /// full path to this item.
        /// </summary>
        public string Path { get; }

        public Item(string path)
        {
            Path = path;
        }

        /// <summary>
        /// A folder which contains this item.
        /// </summary>
        public string Folder => System.IO.Path.GetDirectoryName(Path);

        public string Name => System.IO.Path.GetFileName(Path);

        public string NameWithoutExtension => System.IO.Path.GetFileNameWithoutExtension(Path);
    }
}

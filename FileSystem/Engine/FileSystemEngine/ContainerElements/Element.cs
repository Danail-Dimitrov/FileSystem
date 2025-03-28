namespace FileSystem.Engine.FileSystemEngine.ContainerElements
{
    // I could use inheritance but decided this way it will be easier to store the elements in the container.
    public class Element
    {
        public string Name { get; set; }
        public DateTime CreationTime { get; set; }
        public uint FirstBlockId { get; set; }
        // The block of the parent directory
        public uint ParentBlockId { get; set; }
        // Only for files
        public uint Size { get; set; }
        public bool IsFolder { get; set; }
        // Only for folders
        public uint ChildrenCount { get; set; }
    }
}

namespace FileSystem.Engine.FileSystemEngine.ContainerElements
{
    public abstract class Element
    {
        public string Name { get; set; }
        public DateTime CreationTime { get; set; }
        public uint FirstBlockId { get; set; }
        public uint ParentBlockId { get; set; }
    }
}

using System.Text;

namespace FileSystem.Engine.FileSystemEngine.ContainerElements
{
    // I could use inheritance but decided this way it will be easier to store the elements in the container.
    public class Element
    {
        public int NameLenght => Encoding.UTF8.GetByteCount(Name);
        public string Name { get; set; }
        public DateTime CreationTime { get; set; }
        public uint FirstBlockId { get; set; }
        // The block of the parent directory
        public uint ParentBlockId { get; set; }
        public bool IsFolder { get; set; }
        // Only for folders
        public uint ChildrenCount { get; set; }

        public int SizeInBytes => NameLenght + sizeof(int) + sizeof(uint) + sizeof(uint) + sizeof(bool) + sizeof(uint) + sizeof(long); // Date is written in long
    }
}

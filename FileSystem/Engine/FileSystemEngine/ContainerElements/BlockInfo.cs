namespace FileSystem.Engine.FileSystemEngine.ContainerElements
{
    /// <summary>
    /// This class is responsible for storing information about a block.
    /// </summary>
    public struct BlockInfo
    {
        public const int ChecksumLength = 16;
        public byte[] _checksum;

        public uint BlockId;
        public bool IsUsed;
        public uint NextBlock; 

        public byte[] Checksum 
        {
            get => _checksum;
            set
            {
                if (value.Length != ChecksumLength)
                {
                    throw new System.ArgumentException("Checksum must be 16 bytes long.");
                }
                _checksum = value;
            }
        }

        public static uint GetSize()
        {
            return (uint)(sizeof(uint) + sizeof(bool) + sizeof(uint) + ChecksumLength);
        }
    }
}

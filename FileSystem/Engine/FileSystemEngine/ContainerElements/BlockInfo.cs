namespace FileSystem.Engine.FileSystemEngine.ContainerElements
{
    /// <summary>
    /// This class is responsible for storing information about a block.
    /// </summary>
    public struct BlockInfo
    {
        public const int ChecksumLength = 16;
        private byte[] _checksum;

        public uint BlockId;
        public bool IsUsed;
        public uint NextBlock;
        public uint ContentLenght;

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
            return sizeof(uint) + sizeof(uint) + sizeof(bool) + sizeof(uint) + ChecksumLength;
        }

        public static uint GetNextBlock()
        {
            return sizeof(uint) + sizeof(bool) + sizeof(uint);
        }

        public static uint GetContentLenghtPosition()
        {
            return sizeof(uint) + sizeof(bool) + sizeof(uint);
        }

        public static uint GetSizeWithoutChecksum()
        {
            return sizeof(uint) + sizeof(uint) + sizeof(bool) + sizeof(uint);
        }

        public static uint GetStatusPosition()
        {
            return sizeof(uint);
        }
    }
}

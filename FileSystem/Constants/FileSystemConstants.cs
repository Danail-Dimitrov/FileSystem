namespace FileSystem.Constants
{
    /// <summary>
    /// A class for constants related to the file system.
    /// </summary>
    public static class FileSystemConstants
    {
        /// <summary>
        /// The path to the container file.
        /// It could be set up to be read from a configuration file.
        /// But for this project a constant is enough.
        /// </summary>
        public const string ContainerPath = "fs.bat";

        /// <summary>
        /// The size of the blocks in the file system.
        /// </summary>
        public const int BlockSize = 1024;

        /// <summary>
        /// The header is the part of the fs that stores system data.
        /// </summary>
        public const int HeaderSize = 64;

        /// <summary>
        /// The size of a block association table entry.
        /// It comes from:
        /// ID 4 bytes (unit)
        /// IsUsed flag: 1 byte (bool)
        /// NextBlock pointer: 4 bytes (uint)
        /// Checksum: 16 bytes
        /// Calculated with the help of the internet and gpts.
        /// </summary>
        public const int BATEntrySize = 25;

        /// <summary>
        /// The magic number is used to identify the file system.
        /// Used to validate the container while opening it.
        /// Idea taken from internet.
        /// </summary>
        public const uint MagicNumber = 0x46533230; // "FS20" in ASCII

        /// <summary>
        /// The version of the file system format.
        /// </summary>
        public const ushort Version = 0x0001; // Version 1
    }
}

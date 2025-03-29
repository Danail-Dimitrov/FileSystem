using FileSystem.Constants;
using FileSystem.DataStructures.MyList;
using FileSystem.Engine.FileSystemEngine.ContainerElements;
using FileSystem.Utilities;
using System.Text;

namespace FileSystem.Engine.FileSystemEngine
{
    // While implementing this class I used claude.ai for research and ideas.
    /// <summary>
    /// This class is responsible for the file system operations.
    /// </summary>
    public class FileSystemEngine : IFileSystemEngine
    {
        /// <summary>
        /// The stream of the container file.
        /// </summary>
        private FileStream _containerStream;
        private Element _rootDirectory;
        private Element _currentDir;
        private uint _nextAvailableBlockId;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public FileSystemEngine()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
            if (File.Exists(FileSystemConstants.ContainerPath))
                // Open the file system.
                OpenContainer();
            else
                // Create the file system.
                CreateContainer();
        }

        /// <summary>
        /// This method creates a new container.
        /// </summary>
        private void CreateContainer()
        {
            _containerStream = new FileStream(FileSystemConstants.ContainerPath, FileMode.Create, FileAccess.ReadWrite);
            _nextAvailableBlockId = 1;

            WriteHeader();

            InitRoot();

            _containerStream.Flush();
        }

        /// <summary>
        /// This method opens an existing container.
        /// </summary>
        private void OpenContainer()
        {
            _containerStream = new FileStream(FileSystemConstants.ContainerPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            if (!ValidateHeader())
                throw new InvalidDataException("Invalid container file format");

            LoadHeaderInfo();

            _rootDirectory = ReadDirectoryEntry(1);
            _currentDir = _rootDirectory;
        }

        private Element ReadDirectoryEntry(uint blockId)
        {
            byte[] data = ReadBlockData(blockId);

            Element element = new Element();

            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                // Read entry name
                int nameLength = reader.ReadInt32();
                byte[] nameBytes = reader.ReadBytes(nameLength);
                element.Name = Encoding.UTF8.GetString(nameBytes);

                // Read entry metadata
                element.CreationTime = DateTime.FromBinary(reader.ReadInt64());
                element.FirstBlockId = reader.ReadUInt32();
                element.ParentBlockId = reader.ReadUInt32();
                element.Size = reader.ReadUInt32();
                element.IsFolder = reader.ReadBoolean();
                element.ChildrenCount = reader.ReadUInt32();
            }

            return element;
        }

        private byte[] ReadBlockData(uint blockId)
        {
            // Verify block checksum
            if (!VerifyBlockChecksum(blockId))
                throw new InvalidDataException($"Block {blockId} failed checksum verification");

            long offset = CalculateBlockOffset(blockId);

            // Seek past the header
            _containerStream.Seek(offset + BlockInfo.GetSize(), SeekOrigin.Begin);

            byte[] data = new byte[FileSystemConstants.BlockSize - BlockInfo.GetSize()];
            _containerStream.Read(data, 0, data.Length);

            return data;
        }

        private bool VerifyBlockChecksum(uint blockId)
        {
            long offset = CalculateBlockOffset(blockId);

            // Read block data (header + data)
            _containerStream.Seek(offset, SeekOrigin.Begin);
            byte[] blockData = new byte[FileSystemConstants.BlockSize - BlockInfo.ChecksumLength];
            _containerStream.Read(blockData, 0, blockData.Length);

            // Read stored checksum
            byte[] storedChecksum = new byte[BlockInfo.ChecksumLength];
            _containerStream.Read(storedChecksum, 0, BlockInfo.ChecksumLength);

            // Calculate checksum
            byte[] calculatedChecksum = CalculateChecksum(blockData);

            // Compare checksums
            for (int i = 0; i < BlockInfo.ChecksumLength; i++)
                if (storedChecksum[i] != calculatedChecksum[i])
                    return false;

            return true;
        }

        private void LoadHeaderInfo()
        {
            // Reset the stream position to the beginning
            _containerStream.Seek(0, SeekOrigin.Begin);

            using (BinaryReader reader = new BinaryReader(_containerStream, Encoding.UTF8, true))
            {
                // Skip magic number and version which were already validated in ValidateHeader
                reader.ReadUInt32(); // Magic number
                reader.ReadUInt16(); // Version

                // Read block size and header size (we already know these constants, but read them to advance the stream position)
                uint blockSize = reader.ReadUInt32();
                uint headerSize = reader.ReadUInt32();

                // Read the next available block ID
                _nextAvailableBlockId = reader.ReadUInt32();
            }

            // Verify that all the read information is valid
            if (_nextAvailableBlockId < 1)
                throw new InvalidDataException("Invalid next available block ID in header");
            
        }

        private bool ValidateHeader()
        {
            _containerStream.Seek(0, SeekOrigin.Begin);

            if (_containerStream.Length < FileSystemConstants.HeaderSize)
                return false;

            using (BinaryReader reader = new BinaryReader(_containerStream, Encoding.UTF8, true))
            {
                uint magicNumber = reader.ReadUInt32();
                if(magicNumber != FileSystemConstants.MagicNumber)
                    return false;

                ushort version = reader.ReadUInt16();
                if (version != FileSystemConstants.Version)
                    return false;

                // Read header data for checksum verification
                _containerStream.Seek(0, SeekOrigin.Begin);
                byte[] headerData = new byte[FileSystemConstants.HeaderSize - 16];
                _containerStream.Read(headerData, 0, headerData.Length);

                // Read stored checksum
                byte[] storedChecksum = new byte[16];
                _containerStream.Read(storedChecksum, 0, 16);

                byte[] calculatedChecksum = CalculateChecksum(headerData);

                for (int i = 0; i < 16; i++)
                    if (storedChecksum[i] != calculatedChecksum[i])
                        return false;
            }

            return true;
        }

        /// <summary>
        /// Calculates the begining of the block in the container.
        /// </summary>
        /// <param name="batStartBlockId">Id of the block</param>
        /// <returns>The offset</returns>
        private long CalculateBlockOffset(uint batStartBlockId)
        {
            return FileSystemConstants.HeaderSize + (batStartBlockId - 1) * FileSystemConstants.BlockSize; //Ids start at one
        }

        /// <summary>
        /// Writes the header of the container.
        /// </summary>
        private void WriteHeader()
        {
            _containerStream.Seek(0, SeekOrigin.Begin);

            using (var writer = new BinaryWriter(_containerStream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(FileSystemConstants.MagicNumber);
                writer.Write(FileSystemConstants.Version);
                writer.Write(FileSystemConstants.BlockSize);
                writer.Write(FileSystemConstants.HeaderSize);
                writer.Write(_nextAvailableBlockId);
            }

            WriteHeaderChecksum();

            FillHeaderWithZeros();
        }

        private void InitRoot()
        {
            _rootDirectory = new Element
            {
                Name = "",
                CreationTime = DateTime.Now,
                FirstBlockId = AllocateBlock(1),
                ParentBlockId = 0,
                Size = 0,
                IsFolder = true
            };

            _currentDir = _rootDirectory;

            long blockOffset = CalculateBlockOffset(_rootDirectory.FirstBlockId);
            _containerStream.Seek(blockOffset, SeekOrigin.Begin);

            CreateFolder(_rootDirectory);
        }

        private void CreateFolder(Element folder)
        {
            long blockOffset = CalculateBlockOffset(folder.FirstBlockId);
            _containerStream.Seek(blockOffset + BlockInfo.GetSize(), SeekOrigin.Begin);
            long dataStartPosition = _containerStream.Position;

            byte[] nameBytes = Encoding.UTF8.GetBytes(folder.Name);

            using (var writer = new BinaryWriter(_containerStream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(nameBytes.Length);
                if (nameBytes.Length > 0)
                    writer.Write(nameBytes);
                writer.Write(folder.CreationTime.ToBinary());
                writer.Write(folder.FirstBlockId);
                writer.Write(folder.ParentBlockId);
                writer.Write(folder.Size);
                writer.Write(folder.IsFolder);
                writer.Write(folder.ChildrenCount);
            }

            uint bitesWritten = (uint)(_containerStream.Position - dataStartPosition);
            UpdateBlockContentLenght(folder.FirstBlockId, bitesWritten);

            WriteBlockChecksum(folder.FirstBlockId);
        }

        private void UpdateBlockContentLenght(uint firstBlockId, uint bitesWritten)
        {
            long blockOffset = CalculateBlockOffset(firstBlockId);

            _containerStream.Seek(blockOffset + 8, SeekOrigin.Begin);

            using (var writer = new BinaryWriter(_containerStream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(bitesWritten);
            }

            // Not updating checksum because this will only be called in methods that already will update it
        }

        private uint AllocateBlock(uint count)
        {
            uint firstId = 0;
            uint prevId = 0;
            for (uint i = 0; i < count; i++)
            {
                // Find the first free block
                uint blockId = FindFreeBlock();

                GetBlockInContainer(prevId, blockId);

                if (firstId == 0)
                    firstId = blockId;

                prevId = blockId;
            }

            return firstId;
        }

        private void GetBlockInContainer(uint parentId, uint blockId)
        {
            long blockOffset = CalculateBlockOffset(blockId);
            _containerStream.Seek(blockOffset, SeekOrigin.Begin);

            using (var writer = new BinaryWriter(_containerStream, Encoding.UTF8, leaveOpen: true))
            {
                // Write block ID
                writer.Write(blockId);

                // Write block status (1 = used)
                writer.Write((byte)1);

                // Write next block ID (0 for now - will be updated if this block gets a child)
                writer.Write((uint)0);

                // Write data length (0 for now - will be updated when content is added)
                writer.Write((uint)0);
            }

            int remainingSize = FileSystemConstants.BlockSize - (int)BlockInfo.GetSize();
            byte[] padding = new byte[remainingSize];
            _containerStream.Write(padding, 0, padding.Length);

            if (parentId != 0)
                UpdateNextBlockPointer(parentId, blockId);

            WriteBlockChecksum(blockId);

            UpdateHeaderNextBlock();
        }

        private void UpdateHeaderNextBlock()
        {
            _containerStream.Seek(14, SeekOrigin.Begin);

            using (var writer = new BinaryWriter(_containerStream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(_nextAvailableBlockId);
            }

            WriteHeaderChecksum();
        }

        private void WriteBlockChecksum(uint blockId)
        {
            long blockOffset = CalculateBlockOffset(blockId);

            // Read block data (excluding checksum)
            _containerStream.Seek(blockOffset, SeekOrigin.Begin);

            // Use BinaryReader to read the block data
            byte[] blockData = new byte[FileSystemConstants.BlockSize - 16]; // 16 bytes for checksum
            using (var reader = new BinaryReader(_containerStream, Encoding.UTF8, leaveOpen: true))
            {
                blockData = reader.ReadBytes(FileSystemConstants.BlockSize - 16);
            }

            // Calculate checksum
            byte[] checksum = CalculateChecksum(blockData);

            // Write checksum at the end of the block
            _containerStream.Seek(blockOffset + FileSystemConstants.BlockSize - 16, SeekOrigin.Begin);

            // Use BinaryWriter to write the checksum
            using (var writer = new BinaryWriter(_containerStream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(checksum);
            }
        }

        private void UpdateNextBlockPointer(uint blockId, uint nextBlockId)
        {
            long blockOffset = CalculateBlockOffset(blockId);

            // Next block ID is stored after block ID (4 bytes) and status (1 byte)
            long nextBlockOffset = blockOffset + 4 + 1;

            _containerStream.Seek(nextBlockOffset, SeekOrigin.Begin);

            // Write the next block ID
            using (var writer = new BinaryWriter(_containerStream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(nextBlockId);
            }

            WriteBlockChecksum(blockId);
        }

        private uint FindFreeBlock()
        {
            for (uint blockId = 1; blockId < _nextAvailableBlockId; blockId++)
            {
                if (IsBlockFree(blockId))
                    return blockId;
            }

            return _nextAvailableBlockId++;
        }

        private bool IsBlockFree(uint blockId)
        {
            long batOffset = FileSystemConstants.HeaderSize + (blockId - 1);

            _containerStream.Seek(batOffset, SeekOrigin.Begin);
            int status = _containerStream.ReadByte();

            return status == 0;
        }

        /// <summary>
        /// Fills the remaining part of the header with zeros.
        /// </summary>
        private void FillHeaderWithZeros()
        {
            uint takenSpace = (uint)_containerStream.Position;
            uint remainingSpace = FileSystemConstants.HeaderSize - takenSpace;

            byte[] zeros = new byte[remainingSpace];
            _containerStream.Write(zeros, 0, zeros.Length);
        }

        /// <summary>
        /// Calculates the checksum of the header and writes it to the file.
        /// </summary>
        private void WriteHeaderChecksum()
        {
            byte[] headerData = new byte[FileSystemConstants.HeaderSize - 16];
            _containerStream.Seek(0, SeekOrigin.Begin);
            _containerStream.Read(headerData, 0, headerData.Length);

            byte[] checksum = CalculateChecksum(headerData);

            _containerStream.Seek(FileSystemConstants.HeaderSize - 16, SeekOrigin.Begin);
            _containerStream.Write(checksum, 0, checksum.Length);
        }

        // Normally I would use Sha256 for checksums but since it is a hashing function, which is not allowed,
        // I used the internet and claude.ai to find this implementation.
        /// <summary>
        /// Calculates a simple checksum for the given data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The checksum.</returns>
        private byte[] CalculateChecksum(byte[] data)
        {
            // Create a simple 16-byte (128-bit) checksum
            byte[] checksum = new byte[16];

            if (data == null || data.Length == 0)
                return checksum;

            // Initialize with some prime numbers
            uint[] accumulators = [2147483647, 16777619, 65521, 32771];

            // Process each byte of the data
            for (int i = 0; i < data.Length; i++)
            {
                accumulators[0] = ((accumulators[0] << 5) + accumulators[0]) ^ data[i];
                accumulators[1] = accumulators[1] + (data[i] * (uint)(i % 1000 + 1));
                accumulators[2] = ((accumulators[2] + data[i]) * 17) & 0xFFFFFFFF;
                accumulators[3] = accumulators[3] ^ ((uint)(data[i] << (i % 8 * 4)) | ((uint)data[i] >> (8 - (i % 8 * 4))));
            }

            // Convert the accumulators to bytes
            for (int i = 0; i < 4; i++)
            {
                BitConverter.GetBytes(accumulators[i]).CopyTo(checksum, i * 4);
            }

            return checksum;
        }

        private bool FileExistsInCurrentDirectory(string fileName)
        {
            MyList<Element> entries = GetDirectoryEntries(_currentDir);

            foreach (var entry in entries)
            {
                if (StringHandler.Compare(entry.Name, fileName) == 0)
                    return true;
            }

            return false;
        }

        private MyList<Element> GetDirectoryEntries(Element directory)
        {
            if (!directory.IsFolder)
                throw new ArgumentException("Not a directory", nameof(directory));

            MyList<Element> entries = new MyList<Element>();

            uint childCount = directory.ChildrenCount;

            if (childCount == 0)
                return entries;

            long directoryOffset = CalculateBlockOffset(directory.FirstBlockId);

            _containerStream.Seek(directoryOffset + BlockInfo.GetSize(), SeekOrigin.Begin);

            using (BinaryReader reader = new BinaryReader(_containerStream, Encoding.UTF8, true))
            {
                // Skip the name length and name
                int nameLength = reader.ReadInt32();
                reader.ReadBytes(nameLength); // Skip name

                // Skip other directory metadata
                reader.ReadInt64(); // Creation time
                reader.ReadUInt32(); // First block ID
                reader.ReadUInt32(); // Parent block ID
                reader.ReadUInt32(); // Size
                reader.ReadBoolean(); // Is folder
                reader.ReadUInt32();

                // Now read children block IDs
                for (int i = 0; i < childCount; i++)
                {
                    uint childBlockId = reader.ReadUInt32();
                    entries.Add(ReadDirectoryEntry(childBlockId));
                }
            }

            return entries;
        }

        private uint GetNextBlockId(uint currentBlockId)
        {
            long blockOffset = CalculateBlockOffset(currentBlockId);

            _containerStream.Seek(blockOffset + 5, SeekOrigin.Begin);

            using (var reader = new BinaryReader(_containerStream, Encoding.UTF8, leaveOpen: true))
            {
                return reader.ReadUInt32();
            }
        }

        private void WriteFileDataToBlock(uint blockId, byte[] data, int length)
        {
            long blockOffset = CalculateBlockOffset(blockId);

            _containerStream.Seek(blockOffset + BlockInfo.GetSize(), SeekOrigin.Begin);

            // Write the data
            _containerStream.Write(data, 0, length);

            // Update the block's content length in the header
            UpdateBlockContentLenght(blockId, (uint)length);

            // Update the block's checksum
            WriteBlockChecksum(blockId);
        }

        /// <summary>
        /// This is the implementation of the cpin command.
        /// It takes a file from the real file system and copies it into the container.
        /// </summary>
        /// <param name="source">The real file on the pc.</param>
        /// <param name="destination">The destination in the container</param>
        public void CopyIn(string source, string fileName)
        {
            if (!File.Exists(source))
                throw new FileNotFoundException("Source file not found", source);

            FileInfo fileInfo = new FileInfo(source);
            long fileSize = fileInfo.Length;

            if (FileExistsInCurrentDirectory(fileName))
                throw new IOException($"A file with name '{fileName}' already exists in the current directory");

            uint usableBlockSize = FileSystemConstants.BlockSize - BlockInfo.GetSize() - BlockInfo.ChecksumLength;
            uint blocksNeeded = (uint)Math.Ceiling((double)fileSize / usableBlockSize);

            uint firstBlockId = AllocateBlock(blocksNeeded);
            if (firstBlockId == 0)
                throw new IOException("Failed to allocate blocks for the file");

            Element fileEntry = new Element
            {
                Name = fileName,
                CreationTime = DateTime.Now,
                FirstBlockId = firstBlockId,
                ParentBlockId = _currentDir.FirstBlockId,
                Size = (uint)fileSize,
                IsFolder = false
            };

            using (FileStream sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read))
            {
                uint currentBlockId = firstBlockId;
                byte[] buffer = new byte[usableBlockSize];
                int bytesRead;
                long totalBytesWritten = 0;

                while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    // Write data to the current block
                    WriteFileDataToBlock(currentBlockId, buffer, bytesRead);
                    totalBytesWritten += bytesRead;

                    // Move to the next block if necessary
                    if (totalBytesWritten < fileSize)
                    {
                        // Get the next block ID in the chain
                        currentBlockId = GetNextBlockId(currentBlockId);
                        if (currentBlockId == 0)
                            throw new IOException("Unexpected end of block chain");
                    }
                }
            }
        }
    }
}

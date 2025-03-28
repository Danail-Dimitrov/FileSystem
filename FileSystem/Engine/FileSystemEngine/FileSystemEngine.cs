using FileSystem.Constants;
using FileSystem.Engine.FileSystemEngine.ContainerElements;
using System.Net.Sockets;
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

        public FileSystemEngine()
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
            throw new NotImplementedException();
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

            Console.WriteLine("Root Block Header Information (Before CreateFolder):");
            Console.WriteLine($"Block ID: {_rootDirectory.FirstBlockId}");
            Console.WriteLine($"Block Offset: {blockOffset} bytes");

            using (var reader = new BinaryReader(_containerStream, Encoding.UTF8, leaveOpen: true))
            {
                uint blockId = reader.ReadUInt32();
                byte status = reader.ReadByte();
                uint nextBlock = reader.ReadUInt32();
                uint dataLength = reader.ReadUInt32();

                Console.WriteLine($"Block ID from header: {blockId}");
                Console.WriteLine($"Status: {status} ({(status == 1 ? "Used" : "Free")})");
                Console.WriteLine($"Next Block: {nextBlock}");
                Console.WriteLine($"Data Length: {dataLength} bytes");

                // Read and display filler zeros (or whatever data is in the block)
                uint fillerSize = FileSystemConstants.BlockSize - BlockInfo.GetSize();
                byte[] fillerData = reader.ReadBytes((int)fillerSize);

                Console.WriteLine("Filler Data (first 20 bytes):");
                for (int i = 0; i < Math.Min(20, fillerData.Length); i++)
                {
                    Console.Write($"{fillerData[i]:X2} ");
                }
                Console.WriteLine();

                // Count zeros in the filler data
                int zeroCount = fillerData.Count(b => b == 0);
                Console.WriteLine($"Zero bytes in filler: {zeroCount} of {fillerData.Length}");
            }

            Console.WriteLine();
            Console.ReadLine();

            CreateFolder(_rootDirectory);
        }

        private void CreateFolder(Element folder)
        {
            long blockOffset = CalculateBlockOffset(folder.FirstBlockId);
            _containerStream.Seek(blockOffset + BlockInfo.GetSize(), SeekOrigin.Begin);

            using (var writer = new BinaryWriter(_containerStream, Encoding.UTF8, leaveOpen: true))
            {

                writer.Write(folder.Name);
                writer.Write(folder.CreationTime.ToBinary());
                writer.Write(folder.FirstBlockId);
                writer.Write(folder.ParentBlockId);
                writer.Write(folder.Size);
                writer.Write(folder.IsFolder);
                writer.Write(folder.ChildrenCount);
            }

            uint bitesWritten = (uint)(_containerStream.Position - blockOffset);
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

        /// <summary>
        /// This is the implementation of the cpin command.
        /// It takes a file from the real file system and copies it into the container.
        /// </summary>
        /// <param name="source">The real file on the pc.</param>
        /// <param name="destination">The destination in the container</param>
        public void CopyIn(string source, string destination)
        {
            throw new System.NotImplementedException();
        }
    }
}

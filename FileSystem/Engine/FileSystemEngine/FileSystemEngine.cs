using FileSystem.Compression;
using FileSystem.Constants;
using FileSystem.DataStructures.List;
using FileSystem.Engine.FileSystemEngine.ContainerElements;
using FileSystem.IO;
using FileSystem.Utilities;
using System.Text;

namespace FileSystem.Engine.FileSystemEngine
{
    // While implementing this class I used claude.ai for research, ideas and help with the implementation.
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
        /// This is the implementation of the cpin command.
        /// It takes a file from the real file system and copies it into the container.
        /// </summary>
        /// <param name="source">The real file on the pc.</param>
        /// <param name="destination">The destination in the container</param>
        public void CopyIn(string source, string destination)
        {
            Element originalCurrent = _currentDir;

            if (!File.Exists(source))
                throw new FileNotFoundException("Source file not found", source);

            var destInfo = StringHandler.SplitByLastOccurrence(destination, '/');
            CD(destInfo.Item1);
            string fileName = destInfo.Item2;

            if (FileExistsInCurrentDirectory(fileName))
                throw new IOException($"A file with name '{fileName}' already exists in the current directory");

            byte[] fileData;
            using (FileStream sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read))
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    sourceStream.CopyTo(memoryStream);
                    fileData = memoryStream.ToArray();
                }
            }

            // Compress the file data
            byte[] compressedData = HuffmanCompression.Compress(fileData);

            uint usableBlockSize = FileSystemConstants.BlockSize - BlockInfo.GetSize();
            long fileSize = compressedData.Length;
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
                IsFolder = false,
                ChildrenCount = 0
            };

            try
            {
                CreateElement(fileEntry);

                uint currentBlockId = firstBlockId;
                int totalBytesWritten = 0;
                int additionalOffset = fileEntry.SizeInBytes;
                while (totalBytesWritten < compressedData.Length)
                {
                    int bytesToWrite = (int)Math.Min(usableBlockSize, compressedData.Length - totalBytesWritten);

                    WriteFileDataToBlock(currentBlockId, additionalOffset, compressedData, totalBytesWritten, bytesToWrite);

                    totalBytesWritten += bytesToWrite;

                    // Move to the next block if necessary
                    if (totalBytesWritten < fileSize)
                    {
                        // Get the next block ID in the chain
                        currentBlockId = GetNextBlockId(currentBlockId);
                        if (currentBlockId == 0)
                            throw new IOException("Unexpected end of block chain");
                    }

                    additionalOffset = 0;
                }

                AddElementAsChild(_currentDir.FirstBlockId, fileEntry.FirstBlockId);

                _currentDir = originalCurrent;

                WriteContainerContnet();
            }
            catch (Exception)
            {
                DeleteFile(fileEntry);
                _currentDir = originalCurrent;
                throw;
            }
        }

        public void CD(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            if (path == "/")
            {
                _currentDir = _rootDirectory;
                return;
            }

            if (path == ".")
                return;

            if (path == "..")
            {
                if (_currentDir.ParentBlockId == 0)
                    return;

                _currentDir = ReadDirectoryEntry(_currentDir.ParentBlockId);
                return;
            }

            bool isAbsolute = path.StartsWith("/");
            string[] pathComponents = StringHandler.SplitString(path, '/');
            Element targetDir = isAbsolute ? _rootDirectory : _currentDir;

            foreach (string component in pathComponents)
            {
                if(component == ".")
                    continue;

                if (component == "..")
                {
                    if (targetDir.ParentBlockId == 0)
                        continue;

                    targetDir = ReadDirectoryEntry(targetDir.ParentBlockId);
                    continue;
                }

                targetDir = FindChildDirectory(targetDir, component);
            }

            _currentDir = targetDir;
        }

        public void CopyOut(string source, string destination)
        {
            WriteContainerContnet();

            Element originalCurrent = _currentDir;

            var destInfo = StringHandler.SplitByLastOccurrence(source, '/');
            CD(destInfo.Item1);

            Element file = GetElementByName(_currentDir, destInfo.Item2);

            CopyFileToRealSystem(file, destination);

            _currentDir = originalCurrent;
        }

        public void List()
        {
            IOController.PrintLine("All files in container");

            TraverseDirectory(_rootDirectory);
        }

        public void RemoveFile(string path)
        {
            Element originalCurrent = _currentDir;

            var pathInfo = StringHandler.SplitByLastOccurrence(path, '/');
            CD(pathInfo.Item1);
            string fileName = pathInfo.Item2;

            Element fileToRemove = null;
            MyList<Element> entries = GetChildDirectoryEntries(_currentDir);
            for (int i = 0; i < entries.Count; i++)
            {
                if (StringHandler.Compare(entries[i].Name, fileName) == 0)
                {
                    fileToRemove = entries[i];
                    break;
                }
            }

            if (fileToRemove == null)
                throw new FileNotFoundException($"File '{fileName}' not found in directory '{pathInfo.Item1}'");

            if (fileToRemove.IsFolder)
                throw new IOException($"'{fileName}' is a directory, not a file. Use RemoveDirectory instead.");

            RemoveChildFromDirectory(_currentDir, fileToRemove.FirstBlockId);

            FreeFileBlocks(fileToRemove.FirstBlockId);

            _currentDir = originalCurrent;
        }

        public void PrintCurrentPath()
        {
            string name = _currentDir.Name;

            if (name == "")
                name = "/";

            IOController.PrintLine(name);
        }

        public void CraeteDir(string dirFullName)
        {
            Element originalCurrent = _currentDir;

            try
            {
                var pathInfo = StringHandler.SplitByLastOccurrence(dirFullName, '/');
                string parentPath = pathInfo.Item1;
                string dirName = pathInfo.Item2;

                CD(parentPath);

                if (FileExistsInCurrentDirectory(dirName))
                    throw new IOException($"A file or directory named '{dirName}' already exists");

                uint blockId = AllocateBlock(1);
                if (blockId == 0)
                    throw new IOException("Failed to allocate block for the directory");

                Element dirEntry = new Element
                {
                    Name = dirName,
                    CreationTime = DateTime.Now,
                    FirstBlockId = blockId,
                    ParentBlockId = _currentDir.FirstBlockId,
                    IsFolder = true,
                    ChildrenCount = 0
                };

                CreateElement(dirEntry);
                AddElementAsChild(_currentDir.FirstBlockId, dirEntry.FirstBlockId);

                _currentDir = originalCurrent;
            }
            catch (Exception)
            {
                _currentDir = originalCurrent;
                throw;
            }
        }

        public void WriteContainerContnet(int blocksCount = -1)
        {
            Console.WriteLine("Container Content:");

            // Read and display header information
            _containerStream.Seek(0, SeekOrigin.Begin);

            using (BinaryReader reader = new BinaryReader(_containerStream, Encoding.UTF8, true))
            {

                uint magicNumber = reader.ReadUInt32();
                ushort version = reader.ReadUInt16();
                uint blockSize = reader.ReadUInt32();
                uint headerSize = reader.ReadUInt32();
                uint nextAvailableBlock = reader.ReadUInt32();


                // Display header information
                Console.WriteLine("Header Information:");
                Console.WriteLine($"  Magic Number: 0x{magicNumber:X8}");
                Console.WriteLine($"  Version: {version}");
                Console.WriteLine($"  Block Size: {blockSize} bytes");
                Console.WriteLine($"  Header Size: {headerSize} bytes");
                Console.WriteLine($"  Next Available Block ID: {nextAvailableBlock}");

                Console.WriteLine();

                if (blocksCount == -1)
                    blocksCount = (int)nextAvailableBlock;

                // Read and display current blocks in use
                Console.WriteLine("\nAllocated Blocks:");

                for (uint j = 1; j < blocksCount; j++)
                {
                    try
                    {
                        long blockOffset = CalculateBlockOffset(j);
                        _containerStream.Seek(blockOffset, SeekOrigin.Begin);

                        uint blockId = reader.ReadUInt32();
                        byte status = reader.ReadByte();
                        uint nextBlock = reader.ReadUInt32();
                        uint dataLength = reader.ReadUInt32();

                        if (status == 1) // Block is in use
                        {
                            Console.WriteLine($"  Block {blockId}:");
                            Console.WriteLine($"    Status: Used");
                            Console.WriteLine($"    Next Block: {nextBlock}");
                            Console.WriteLine($"    Data Length: {dataLength} bytes");
                            Console.WriteLine();

                            int nameLength = reader.ReadInt32();
                            byte[] nameBytes = reader.ReadBytes(nameLength);
                            string name = Encoding.UTF8.GetString(nameBytes);
                            DateTime creationTime = DateTime.FromBinary(reader.ReadInt64());
                            uint firstBlockId = reader.ReadUInt32();
                            uint parentBlockId = reader.ReadUInt32();
                            bool isFolder = reader.ReadBoolean();
                            uint childrenCount = reader.ReadUInt32();

                            Console.WriteLine($"    Name: {name}");
                            Console.WriteLine($"    Creation Time: {creationTime}");
                            Console.WriteLine($"    First Block ID: {firstBlockId}");
                            Console.WriteLine($"    Parent Block ID: {parentBlockId}");
                            Console.WriteLine($"    Is Folder: {isFolder}");
                            Console.WriteLine($"    Children Count: {childrenCount}");

                            if (isFolder && childrenCount > 0)
                            {
                                Console.WriteLine($"    Children:");

                                // Read the children block IDs
                                for (int i = 0; i < childrenCount; i++)
                                {
                                    uint childBlockId = reader.ReadUInt32();

                                    Console.WriteLine($"      Child {i + 1}: Block {childBlockId}");
                                }
                            }
                        }

                        Console.ReadLine();
                    }
                    catch
                    {
                        // Skip if we can't read this block
                        Console.WriteLine($"  Block {j}: Failed to read");
                    }
                }
            }

            Console.WriteLine("END");
            Console.WriteLine();
        }

        private void FreeFileBlocks(uint firstBlockId)
        {
            uint currentBlockId = firstBlockId;

            while (currentBlockId != 0)
            {
                uint nextBlockId = GetNextBlockId(currentBlockId);
                MarkBlockAsFree(currentBlockId);
                currentBlockId = nextBlockId;
            }
        }

        private void MarkBlockAsFree(uint blockId)
        {
            long blockOffset = CalculateBlockOffset(blockId);

            _containerStream.Seek(blockOffset + BlockInfo.GetStatusPosition(), SeekOrigin.Begin);

            using (BinaryWriter writer = new BinaryWriter(_containerStream, Encoding.UTF8, true))
            {
                // Status
                writer.Write((byte)0);

                // Next
                writer.Write((uint)0);

                // Lenght
                writer.Write((uint)0);
            }

            uint dataSize = FileSystemConstants.BlockSize - BlockInfo.GetSize();
            byte[] emptyData = new byte[dataSize];

            _containerStream.Seek(blockOffset + BlockInfo.GetSizeWithoutChecksum(), SeekOrigin.Begin);
            _containerStream.Write(emptyData, 0, emptyData.Length);

            WriteBlockChecksum(blockId);
        }

        private void RemoveChildFromDirectory(Element directory, uint childBlockId)
        {
            long directoryOffset = CalculateBlockOffset(directory.FirstBlockId);
            int metadataSize = directory.SizeInBytes;
            long childrenListOffset = directoryOffset + BlockInfo.GetSizeWithoutChecksum() + metadataSize - sizeof(uint); // 

            _containerStream.Seek(childrenListOffset, SeekOrigin.Begin);

            using (BinaryReader reader = new BinaryReader(_containerStream, Encoding.UTF8, true))
            {
                uint childrenCount = reader.ReadUInt32();

                uint[] childBlockIds = new uint[childrenCount];
                int indexToRemove = -1;
                for (int i = 0; i < childrenCount; i++)
                {
                    childBlockIds[i] = reader.ReadUInt32();
                    if(childBlockIds[i] == childBlockId)
                        indexToRemove = i;
                }

                if (indexToRemove == -1)
                    throw new IOException("Child not found in directory");

                uint[] newChildBlockIds = new uint[childrenCount - 1];
                int newIndex = 0;

                for (int i = 0; i < childrenCount; i++)
                    if (i != indexToRemove)
                        newChildBlockIds[newIndex++] = childBlockIds[i];

                // Update directory's children count and write the new children list
                directory.ChildrenCount--;

                _containerStream.Seek(childrenListOffset, SeekOrigin.Begin);

                using (BinaryWriter writer = new BinaryWriter(_containerStream, Encoding.UTF8, true))
                {
                    writer.Write(directory.ChildrenCount);

                    // Write the new children list
                    for (int i = 0; i < directory.ChildrenCount; i++)
                    {
                        writer.Write(newChildBlockIds[i]);
                    }
                }
            }

            // Update the checksum for the directory block
            WriteBlockChecksum(directory.FirstBlockId);

            // Update in-memory objects if necessary
            if (directory.FirstBlockId == _currentDir.FirstBlockId)
                _currentDir.ChildrenCount--;
            else if (directory.FirstBlockId == _rootDirectory.FirstBlockId)
                _rootDirectory.ChildrenCount--;
        }

        private void TraverseDirectory(Element directory)
        {
            if (!directory.IsFolder)
                return;

            // Get all children of this directory
            MyList<Element> children = GetChildDirectoryEntries(directory);

            foreach (Element child in children)
            {
                if (child.IsFolder)
                    TraverseDirectory(child);
                else
                {
                    // It's a file, so list it
                    long fileSize = GetOriginalFileSize(child);
                    IOController.PrintLine($"{child.Name} ({fileSize} bytes)");
                }
            }
        }

        private long GetOriginalFileSize(Element element)
        {
            if (element.IsFolder)
                return 0;

            try
            {
                long blockOffset = CalculateBlockOffset(element.FirstBlockId);
                _containerStream.Seek(blockOffset + BlockInfo.GetSizeWithoutChecksum() + element.SizeInBytes, SeekOrigin.Begin);

                // Read the original size (first integer in the compressed data header)
                using (BinaryReader reader = new BinaryReader(_containerStream, Encoding.UTF8, true))
                {
                    reader.ReadInt32(); // Skip original size
                    int compressedSize = reader.ReadInt32();
                    int treeSize = reader.ReadInt32();
                    return compressedSize + treeSize;
                }
            }
            catch (Exception ex)
            {
                // Log the error but continue processing
                Console.WriteLine($"Warning: Could not read size for file {element.Name}: {ex.Message}");
                return 0;
            }
        }

        private Element FindChildDirectory(Element directory, string name)
        {
            if (!directory.IsFolder)
                throw new ArgumentException("Not a directory", nameof(directory));

            uint childCount = directory.ChildrenCount;

            if (childCount == 0)
                throw new DirectoryNotFoundException($"Directory '{name}' not found");

            long directoryOffset = CalculateBlockOffset(directory.FirstBlockId);
            int metadataSize = directory.SizeInBytes;
            long childrenListOffset = directoryOffset + BlockInfo.GetSizeWithoutChecksum() + metadataSize;

            using (BinaryReader reader = new BinaryReader(_containerStream, Encoding.UTF8, true))
            {
                for (int i = 0; i < childCount; i++)
                {
                    _containerStream.Seek(childrenListOffset + i * sizeof(uint), SeekOrigin.Begin);
                    uint childBlockId = reader.ReadUInt32();
                    Element child = ReadDirectoryEntry(childBlockId);

                    if (StringHandler.Compare(child.Name, name) == 0)
                    {
                        if (!child.IsFolder)
                            throw new DirectoryNotFoundException($"'{name}' is not a directory");

                        return child;
                    }
                }
            }

            throw new DirectoryNotFoundException($"Directory '{name}' not found");
        }

        private void CopyFileToRealSystem(Element file, string destination)
        {
            if (file.IsFolder)
                throw new ArgumentException("Source is a directory, not a file", nameof(file));

            uint usableBlockSize = FileSystemConstants.BlockSize - BlockInfo.GetSize();
            long blockOffset = CalculateBlockOffset(file.FirstBlockId);

            _containerStream.Seek(blockOffset + BlockInfo.GetSizeWithoutChecksum() + file.SizeInBytes, SeekOrigin.Begin);

            int treeLength, originalLength, compressedBodyLength;
            using (BinaryReader reader = new BinaryReader(_containerStream, Encoding.UTF8, leaveOpen: true))
            {
                originalLength = reader.ReadInt32();
                compressedBodyLength = reader.ReadInt32();
                treeLength = reader.ReadInt32();
            }

            HuffmanNode root = ReadHuffmanTree(file.FirstBlockId, file.SizeInBytes + 12, treeLength);

            using (FileStream destinationStream = new FileStream(destination, FileMode.Create, FileAccess.Write))
            {
                int decompressedBytes = 0;
                uint currentBlockId = file.FirstBlockId;
                long currentOffset = file.SizeInBytes + 12 + treeLength; // Position after tree data
                HuffmanNode currentNode = root;

                while (decompressedBytes < originalLength)
                {
                    // Check if we need to move to the next block
                    if (currentOffset >= usableBlockSize)
                    {
                        currentBlockId = GetNextBlockId(currentBlockId);
                        if (currentBlockId == 0)
                            throw new IOException("Unexpected end of block chain");
                        currentOffset = 0;
                    }

                    byte[] blockData = ReadBlockData(currentBlockId);

                    // In this part the idea is mine but I used claude.ai to help me with the implementation.
                    // Process each bit in the current block
                    for (int byteIndex = (int)currentOffset; byteIndex < usableBlockSize && decompressedBytes < originalLength; byteIndex++)
                    {
                        if (byteIndex >= blockData.Length)
                            break;

                        byte currentByte = blockData[byteIndex];

                        // Process each bit in the byte
                        for (int bitIndex = 7; bitIndex >= 0 && decompressedBytes < originalLength; bitIndex--)
                        {
                            // Extract the current bit
                            bool bit = (currentByte & (1 << bitIndex)) != 0;

                            // Navigate the Huffman tree
                            currentNode = bit ? currentNode.Right : currentNode.Left;

                            // If we've reached a leaf node, output the symbol
                            if (currentNode.IsLeaf)
                            {
                                destinationStream.WriteByte(currentNode.Value);
                                decompressedBytes++;
                                currentNode = root; // Reset to root for next symbol
                            }
                        }

                        // Update current offset
                        currentOffset++;
                    }
                }
            }
        }

        private HuffmanNode ReadHuffmanTree(uint startBlockId, int startOffset, int treeLength)
        {
            uint usableBlockSize = FileSystemConstants.BlockSize - BlockInfo.GetSize();
            uint currentBlockId = startBlockId;
            long currentOffset = startOffset;
            int remainingBytes = treeLength;

            byte[] treeData = new byte[treeLength];
            int bytesRead = 0;

            while (remainingBytes > 0)
            {
                if (currentOffset >= usableBlockSize)
                {
                    currentBlockId = GetNextBlockId(currentBlockId);
                    if (currentBlockId == 0)
                        throw new InvalidDataException("Unexpected end of block chain");
                    currentOffset = 0;
                }

                int bytesToReadNow = (int)Math.Min(remainingBytes, usableBlockSize - currentOffset);
                byte[] blockData = ReadBytesFromBlock(currentBlockId, currentOffset, bytesToReadNow);

                for (int i = 0; i < bytesToReadNow; i++)
                    treeData[bytesRead + i] = blockData[i];

                bytesRead += bytesToReadNow;
                currentOffset += bytesToReadNow;
                remainingBytes -= bytesToReadNow;
            }


            // Deserialize the tree
            return HuffmanCompression.DeserializeTree(treeData);
        }

        private byte[] ReadBytesFromBlock(uint blockId, long offsetInBlock, int count)
        {
            if (!VerifyBlockChecksum(blockId))
                throw new InvalidDataException($"Block {blockId} failed checksum verification");

            long blockOffset = CalculateBlockOffset(blockId);
            long dataOffset = blockOffset + BlockInfo.GetSizeWithoutChecksum() + offsetInBlock;

            // Seek to the position
            _containerStream.Seek(dataOffset, SeekOrigin.Begin);

            // Read the data
            byte[] data = new byte[count];
            int bytesRead = _containerStream.Read(data, 0, count);

            if (bytesRead < count)
                throw new EndOfStreamException($"Expected to read {count} bytes but got {bytesRead}");

            return data;
        }

        private Element GetElementByName(Element directory, string name)
        {
            if (!directory.IsFolder)
                throw new ArgumentException("Not a directory", nameof(directory));

            uint childCount = directory.ChildrenCount;

            long directoryOffset = CalculateBlockOffset(directory.FirstBlockId);
            int metadataSize = directory.SizeInBytes;
            long childrenListOffset = directoryOffset + BlockInfo.GetSizeWithoutChecksum() + metadataSize;


            using (BinaryReader reader = new BinaryReader(_containerStream, Encoding.UTF8, true))
            {
                for (int i = 0; i < childCount; i++)
                {
                    _containerStream.Seek(childrenListOffset + i * sizeof(uint), SeekOrigin.Begin);
                    uint childBlockId = reader.ReadUInt32();
                    Element child = ReadDirectoryEntry(childBlockId);

                    if (StringHandler.Compare(child.Name, name) == 0)
                        return child;
                }
            }

            throw new FileNotFoundException($"File '{name}' not found.");
        }

        private void DeleteFile(Element fileEntry)
        {
            throw new NotImplementedException();
        }

        private void AddElementAsChild(uint directoryBlockId, uint elementBlockId)
        {
            Element directory = ReadDirectoryEntry(directoryBlockId);

            long directoryOffset = CalculateBlockOffset(directoryBlockId);
            int metadataSize = directory.SizeInBytes;
            long childrenListOffset = directoryOffset + BlockInfo.GetSizeWithoutChecksum() + metadataSize - sizeof(uint); // Position right at the children count

            directory.ChildrenCount++;

            _containerStream.Seek(childrenListOffset, SeekOrigin.Begin);

            using (BinaryWriter writer = new BinaryWriter(_containerStream, Encoding.UTF8, true))
            {
                // The new children count
                writer.Write(directory.ChildrenCount);

                // Seek to the end of the existing children list to append the new child
                _containerStream.Seek((directory.ChildrenCount - 1) * sizeof(uint), SeekOrigin.Current);

                // Write the new child block ID
                writer.Write(elementBlockId);
            }

            // Update the checksum for the directory block
            WriteBlockChecksum(directoryBlockId);

            if (directoryBlockId == _currentDir.FirstBlockId)
                _currentDir.ChildrenCount++;

            else if (directoryBlockId == _rootDirectory.FirstBlockId)
                _rootDirectory.ChildrenCount++;
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
            Element element = new Element();

            long offset = CalculateBlockOffset(blockId);

            if (!VerifyBlockChecksum(blockId))
                throw new InvalidDataException($"Block {blockId} failed checksum verification");

            _containerStream.Seek(offset + BlockInfo.GetSizeWithoutChecksum(), SeekOrigin.Begin);

            using (BinaryReader reader = new BinaryReader(_containerStream, Encoding.UTF8, true))
            {
                int nameLength = reader.ReadInt32();
                byte[] nameBytes = reader.ReadBytes(nameLength);
                element.Name = Encoding.UTF8.GetString(nameBytes);

                element.CreationTime = DateTime.FromBinary(reader.ReadInt64());
                element.FirstBlockId = reader.ReadUInt32();
                element.ParentBlockId = reader.ReadUInt32();
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
            _containerStream.Seek(offset + BlockInfo.GetSizeWithoutChecksum(), SeekOrigin.Begin);

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
                if (magicNumber != FileSystemConstants.MagicNumber)
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
                IsFolder = true,
                ChildrenCount = 0
            };

            _currentDir = _rootDirectory;

            CreateElement(_rootDirectory);
        }

        private void CreateElement(Element folder)
        {
            long blockOffset = CalculateBlockOffset(folder.FirstBlockId);
            _containerStream.Seek(blockOffset + BlockInfo.GetSizeWithoutChecksum(), SeekOrigin.Begin);
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

            _containerStream.Seek(blockOffset + BlockInfo.GetContentLenghtPosition(), SeekOrigin.Begin);

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

                SaveBlockInContainer(prevId, blockId);

                if (firstId == 0)
                    firstId = blockId;

                prevId = blockId;
            }

            return firstId;
        }

        private void SaveBlockInContainer(uint parentId, uint blockId)
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
            long blockOffset = CalculateBlockOffset(blockId);

            _containerStream.Seek(blockOffset + BlockInfo.GetStatusPosition(), SeekOrigin.Begin);
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
            MyList<Element> entries = GetChildDirectoryEntries(_currentDir);

            foreach (var entry in entries)
            {
                if (StringHandler.Compare(entry.Name, fileName) == 0)
                    return true;
            }

            return false;
        }

        private MyList<Element> GetChildDirectoryEntries(Element directory)
        {
            if (!directory.IsFolder)
                throw new ArgumentException("Not a directory", nameof(directory));

            MyList<Element> entries = new MyList<Element>();

            uint childCount = directory.ChildrenCount;

            if (childCount == 0)
                return entries;

            long directoryOffset = CalculateBlockOffset(directory.FirstBlockId);
            int metadataSize = directory.SizeInBytes;
            long childrenListOffset = directoryOffset + BlockInfo.GetSizeWithoutChecksum() + metadataSize;

            using (BinaryReader reader = new BinaryReader(_containerStream, Encoding.UTF8, true))
            {
                for (int i = 0; i < childCount; i++)
                {
                    _containerStream.Seek(childrenListOffset + i * sizeof(uint), SeekOrigin.Begin);
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

        private void WriteFileDataToBlock(uint blockId, int additionalStreamOffset, byte[] data, int offset, int length)
        {
            long blockOffset = CalculateBlockOffset(blockId);

            _containerStream.Seek(blockOffset + BlockInfo.GetSizeWithoutChecksum() + additionalStreamOffset, SeekOrigin.Begin);

            // Write the data
            _containerStream.Write(data, offset, length);

            // Update the block's content length in the header
            UpdateBlockContentLenght(blockId, (uint)length);

            // Update the block's checksum
            WriteBlockChecksum(blockId);
        }
    }
}
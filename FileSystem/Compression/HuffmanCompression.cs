using FileSystem.Compression.BitUtils;
using FileSystem.DataStructures.Dictionary;
using FileSystem.DataStructures.List;
using FileSystem.DataStructures.List.SortedList;
using System.Reflection.PortableExecutable;

namespace FileSystem.Compression
{
    // The information about the Huffman encoding algorithm is taken from the following sources:
    // https://www.geeksforgeeks.org/huffman-coding-greedy-algo-3/
    // https://www.youtube.com/watch?v=JsTptu56GM8
    // claude.ai used for further researcha and solving implementation issues
    public static class HuffmanCompression
    {
        public static byte[] Compress(byte[] data)
        {
            // Handle empty data
            if (data == null || data.Length == 0)
                using (MemoryStream ms = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(ms))

                        writer.Write(0); // Original length = 0

                    return ms.ToArray();
                }

            MyDictionary<byte, int> frequencies = BuildFrequencyTable(data);

            HuffmanNode root = BuildHuffmanTree(frequencies);

            MyDictionary<byte, MyList<bool>> codeTable = BuildCodeTable(root);

            BitWriter bitWriter = new BitWriter();

            SerializeTree(root, bitWriter);

            EncodeData(data, codeTable, bitWriter);

            byte[] compressedBody = bitWriter.ToArray();

            using (MemoryStream outputStream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(outputStream))
                {
                    writer.Write(data.Length);

                    writer.Write(compressedBody.Length);
                    writer.Write(compressedBody);
                }

                return outputStream.ToArray();
            }
        }

        public static byte[] Decompress(byte[] compressedData)
        {
            using (MemoryStream ms = new MemoryStream(compressedData))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    int originalLength = reader.ReadInt32();

                    if (originalLength == 0)
                        return [];

                    int compressedBodyLength = reader.ReadInt32();
                    byte[] compressedBody = reader.ReadBytes(compressedBodyLength);

                    // Create a bit reader for the compressed body
                    BitReader bitReader = new BitReader(compressedBody);
                    HuffmanNode root = DeserializeTree(bitReader);

                    // Decode the data
                    byte[] decompressedData = new byte[originalLength];
                    int index = 0;

                    while (index < originalLength && bitReader.HasMoreBits)
                    {
                        // Start from the root for each symbol
                        HuffmanNode current = root;

                        // Traverse the tree until a leaf node is reached
                        while (!current.IsLeaf && bitReader.HasMoreBits)
                        {
                            bool bit = bitReader.ReadBit();
                            current = bit ? current.Right : current.Left;
                        }

                        // If we reached a leaf, output the symbol
                        if (current.IsLeaf)
                        {
                            decompressedData[index++] = current.Value;
                        }
                    }

                    return decompressedData;
                }
            }
        }

        private static HuffmanNode DeserializeTree(BitReader reader)
        {
            bool isLeaf = reader.ReadBit();

            if (isLeaf)
            {
                // Leaf node - read the symbol (8 bits)
                byte symbol = 0;
                for (int i = 7; i >= 0; i--)
                    if (reader.ReadBit())
                        symbol |= (byte)(1 << i);

                return new HuffmanNode(symbol, 0); // Frequency not needed for decompression
            }
            else
            {
                HuffmanNode left = DeserializeTree(reader);
                HuffmanNode right = DeserializeTree(reader);
                return new HuffmanNode(left, right);
            }
        }

        private static void EncodeData(byte[] data, MyDictionary<byte, MyList<bool>> codeTable, BitWriter writer)
        {
            foreach (byte b in data)
                writer.WriteBits(codeTable[b]);
        }

        // We need to store the three with the file in order to be able to decompress it later
        private static void SerializeTree(HuffmanNode node, BitWriter writer)
        {
            if (node.IsLeaf)
            {
                writer.WriteBit(true);

                // Write the only symbol (8 bits) of whitch the text is composed
                for (int i = 7; i >= 0; i--)
                    writer.WriteBit((node.Value & (1 << i)) != 0);
            }
            else
            {
                // Internal node - write 0 bit and then recursively write left and right children
                writer.WriteBit(false);
                SerializeTree(node.Left, writer);
                SerializeTree(node.Right, writer);
            }
        }

        private static MyDictionary<byte, MyList<bool>> BuildCodeTable(HuffmanNode root)
        {
            MyDictionary<byte, MyList<bool>> codeTable = new MyDictionary<byte, MyList<bool>>();
            BuildCodeTableRecursive(root, new MyList<bool>(), codeTable);
            return codeTable;
        }

        private static void BuildCodeTableRecursive(HuffmanNode node,
            MyList<bool> currentCode,
            MyDictionary<byte, MyList<bool>> codeTable)
        {
            if (node.IsLeaf)
            {
                // We've reached a leaf node, so we have a complete code for this symbol
                codeTable[node.Value] = new MyList<bool>(currentCode);
                return;
            }

            // Traverse left (add 0)
            if (node.Left != null)
            {
                currentCode.Add(false);
                BuildCodeTableRecursive(node.Left, currentCode, codeTable);
                currentCode.RemoveAt(currentCode.Count - 1);
            }

            // Traverse right (add 1)
            if (node.Right != null)
            {
                currentCode.Add(true);
                BuildCodeTableRecursive(node.Right, currentCode, codeTable);
                currentCode.RemoveAt(currentCode.Count - 1);
            }
        }

        private static HuffmanNode BuildHuffmanTree(MyDictionary<byte, int> frequencies)
        {
            MySortedList<HuffmanNode> nodes = new MySortedList<HuffmanNode>();

            foreach (var pair in frequencies)
                nodes.Add(new HuffmanNode(pair.Key, pair.Value));

            while (nodes.Count > 1)
            {
                nodes.Sort();

                HuffmanNode left = nodes[0];
                HuffmanNode right = nodes[1];

                nodes.RemoveAt(0);
                nodes.RemoveAt(0);

                HuffmanNode parent = new HuffmanNode(left, right);
                nodes.Add(parent);
            }

                return nodes[0];
        }

        private static MyDictionary<byte, int> BuildFrequencyTable(byte[] data)
        {
            MyDictionary<byte, int> frequencies = new MyDictionary<byte, int>();

            foreach (byte b in data)
            {
                if (frequencies.ContainsKey(b))
                    frequencies[b]++;
                else
                    frequencies[b] = 1;
            }

            return frequencies;
        }
    }
}


namespace FileSystem.Compression
{
    // The information about the Huffman encoding algorithm is taken from the following sources:
    // https://www.geeksforgeeks.org/huffman-coding-greedy-algo-3/
    // https://www.youtube.com/watch?v=JsTptu56GM8
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

            Dictionary<byte, int> frequencies = BuildFrequencyTable(data);

            HuffmanNode root = BuildHuffmanTree(frequencies);

            return null;
        }

        private static HuffmanNode BuildHuffmanTree(Dictionary<byte, int> frequencies)
        {
            List<HuffmanNode> priorityQueue = new List<HuffmanNode>();

            foreach (var pair in frequencies)
                priorityQueue.Add(new HuffmanNode(pair.Key, pair.Value));



            return null;
        }

        private static Dictionary<byte, int> BuildFrequencyTable(byte[] data)
        {
            Dictionary<byte, int> frequencies = new Dictionary<byte, int>();

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
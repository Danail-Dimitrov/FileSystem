namespace FileSystem.Compression
{
    public class HuffmanNode : IComparable<HuffmanNode>
    {
        public byte Value { get; set; }
        public int Frequency { get; set; }
        public HuffmanNode Left { get; set; }
        public HuffmanNode Right { get; set; }
        public HuffmanNode(byte value, int frequency)
        {
            Value = value;
            Frequency = frequency;
        }
        public HuffmanNode(int frequency, HuffmanNode left, HuffmanNode right)
        {
            Frequency = frequency;
            Left = left;
            Right = right;
        }
        public bool IsLeaf()
        {
            return Left == null && Right == null;
        }

        public int CompareTo(HuffmanNode? other)
        {
            if (other == null)
                return 1;
            
            return Frequency.CompareTo(other.Frequency);
        }
    }
}

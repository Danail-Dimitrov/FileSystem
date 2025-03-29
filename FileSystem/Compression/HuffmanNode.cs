namespace FileSystem.Compression
{
    public class HuffmanNode : IComparable<HuffmanNode>
    {
        public byte Value { get; set; }
        public int Frequency { get; set; }
        public HuffmanNode Left { get; set; }
        public HuffmanNode Right { get; set; }
        public bool IsLeaf => Left == null && Right == null;
        public HuffmanNode(byte value, int frequency)
        {
            Value = value;
            Frequency = frequency;
            Left = null;
            Right = null;
        }
        public HuffmanNode(HuffmanNode left, HuffmanNode right)
        {
            Frequency = left.Frequency + right.Frequency;
            Left = left;
            Right = right;
            Value = 0;
        }
        public int CompareTo(HuffmanNode? other)
        {
            if (other == null)
                return 1;
            
            return Frequency.CompareTo(other.Frequency);
        }
    }
}

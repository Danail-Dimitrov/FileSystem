using FileSystem.DataStructures.List;

namespace FileSystem.Compression.BitUtils
{
    // BitWriter to handle bit-level writing
    // IO actions operate on byte level, but the huffman encoding algorithm operates on bit level
    // This class bridges the gap between the two
    public class BitWriter
    {
        private byte _currentByte;
        private int _bitPosition;
        private readonly MemoryStream _stream;
        private int _bitsWritten;

        public BitWriter()
        {
            _stream = new MemoryStream();
            _currentByte = 0;
            _bitPosition = 0;
            _bitsWritten = 0;
        }

        public int BitsWritten => _bitsWritten;

        public void WriteBit(bool bit)
        {
            if (bit)
            {
                _currentByte |= (byte)(1 << (7 - _bitPosition));
            }

            _bitPosition++;

            if (_bitPosition == 8)
            {
                _stream.WriteByte(_currentByte);
                _currentByte = 0;
                _bitPosition = 0;
            }

            _bitsWritten++;
        }

        public void WriteBits(IMyList<bool> bits)
        {
            foreach (bool bit in bits)
            {
                WriteBit(bit);
            }
        }

        public void Flush()
        {
            if (_bitPosition > 0)
            {
                _stream.WriteByte(_currentByte);
                _currentByte = 0;
                _bitPosition = 0;
            }
        }

        public byte[] ToArray()
        {
            Flush();
            return _stream.ToArray();
        }
    }
}

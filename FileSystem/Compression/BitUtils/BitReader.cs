namespace FileSystem.Compression.BitUtils
{
    // Same as bit writer but for reading
    public class BitReader
    {
        private readonly byte[] _data;
        private int _bytePosition;
        private int _bitPosition;

        public BitReader(byte[] data)
        {
            _data = data;
            _bytePosition = 0;
            _bitPosition = 0;
        }

        public bool ReadBit()
        {
            if (_bytePosition >= _data.Length)
            {
                throw new EndOfStreamException("End of data reached while reading bits");
            }

            bool bit = (_data[_bytePosition] & (1 << (7 - _bitPosition))) != 0;

            _bitPosition++;
            if (_bitPosition == 8)
            {
                _bytePosition++;
                _bitPosition = 0;
            }

            return bit;
        }

        public bool HasMoreBits => _bytePosition < _data.Length || (_bytePosition == _data.Length - 1 && _bitPosition < 8);
    }
}

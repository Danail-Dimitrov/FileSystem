#pragma warning disable CS8602 

using System.Collections;

namespace FileSystem.DataStructures.MyList
{
    public class MyList<T> : IMyList<T>
    {
        protected T[] _items;
        protected int _count;

        private const uint DefaultCapacity = 4;
        private const short IncreaseStep = 2;

        public MyList()
        {
            _items = new T[DefaultCapacity];
            _count = 0;
        }

        public MyList(uint capacity)
        {
            _items = new T[capacity];
            _count = 0;
        }

        public int Count => _count;
        public int Capacity => _items.Length;
        public bool IsReadOnly => false;

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                    throw new IndexOutOfRangeException();
                return _items[index];
            }
            set
            {
                if (index < 0 || index >= _count)
                    throw new IndexOutOfRangeException();
                _items[index] = value;
            }
        }

        public void Add(T item)
        {
            if (_count == _items.Length)
                Resize();

            _items[_count++] = item;
        }

        private void Resize()
        {
            uint newCapacity = (uint)(_items.Length * IncreaseStep);

            T[] newItems = new T[newCapacity];

            for (int i = 0; i < _items.Length; i++)
                newItems[i] = _items[i];

            _items = newItems;
        }

        // If the count is zero I will still have the values in the array but they will be ignored.
        // They will be treated like garbage values and won't be accessible because of checks that the indexes don't surrpass coutn
        // Basically, the same way hard drives work.
        public void Clear()
        {
            _count = 0;
        }

        public bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if(array == null)
                throw new System.ArgumentNullException();

            if (arrayIndex < 0 || arrayIndex >= array.Length)
                throw new System.ArgumentOutOfRangeException();

            for(int i = arrayIndex; i < array.Length; i++)
                Add(array[i]);
        }

        // Tutorial for yield return:
        // https://www.youtube.com/watch?v=uv74SZ5MX5Q
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
                yield return _items[i];
        }

        public int IndexOf(T item)
        {
            for (int i = 0; i < _count; i++)
                if (_items[i].Equals(item))
                    return i;

            return -1;
        }

        public void Insert(int index, T item)
        {
            if (index < 0 || index > _count)
                throw new IndexOutOfRangeException($"Index {index} is out of range [0, {_count}]");

            if (_count == _items.Length)
                Resize();

            // Shift elements one position further right
            for(int i = index; i < _count; i++)
                _items[i + 1] = _items[i];

            _items[index] = item;
            _count++;
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _count)
                throw new IndexOutOfRangeException($"Index {index} is out of range [0, {_count - 1}]");

            for (int i = index; i < _count - 1; i++)
                _items[i] = _items[i + 1];

            _count--;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Reverse()
        {
            uint startIndex = 0;
            uint endIndex = (uint)(_count - 1);
            while (startIndex < endIndex)
            {
                T temp = _items[startIndex];
                _items[startIndex] = _items[endIndex];
                _items[endIndex] = temp;

                startIndex++;
                endIndex--;
            }
        }
    }
}

#pragma warning disable CS8600 
#pragma warning disable CS8625 

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace FileSystem.DataStructures.Dictionary
{
    public class MyDictionary<TKey, TValue> : IMyDictionary<TKey, TValue> where TKey : IComparable
    {
        private class Node
        {
            public TKey Key { get; set; }
            public TValue Value { get; set; }
            public Node Next { get; set; }
            public Node Prev { get; set; }
            public Node(TKey key, TValue value)
            {
                Key = key;
                Value = value;
                Next = null;
                Prev = null;
            }
        }

        private Node _head;
        private Node _tail;
        private int _count;

        public MyDictionary()
        {
            _head = null;
            _tail = null;
            _count = 0;
        }

        public TValue this[TKey key]
        {
            get => TryGetValue(key, out TValue value) ? value : throw new KeyNotFoundException();
            set
            {
                Node current = _head;
                while (current != null)
                {
                    if (current.Key.CompareTo(key) == 0)
                    {
                        current.Value = value;
                        return;
                    }
                    current = current.Next;
                }

                Add(key, value);
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                List<TKey> keys = new List<TKey>();
                Node current = _head;
                while (current != null)
                {
                    keys.Add(current.Key);
                    current = current.Next;
                }
                return keys;
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                List<TValue> values = new List<TValue>();
                Node current = _head;
                while (current != null)
                {
                    values.Add(current.Value);
                    current = current.Next;
                }
                return values;
            }
        }

        public int Count => _count;

        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (ContainsKey(key))
                throw new ArgumentException("An element with the same key already exists in the dictionary.");

            Node node = new Node(key, value);
            if (_head == null)
            {
                _head = node;
                _tail = node;
            }
            else
            {
                node.Prev = _tail;
                _tail.Next = node;
                _tail = node;
            }

            _count++;
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _head = null;
            _tail = null;
            _count = 0;
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }

        public bool ContainsKey(TKey key)
        {
            Node current = _head;

            while (current != null)
            {
                if (current.Key.CompareTo(key) == 0)
                    return true;

                current = current.Next;
            }

            return false;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            for (int i = arrayIndex; i < array.Length; i++)
                Add(array[i]);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            Node current = _head;

            while (current != null)
            {
                yield return new KeyValuePair<TKey, TValue>(current.Key, current.Value);
                current = current.Next;
            }
        }

        public bool Remove(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (_head == null)
                return false;

            if (_head.Key.CompareTo(key) == 0)
            {
                _head.Next.Prev = null;
                _head = _head.Next;
                _count--;
                return true;
            }

            Node current = _head;
            while (current.Next != null)
            {
                if (_head.Key.CompareTo(key) == 0)
                {
                    current.Prev.Next = current.Next;
                    current.Next.Prev = current.Prev;
                    _count--;
                    return true;
                }

                current = current.Next;
            }

            return false;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            Node current = _head;
            while (current != null)
            {
                if (current.Key.CompareTo(key) == 0)
                {
                    value = current.Value;
                    return true;
                }

                current = current.Next;
            }

            value = default;
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
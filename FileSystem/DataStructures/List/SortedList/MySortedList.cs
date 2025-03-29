namespace FileSystem.DataStructures.List.SortedList
{
    public class MySortedList<T> : MyList<T>, IMySortedList<T> where T : IComparable<T>
    {
        public MySortedList() : base()
        { }

        public MySortedList(uint capacity) : base(capacity)
        {
        }

        public void Sort()
        {
            MergeSort(0, _count - 1);
        }

        // Implementation from lectures
        private void MergeSort(int l, int r)
        {
            if (l < r)
            {
                int m = (l + r) / 2;

                MergeSort(l, m);
                MergeSort(m + 1, r);

                Merge(l, m, r);
            }
        }

        private void Merge(int l, int m, int r)
        {
            int n1 = m - l + 1;
            int n2 = r - m;

            T[] L = new T[n1];
            for (int i = 0; i < n1; i++)
                L[i] = _items[l + i];

            T[] R = new T[n2];
            for (int i = 0; i < n2; i++)
                R[i] = _items[m + 1 + i];

            int t = 0;
            int j = 0;
            int k = l;
            while (t < n1 && j < n2)
            {
                if (L[t].CompareTo(R[j]) <= 0)
                    _items[k++] = L[t++];
                else
                    _items[k++] = R[j++];
            }

            while (t < n1)
                _items[k++] = L[t++];

            while (j < n2)
                _items[k++] = R[j++];
        }
    }
}
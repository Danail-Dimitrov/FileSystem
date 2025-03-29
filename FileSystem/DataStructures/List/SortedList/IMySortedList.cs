namespace FileSystem.DataStructures.List.SortedList
{
    public interface IMySortedList<T> : IMyList<T> where T : IComparable<T>
    {
        void Sort();
    }
}
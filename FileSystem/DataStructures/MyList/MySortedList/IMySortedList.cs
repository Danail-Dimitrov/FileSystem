namespace FileSystem.DataStructures.MyList.MySortedList
{
    public interface IMySortedList<T> : IMyList<T> where T : IComparable
    {
        void Sort();
    }
}

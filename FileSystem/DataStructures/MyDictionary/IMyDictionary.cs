namespace FileSystem.DataStructures.MyDictionary
{
    public interface IMyDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TKey : IComparable
                                                                             where TValue : IComparable  
    {
    }
}

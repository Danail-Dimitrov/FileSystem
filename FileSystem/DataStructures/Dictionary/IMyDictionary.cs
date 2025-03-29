namespace FileSystem.DataStructures.Dictionary
{
    public interface IMyDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TKey : IComparable
    {
    }
}
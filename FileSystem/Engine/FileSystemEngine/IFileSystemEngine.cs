namespace FileSystem.Engine.FileSystemEngine
{
    public interface IFileSystemEngine
    {
        /// <summary>
        /// This is the implementation of the cpin command.
        /// It takes a file from the real file system and copies it into the container.
        /// </summary>
        /// <param name="source">The real file on the pc.</param>
        /// <param name="destination">The destination in the container</param>
        void CopyIn(string source, string destination);

        void CopyOut(string source, string destination);

        void CD(string path);
        void List();

        void RemoveFile(string path);
        void WriteContainerContnet(int blocksCount = -1);
    }
}

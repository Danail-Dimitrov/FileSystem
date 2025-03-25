namespace FileSystem.Engine.FileSystemEngine
{
    /// <summary>
    /// This class is responsible for the file system operations.
    /// </summary>
    public class FileSystemEngine : IFileSystemEngine
    {
        /// <summary>
        /// This is the implementation of the cpin command.
        /// It takes a file from the real file system and copies it into the container.
        /// </summary>
        /// <param name="source">The real file on the pc.</param>
        /// <param name="destination">The destination in the container</param>
        public void CopyIn(string source, string destination)
        {
            throw new System.NotImplementedException();
        }
    }
}

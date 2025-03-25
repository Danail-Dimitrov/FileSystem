using FileSystem.Engine.FileSystemEngine;

namespace FileSystem.Commands
{
    /// <summary>
    /// Base class for all commands.
    /// Implementation of the command pattern.
    /// The execute method has IFsEngine as a parameter to allow the command to interact with the file system.
    /// The other options would be to pass the engine at the constructor. 
    /// I prefer this approach, beacuse I don't dinf it suitable for the command to be the place where the engine is decided.
    /// I find it more appropriate for the ApplicationEngine to decide which FileSystemEngine to use. Rather than havong the command remember whaterver it was given.
    /// </summary>
    public abstract class Command
    {
        public abstract void Execute(IFileSystemEngine fsEngine, string[] args);
    }
}

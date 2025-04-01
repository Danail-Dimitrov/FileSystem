using FileSystem.Engine.FileSystemEngine;

namespace FileSystem.Commands
{
    public class InfoCommand : Command
    {
        public override void Execute(IFileSystemEngine fsEngine, string[] args)
        {
            fsEngine.WriteContainerContnet();
        }
    }
}

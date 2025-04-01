using FileSystem.Engine.FileSystemEngine;

namespace FileSystem.Commands
{
    public class ListCommand : Command
    {
        public override void Execute(IFileSystemEngine fsEngine, string[] args)
        {
            fsEngine.List();
        }
    }
}

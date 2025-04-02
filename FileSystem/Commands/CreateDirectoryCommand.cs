using FileSystem.Engine.FileSystemEngine;

namespace FileSystem.Commands
{
    public class CreateDirectoryCommand : Command
    {
        public override void Execute(IFileSystemEngine fsEngine, string[] args)
        {
            fsEngine.CraeteDir(args[1]);
        }
    }
}

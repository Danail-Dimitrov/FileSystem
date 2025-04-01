using FileSystem.Engine.FileSystemEngine;

namespace FileSystem.Commands
{
    public class RemoveFileCommand : Command
    {
        public override void Execute(IFileSystemEngine fsEngine, string[] args)
        {
            fsEngine.RemoveFile(args[1]);
        }
    }
}

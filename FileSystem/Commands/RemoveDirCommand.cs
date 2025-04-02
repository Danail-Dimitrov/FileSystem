using FileSystem.Engine.FileSystemEngine;

namespace FileSystem.Commands
{
    public class RemoveDirCommand : Command
    {
        public override void Execute(IFileSystemEngine fsEngine, string[] args)
        {
            fsEngine.RemoveDir(args[1]);
        }
    }
}

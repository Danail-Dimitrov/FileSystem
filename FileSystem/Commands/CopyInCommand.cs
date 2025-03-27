using FileSystem.Engine.FileSystemEngine;

namespace FileSystem.Commands
{
    public class CopyInCommand : Command
    {
        public override void Execute(IFileSystemEngine fsEngine, string[] args)
        {
            fsEngine.CopyIn(args[0], args[1]);
        }
    }
}

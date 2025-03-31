using FileSystem.Engine.FileSystemEngine;

namespace FileSystem.Commands
{
    public class CopyOutCommand : Command
    {
        public override void Execute(IFileSystemEngine fsEngine, string[] args)
        {
            fsEngine.CopyOut(args[0], args[1]);
        }
    }
}

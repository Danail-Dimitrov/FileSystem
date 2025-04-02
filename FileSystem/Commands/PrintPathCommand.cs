using FileSystem.Engine.FileSystemEngine;

namespace FileSystem.Commands
{
    public class PrintPathCommand : Command
    {
        public override void Execute(IFileSystemEngine fsEngine, string[] args)
        {
            fsEngine.PrintCurrentPath();
        }
    }
}

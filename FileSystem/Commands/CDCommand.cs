using FileSystem.Engine.FileSystemEngine;

namespace FileSystem.Commands
{
    public class CDCommand : Command
    {
        public override void Execute(IFileSystemEngine fsEngine, string[] args)
        {
            fsEngine.CD(args[1]);
        }
    }
}

using FileSystem.Engine.FileSystemEngine;

namespace FileSystem.Commands
{
    public class CreateDirectoryCommand : Command
    {
        public override void Execute(IFileSystemEngine fsEngine, string[] args)
        {
            if (args.Length < 2)
                throw new ArgumentException("The command createdir requires a path as an argument.");

            fsEngine.CraeteDir(args[1]);
        }
    }
}

using FileSystem.Engine.FileSystemEngine;

namespace FileSystem.Commands
{
    public class RemoveFileCommand : Command
    {
        public override void Execute(IFileSystemEngine fsEngine, string[] args)
        {
            if (args.Length < 2)
                throw new ArgumentException("The command removefile requires a path as an argument.");

            fsEngine.RemoveFile(args[1]);
        }
    }
}

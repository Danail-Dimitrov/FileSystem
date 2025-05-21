using FileSystem.Engine.FileSystemEngine;

namespace FileSystem.Commands
{
    public class RemoveDirCommand : Command
    {
        public override void Execute(IFileSystemEngine fsEngine, string[] args)
        {
            if(args.Length < 2)
                throw new ArgumentException("The command removedir requires a path as an argument.");

            fsEngine.RemoveDir(args[1]);
        }
    }
}

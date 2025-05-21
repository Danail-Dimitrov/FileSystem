using FileSystem.Engine.FileSystemEngine;

namespace FileSystem.Commands
{
    public class CopyInCommand : Command
    {
        public override void Execute(IFileSystemEngine fsEngine, string[] args)
        {
            if(args.Length < 3)
                throw new ArgumentException("The command copyin requires a source and a destination as arguments.");

            fsEngine.CopyIn(args[1], args[2]);
        }
    }
}

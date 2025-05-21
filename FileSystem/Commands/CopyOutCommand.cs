using FileSystem.Engine.FileSystemEngine;

namespace FileSystem.Commands
{
    public class CopyOutCommand : Command
    {
        public override void Execute(IFileSystemEngine fsEngine, string[] args)
        {
            if (args.Length < 3)
                throw new ArgumentException("The command copyout requires a source and a destination as arguments.");

            fsEngine.CopyOut(args[1], args[2]);
        }
    }
}

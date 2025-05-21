using FileSystem.Engine.FileSystemEngine;

namespace FileSystem.Commands
{
    public class CDCommand : Command
    {
        public override void Execute(IFileSystemEngine fsEngine, string[] args)
        {
            if (args.Length < 2)
                throw new ArgumentException("The command cd requires a path as an argument.");
            
            fsEngine.CD(args[1]);
        }
    }
}

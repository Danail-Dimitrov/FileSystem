using FileSystem.Commands;
using FileSystem.Engine.FileSystemEngine;
using FileSystem.IO;
using FileSystem.Utilities;

namespace FileSystem.Engine.ApplicationEngine
{
    /// <summary>
    /// This class is responsible for running the application.
    /// It hosts the application loop and makes calls to the necessary components to ensure that the application runs as expected.
    /// </summary>
    public class ApplicationEngine : IApplicationEngine
    {
        private IFileSystemEngine _fsEngine;
        private Command  _cin;
        private Command _cpout;
        private Command _cd;
        private Command _ls;
        private Command _rm;
        private Command _info;

        public ApplicationEngine(IFileSystemEngine engine)
        {
            IsRunning = false;
            _fsEngine = engine;
            InitComands();
        }

        private void InitComands()
        {
            _cin = new CopyInCommand();
            _cpout = new CopyOutCommand();
            _cd = new CDCommand();
            _ls = new ListCommand();
            _info = new InfoCommand();
        }

        /// <summary>
        /// A flag that indicates whether the application is running.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// This method is responsible for starting the application.
        /// </summary>
        public void Run()
        {
            IsRunning = true;

            IOController.PrintInstructions();

            try
            {
                RunLoop();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fatal Error: " + ex.Message);
            }
        }

        /// <summary>
        /// This method is responsible for running the main loop of the application.
        /// </summary>
        private void RunLoop()
        {
            while (IsRunning)
            {
                IOController.Print(">" );

                string command = IOController.GetInput();
                string[] args = StringHandler.SplitString(command, ' ');

                switch(args[0])
                {
                    case "cpin":
                        _cin.Execute(_fsEngine, args);
                        break;
                    case "cpout":
                        _cpout.Execute(_fsEngine, args);
                        break;
                    case "ls":
                        _ls.Execute(_fsEngine, args);
                        break;
                    case "rm":
                        _rm.Execute(_fsEngine, args);
                        break;
                    case "md":
                        break;
                    case "cd":
                        _cd.Execute(_fsEngine, args);
                        break;
                    case "rd":
                        break;
                    case "info":
                        _info.Execute(_fsEngine, args);
                        break;
                    case "q":
                        IsRunning = false;
                        break;
                    default:
                        Console.WriteLine("Invalid command.");
                        break;
                }
            }
        }
    }
}

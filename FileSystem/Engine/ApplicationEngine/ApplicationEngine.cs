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
        private Command _cout;

        public ApplicationEngine(IFileSystemEngine engine)
        {
            IsRunning = false;
            _fsEngine = engine;
            InitComands();
        }

        private void InitComands()
        {
            _cin = new CopyInCommand();
            _cout = new CopyOutCommand();
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
                string command = IOController.GetInput();
                string[] args = StringHandler.SplitString(command, ' ');

                switch(args[0])
                {
                    case "cpin":
                        _cin.Execute(_fsEngine, args);
                        break;
                    case "cpout":
                        _cout.Execute(_fsEngine, args);
                        break;
                    case "ls":
                        break;
                    case "rm":
                        break;
                    case "md":
                        break;
                    case "cd":
                        break;
                    case "rd":
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

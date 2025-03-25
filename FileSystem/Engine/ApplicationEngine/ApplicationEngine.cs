using FileSystem.IO;

namespace FileSystem.Engine.ApplicationEngine
{
    /// <summary>
    /// This class is responsible for running the application.
    /// It hosts the application loop and makes calls to the necessary components to ensure that the application runs as expected.
    /// </summary>
    public class ApplicationEngine : IApplicationEngine
    {
        public ApplicationEngine()
        {
            IsRunning = false;
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

            }
        }
    }
}

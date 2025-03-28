﻿namespace FileSystem.IO
{
    /// <summary>
    /// Responsible for IO on the console.
    /// I could make it work with any stream but for the porpuses of this project, I will keep this part simple.
    /// </summary>
    public static class IOController
    {
        /// <summary>
        /// Prints the instructions for the user.
        /// </summary>
        public static void PrintInstructions()
        {
            Console.WriteLine("Commands:");
            Console.WriteLine("  cpin <source> <destination> - Copy file into container");
            Console.WriteLine("  cpout <source> <destination> - Copy file from container");
            Console.WriteLine("  ls - List container contents");
            Console.WriteLine("  rm <name> - Remove file or directory");
            Console.WriteLine("  md <name> - Create directory");
            Console.WriteLine("  cd <path> - Change directory");
            Console.WriteLine("  rd <name> - Remove directory");
        }
    }
}

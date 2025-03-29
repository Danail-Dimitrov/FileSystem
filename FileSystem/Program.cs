using FileSystem.Compression;
using FileSystem.DataStructures.List.SortedList;
using FileSystem.DependancyInjection;
using FileSystem.Engine.ApplicationEngine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text;

namespace FileSystem
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var host = DependancyInjectionSetUp.CreateHostBuilder().Build();

            var appEngine = host.Services.GetRequiredService<IApplicationEngine>();
            appEngine.Run();
        }
    }
}

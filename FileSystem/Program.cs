using FileSystem.DependancyInjection;
using FileSystem.Engine.ApplicationEngine;
using Microsoft.Extensions.DependencyInjection;

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

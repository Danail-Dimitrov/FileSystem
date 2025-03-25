using FileSystem.Engine.ApplicationEngine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FileSystem.DependancyInjection
{
    /// <summary>
    /// This class is responsible for setting up the dependancy injection.
    /// Dependancy inection has manny benefits, but here I used it mainly because I wanted to practice it.
    /// </summary>
    public static class DependancyInjectionSetUp
    {
        public static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IApplicationEngine, ApplicationEngine>();
                });
    }
}

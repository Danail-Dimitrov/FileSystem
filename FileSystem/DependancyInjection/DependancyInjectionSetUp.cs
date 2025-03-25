using FileSystem.Engine.ApplicationEngine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FileSystem.DependancyInjection
{
    public static class DependancyInjectionSetUp
    {
        public static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    // Register your services here
                    services.AddSingleton<IApplicationEngine, ApplicationEngine>();
                });
    }
}

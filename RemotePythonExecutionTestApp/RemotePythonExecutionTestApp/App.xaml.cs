using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;
using Prism.Modularity;
using RemotePythonExecutionTestApp.Modules.ModuleName;
using RemotePythonExecutionTestApp.Services;
using RemotePythonExecutionTestApp.Services.Interfaces;
using RemotePythonExecutionTestApp.Views;
using Serilog;
using Serilog.Core;

namespace RemotePythonExecutionTestApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IRemotePythonRunnerService, RemotePythonRunnerService>();

            containerRegistry.RegisterServices(services =>
            {
                
                Logger mainLogger = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.File("logs/log-.txt",
                            rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10,
                            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                    .CreateLogger();

                services.AddLogging(s => s.AddSerilog(mainLogger, dispose: true));
            });
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<ModuleNameModule>();
        }
    }
}

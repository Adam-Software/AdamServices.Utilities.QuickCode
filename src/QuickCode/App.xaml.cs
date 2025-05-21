using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using QuickCode.Modules.ContentRegion;
using QuickCode.Services;
using QuickCode.Services.Interfaces;
using QuickCode.Views;
using Serilog;
using Serilog.Core;

namespace QuickCode
{
    public partial class App
    {

        private bool mIsDisposing;

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
            moduleCatalog.AddModule<ContentRegionModule>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            OnAppCrashOrExit();

            base.OnExit(e);
        }

        private void OnAppCrashOrExit()
        {
           Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!mIsDisposing)
            {
                if (disposing)
                {
                    IRegionManager regionManager = Container.Resolve<IRegionManager>();

                    foreach (var region in regionManager.Regions)
                    {
                        region.RemoveAll();
                    }

                    IRemotePythonRunnerService remotePythonRunner = Container.Resolve<IRemotePythonRunnerService>();

                    remotePythonRunner.Dispose();
                }
            }

            mIsDisposing = true;
        }
    }
}

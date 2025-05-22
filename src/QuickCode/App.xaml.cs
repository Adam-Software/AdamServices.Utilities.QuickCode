using Example;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using QuickCode.Modules.ContentRegion;
using QuickCode.Services;
using QuickCode.Services.Interfaces;
using QuickCode.Views;
using Serilog;
using Serilog.Core;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;

namespace QuickCode
{
    public partial class App : PrismApplication, IDisposable
    { 
        public App()
        {
            SetupUnhandledExceptionHandling();
        }

        private bool mIsDisposing;

        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            RegisterServices(containerRegistry);

            containerRegistry.RegisterSingleton<IRemotePythonRunnerService, RemotePythonRunnerService>();
        }

        private void RegisterServices(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterServices((services) =>
            {
                ConfigurationBuilder configurationBuilder = new();
                configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfigurationRoot configuration = configurationBuilder.Build();

                AppSettingService appSettingService = new();
                IConfigurationSection appSettingsSection = configuration.GetSection("AppSettingsOptions");
                appSettingsSection.Bind(appSettingService);
                services.AddSingleton<IAppSettingService>(appSettingService);

                services.AddLogging(loggingBuilder =>
                {
                    Logger logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(configuration)
                        .CreateLogger();

                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddSerilog(logger, dispose: true);
                });
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
            GC.SuppressFinalize(this);
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

        #region UnhandledExceptionHandling

        private void SetupUnhandledExceptionHandling()
        {
            // Catch exceptions from all threads in the AppDomain.
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
                ShowUnhandledException(args.ExceptionObject as Exception, "AppDomain.CurrentDomain.UnhandledException", false);

            // Catch exceptions from each AppDomain that uses a task scheduler for async operations.
            TaskScheduler.UnobservedTaskException += (sender, args) =>
                ShowUnhandledException(args.Exception, "TaskScheduler.UnobservedTaskException", false);

            // Catch exceptions from a single specific UI dispatcher thread.
            Current.Dispatcher.UnhandledException += (sender, args) =>
            {
                // If we are debugging, let Visual Studio handle the exception and take us to the code that threw it.
                if (!Debugger.IsAttached)
                {
                    args.Handled = true;
                    ShowUnhandledException(args.Exception, "Dispatcher.UnhandledException", true);
                }
            };
        }

        private void ShowUnhandledException(Exception e, string unhandledExceptionType, bool promptUserForShutdown)
        {
            ILogger<App> logger = Container.Resolve<ILogger<App>>();

            switch (e)
            {
                case SocketException:
                    {
                        logger.LogError($"SocketException. Is normaly when server unaviable");
                        return;
                    }
                case AggregateException:
                    {
                        logger.LogError($"AggregateException. Is normaly when server unaviable");
                        return;
                    }
            }

            var messageBoxTitle = $"Unexpected Error Occurred: {unhandledExceptionType}";
            var messageBoxMessage = $"The following exception occurred:\n\n{e}";
            var messageBoxButtons = MessageBoxButton.OK;

            if (promptUserForShutdown)
            {
                messageBoxMessage += "\n\nNormally the app would die now. Should we let it die?";
                messageBoxButtons = MessageBoxButton.YesNo;
            }

            logger.LogCritical("Unhandled exception {exception}:", e.Message);

            if (MessageBox.Show(messageBoxMessage, messageBoxTitle, messageBoxButtons) == MessageBoxResult.OK)
            {
                //Current.MainWindow.Close();
                //Current.Shutdown();
            }
        }


        #endregion
    }
}

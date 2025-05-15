using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prism.Mvvm;
using System;
using System.Reflection;

namespace QuickCode.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region Services

        private readonly ILogger<MainWindowViewModel> mLogger;

        #endregion

        public MainWindowViewModel(IServiceProvider serviceProvider)
        {
            mLogger = serviceProvider.GetService<ILogger<MainWindowViewModel>>();

            mLogger.LogInformation("Load main window model");

            var name = Assembly.GetEntryAssembly().GetName().Name;
            var version = Assembly.GetEntryAssembly().GetName().Version;

            Title = $"{name} v.{version.Major}.{version.Minor}.{version.Build}";
        }

        private string mTitle = "";
        public string Title
        {
            get { return mTitle; }
            set { SetProperty(ref mTitle, value); }
        }
    }
}

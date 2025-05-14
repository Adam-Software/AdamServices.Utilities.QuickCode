using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prism.Mvvm;
using System;

namespace RemotePythonExecutionTestApp.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region Services

        private ILogger<MainWindowViewModel> mLogger;

        #endregion

        public MainWindowViewModel(IServiceProvider serviceProvider)
        {
            mLogger = serviceProvider.GetService<ILogger<MainWindowViewModel>>();

            mLogger.LogInformation("Load main window model");
        }

        private string _title = "Prism Application";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }
    }
}

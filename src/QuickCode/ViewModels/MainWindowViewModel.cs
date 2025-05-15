using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prism.Mvvm;
using System;

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
        }

        private string _title = "Quick Happened Code";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }
    }
}

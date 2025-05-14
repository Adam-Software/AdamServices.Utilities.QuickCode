using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Regions;
using RemotePythonExecutionTestApp.Core.Mvvm;
using RemotePythonExecutionTestApp.Services.Interfaces;
using System;

namespace RemotePythonExecutionTestApp.Modules.ModuleName.ViewModels
{
    public class ViewAViewModel : RegionViewModelBase
    {

        #region DelegateCommand

        public DelegateCommand ConnectCommand { get; }
        public DelegateCommand InterpretCommand { get; }

        #endregion

        #region Services

        private readonly ILogger<ViewAViewModel> mLogger;
        private readonly IRemotePythonRunnerService mRemotePythonRunner;

        #endregion


        #region ~

        public ViewAViewModel(IRegionManager regionManager, IRemotePythonRunnerService remotePythonRunner, ILogger<ViewAViewModel> logger) : base(regionManager)
        {
            mLogger = logger;
            mRemotePythonRunner = remotePythonRunner;

            ConnectCommand = new DelegateCommand(Connect, ConnectCanExecute);
            InterpretCommand = new DelegateCommand(Interpret, InterpretCanExecute);
        }

        #endregion

        #region Navigation

        public override void OnNavigatedTo(NavigationContext navigationContext) {}

        #endregion

        #region Public fields

        private string mSourceCode;
        public string SourceCode
        {
            get { return mSourceCode; }
            set 
            { 
                var isNewValue= SetProperty(ref mSourceCode, value);

                if (isNewValue)
                    InterpretCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion


        #region DelegateCommand methods

        private void Connect()
        {
            mRemotePythonRunner.ConnectAsync();
        }

        private bool ConnectCanExecute()
        {
            return true;
        }

        private void Interpret()
        {
            var sourceCode = SourceCode;
            mRemotePythonRunner.SendCode(sourceCode);
        }

        private bool InterpretCanExecute()
        {
            return !string.IsNullOrEmpty(SourceCode);
        }

        #endregion

    }
}

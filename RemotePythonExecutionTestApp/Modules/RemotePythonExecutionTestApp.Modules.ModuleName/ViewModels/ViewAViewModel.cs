using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Regions;
using RemotePythonExecutionTestApp.Core.Mvvm;
using RemotePythonExecutionTestApp.Services.Interfaces;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace RemotePythonExecutionTestApp.Modules.ModuleName.ViewModels
{
    public class ViewAViewModel : RegionViewModelBase
    {

        #region DelegateCommand

        public DelegateCommand StartCommand { get; }
        public DelegateCommand StopCommand { get; }
        public DelegateCommand<object> SendDebuggerCommand { get; }

        #endregion

        #region Services

        private readonly ILogger<ViewAViewModel> mLogger;
        private readonly IRemotePythonRunnerService mRemotePythonRunner;

        #endregion

        #region Var

        private bool mIsExecutionStop = false;

        #endregion

        #region ~

        public ViewAViewModel(IRegionManager regionManager, IRemotePythonRunnerService remotePythonRunner, ILogger<ViewAViewModel> logger) : base(regionManager)
        {
            mLogger = logger;
            mRemotePythonRunner = remotePythonRunner;

            StartCommand = new DelegateCommand(Start, StartCanExecute);
            StopCommand = new DelegateCommand(Stop, StopCanExecute);
            SendDebuggerCommand = new DelegateCommand<object>(SendDebugger, SendDebuggerCanExecute);

            Subscribe();
        }

        #endregion

        #region Subscribe/Unsubscribe

        private void Subscribe()
        {
            mRemotePythonRunner.RaiseDataReceivedEvent += RaiseDataReceivedEvent;
        }

        private void Unsubscribe()
        {
            mRemotePythonRunner.RaiseDataReceivedEvent -= RaiseDataReceivedEvent;
        }

        #endregion

        #region Navigation

        public override void OnNavigatedTo(NavigationContext navigationContext) {}

        #endregion

        #region Events

        private void RaiseDataReceivedEvent(object sender, string data)
        {
            Application.Current?.Dispatcher?.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                if(ExecutionResult != null && !mIsExecutionStop)
                    ExecutionResult = ExecutionResult + $"{data}\n";
            }));
        }

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
                    StartCommand.RaiseCanExecuteChanged();
            }
        }

        private string mExecutionResult = "";
        public string ExecutionResult
        {
            get { return mExecutionResult; }
            set
            {
                SetProperty(ref mExecutionResult, value);
            }
        }

        #endregion


        #region DelegateCommand methods

        private void Start()
        {
            mExecutionResult = "";
            var sourceCode = SourceCode;
            mRemotePythonRunner.ConnectAndSendCodeAsync(sourceCode);
            mIsExecutionStop = false;
        }

        private bool StartCanExecute()
        {
            return !string.IsNullOrEmpty(SourceCode);
        }

        private void Stop()
        {
            mRemotePythonRunner.DisconnectAsync();
            mIsExecutionStop= true; 
        }

        private bool StopCanExecute()
        {
            return true;
        }

        private void SendDebugger(object commandParam)
        {
            var command = commandParam as string;
            mRemotePythonRunner.SendСontrolCharacter(command);
        }

        private bool SendDebuggerCanExecute(object arg)
        {
            return true;
        }

        #endregion

    }
}
